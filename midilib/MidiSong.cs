using Amazon.Runtime.SharedInterfaces;
using MeltySynth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static MeltySynth.MidiFile;
using NM = NAudio.Midi;

namespace midilib
{
    public class MidiSong
    {
        public enum TrackTypeDef
        {
            Drums,
            Arpeggio,
            Chords,
            String,
            MainMelody,
            Accompaniment,
            Bass,
            Empty
        }
        public struct PitchBend
        {
            public int offsetTicks;
            public int pitchOffset;
        }

        public class Note
        {
            public Note(int start, int length, byte not, byte vel)
            {
                startTicks = start;
                lengthTicks = length;
                note = not; 
                velocity = vel;
            }
            public int startTicks;
            public int lengthTicks;
            public byte note;
            public byte velocity;

            public List<PitchBend> pitchBends;
        }
        public class TrackInfo
        {
            public int ChannelNum { get; set; }
            public int ProgramNum;
            public int Volume = 0x7F;
            public int Pan = 0x7F;
            public string Instrument { get; set; }

            MidiFile.Message[] otherMessages;
            public Note[] Notes;
            public float UniqueMeasures = -1;
            public float FilledMeasures = -1;
            public double AverageNoteLength = 0;
            public double AverageNotePitch = 0;
            public double AverageNoteOverlap = 0;
            public double MelodyScore = 0;

            public TrackTypeDef TrackType { get; set; }
           

            public TrackInfo(int channelNum, string instrument, Message[] messages)
            {
                ChannelNum = channelNum;
                Instrument = instrument;
                BuildNotes(messages);
            }
            public TrackInfo(int channelNum, string instrument, Note[] notes)
            {
                ChannelNum = channelNum;
                Instrument = instrument;
                Notes = notes;
            }

            public void AnalyzeForType(int resolution, int songLength)
            {
                if (ChannelNum == 9)
                    TrackType = TrackTypeDef.Drums;
                else if (Notes.Length > 0)
                {
                    AverageNotePitch = Notes.Select(n => (int)n.note).Average();
                    AverageNoteLength = Notes.Select(n => (int)n.lengthTicks).Average();
                    AverageNoteLength /= resolution;
                    FindNoteOverlap();
                    FindMeasurePatterns(resolution, songLength);
                    if (AverageNoteOverlap > 2.25)
                        TrackType = TrackTypeDef.Chords;
                    else if (AverageNotePitch < 45)
                        TrackType = TrackTypeDef.Bass;
                    else if (AverageNoteLength > 2)
                        TrackType = TrackTypeDef.String;
                    else if (UniqueMeasures < 0.2)
                        TrackType = TrackTypeDef.Arpeggio;
                    else
                        TrackType = TrackTypeDef.MainMelody;
                }
                else
                    TrackType = TrackTypeDef.Empty;

                MelodyScore = (UniqueMeasures * FilledMeasures / AverageNoteOverlap);
            }

            class Hash
            {
                ulong hashedValue = 3074457345618258791ul;
                public ulong HashVal => hashedValue;

                public void AddVal(int val)
                {
                    hashedValue += (ulong)val;
                    hashedValue *= 3074457345618258799ul;
                }
            }
            void FindMeasurePatterns(int resolution, int songLength)
            {
                int measureTicks = resolution * 4;
                int measuresInSong = songLength / measureTicks;
                var measureNotes = Notes.GroupBy(n => n.startTicks / measureTicks);
                Dictionary<ulong, int> measureHashes = new Dictionary<ulong, int>();
                int totalMeasures = measureNotes.Count();
                FilledMeasures = (float)totalMeasures / (float)measuresInSong;
                foreach (var measure in measureNotes)
                {
                    Hash h = new Hash();
                    int measureOffsetTicks = measure.Key * measureTicks;

                    foreach (var note in measure)
                    {
                        int relativeTick = note.startTicks - measureOffsetTicks;
                        h.AddVal(relativeTick);
                        h.AddVal(note.note);
                        h.AddVal(note.lengthTicks);
                    }

                    int count;
                    if (measureHashes.TryGetValue(h.HashVal, out count))
                    {
                        measureHashes[h.HashVal] = count + 1;
                    }
                    else
                    {
                        measureHashes[h.HashVal] = 1;
                    }
                }

                int totalBuckets = measureHashes.Keys.Count();
                UniqueMeasures = (float)totalBuckets / (float)totalMeasures;
            }
            
            void FindNoteOverlap()
            {
                SortedDictionary<int, int> noteEvents = new SortedDictionary<int, int>();
                foreach (var note in Notes)
                {
                    int notechange = 0;
                    noteEvents.TryGetValue(note.startTicks, out notechange);
                    noteEvents[note.startTicks] = notechange + 1;
                    int endTicks = note.startTicks + note.lengthTicks;
                    notechange = 0;
                    noteEvents.TryGetValue(endTicks, out notechange);
                    noteEvents[endTicks] = notechange - 1;
                }

                int prevTicks = 0;
                int currentNotesOn = 0;
                int[] notesOn = new int[32]; 
                foreach (var kv in noteEvents)
                {
                    notesOn[currentNotesOn] += kv.Key - prevTicks;
                    currentNotesOn += kv.Value;
                    prevTicks = kv.Key;
                }

                int totalTicks = 0;
                int weightedTicks = 0;
                for (int i = 1; i < 32; i++)
                {
                    totalTicks += notesOn[i];
                    weightedTicks += notesOn[i] * i;
                }

                AverageNoteOverlap = (double)weightedTicks / totalTicks;
            }

            public void Quantize(int qticks)
            {                
                for (int i = 0; i < Notes.Length; ++i)
                {
                    Notes[i].startTicks = ((Notes[i].startTicks + qticks/2) / qticks) * qticks;
                    Notes[i].lengthTicks = (Notes[i].lengthTicks < qticks / 2) ? qticks / 2:
                        ((Notes[i].lengthTicks + qticks / 2) / qticks) * qticks;
                }
            }
            public MidiFile.Message[] Messages
            {
                get
                {
                    byte channel = (byte)ChannelNum;
                    List<Message> msgs = (otherMessages != null) ? otherMessages.ToList() : new List<Message>();
                    msgs.Add(new Message()
                    { Channel = channel, Command = 0xC0, Data1 = (byte)ProgramNum, Data2 = 0, Ticks = 0 });
                    msgs.Add(new Message()
                    { Channel = channel, Command = 0xB0, Data1 = (byte)0x7, Data2 = (byte)Volume, Ticks = 0 });
                    foreach (var note in Notes)
                    {

                        byte oncommand = 0x90;
                        byte offcommand = 0x80;
                        Message onmsg = new Message()
                        { Channel = channel, Command = oncommand, Data1 = note.note, Data2 = note.velocity, Ticks = note.startTicks };
                        msgs.Add(onmsg);
                        Message offmsg = new Message()
                        { Channel = channel, Command = offcommand, Data1 = note.note, Data2 = 0, Ticks = note.startTicks + note.lengthTicks };
                        msgs.Add(onmsg);
                        msgs.Add(offmsg);
                    }
                    return msgs.ToArray();
                }
            }
            void BuildNotes(MidiFile.Message[] messages)
            {
                Note[] noteOnTick = new Note[127 + 1];
                for (int j = 0; j < noteOnTick.Length; j++)
                    noteOnTick[j] = null;
                List<Message> othMessages = new List<Message>();
                List<Note> notes = new List<Note>();
                int mostRecentNote = -1;
                foreach (var msg in messages)
                {
                    if ((msg.Command & 0xF0) == 0x90 &&
                        msg.Data2 > 0)
                    {
                        if (noteOnTick[msg.Data1] == null)
                        {
                            mostRecentNote = msg.Data1;
                            noteOnTick[msg.Data1] = new Note(msg.Ticks, 0, msg.Data1, msg.Data2);
                        }
                    }
                    else if ((msg.Command & 0xF0) == 0x80 ||
                        ((msg.Command & 0xF0) == 0x90 &&
                        msg.Data2 == 0))
                    {
                        int endTicks = msg.Ticks;
                        if (noteOnTick[msg.Data1] != null)
                        {
                            noteOnTick[msg.Data1].lengthTicks = endTicks - noteOnTick[msg.Data1].startTicks;
                            notes.Add(noteOnTick[msg.Data1]);
                            noteOnTick[msg.Data1] = null;
                        }
                    }
                    else if ((msg.Command & 0xE0) == 0xE0)
                    {
                        int pitchBend = ((msg.Data1 | (msg.Data2 << 7)) - 8192);
                        if (mostRecentNote >= 0 && noteOnTick[mostRecentNote] != null)
                        {
                            if (noteOnTick[mostRecentNote].pitchBends == null)
                                noteOnTick[mostRecentNote].pitchBends = new List<PitchBend>();
                            noteOnTick[mostRecentNote].pitchBends.Add(new PitchBend()
                            {
                                offsetTicks = msg.Ticks -
                                noteOnTick[mostRecentNote].startTicks,
                                pitchOffset = pitchBend
                            });
                        }
                        othMessages.Add(msg);
                    }
                    else if ((msg.Command & 0xC0) == 0xC0)
                        ProgramNum = msg.Data1;
                    else if ((msg.Command & 0xB0) == 0xB0)
                    {
                        if (msg.Data1 == 7)
                            Volume = msg.Data2;
                        else if (msg.Data1 == 10)
                            Pan = msg.Data2;
                        else
                            othMessages.Add(msg);
                    }
                    else
                        othMessages.Add(msg);
                }
                Notes = notes.ToArray();
                otherMessages = othMessages.ToArray();
            }
        }

        public int Resolution;
        public int LengthSixteenths;
        public int LengthTicks;

        public MidiFile GetMidiFile() 
        {
            var messages = Tracks.SelectMany(t => t.Messages).ToList();
            messages.Add(Message.TempoChange(Tempo));
            messages.Sort((a, b) => a.Ticks - b.Ticks);
            var msgarray = messages.ToArray();
            SetMessageTimes(msgarray);
            return new MidiFile(msgarray, Resolution);
        }

        public void SaveToStream(Stream s)
        {
            const int MidiFileType = 0;

            const int TrackNumber = 0;

            var collection = new NM.MidiEventCollection(MidiFileType, Resolution);
            collection.AddEvent(new NM.TextEvent("Note Stream", NM.MetaEventType.TextEvent, 0), TrackNumber);
            collection.AddEvent(new NM.TempoEvent(CalculateMicrosecondsPerQuaterNote((int)Tempo), 0), TrackNumber);


            var messages = Tracks.SelectMany(t => t.Messages).ToList();
            messages.Add(Message.TempoChange(Tempo));
            messages.Sort((a, b) => a.Ticks - b.Ticks);
            var msgarray = messages.ToArray();
            SetMessageTimes(msgarray);

            foreach (var message in msgarray)
            {
                switch (message.Type)
                {
                    case MessageType.Normal:
                        {
                            if (message.Command == 0xC0)
                                collection.AddEvent(new NM.PatchChangeEvent(message.Ticks, message.Channel + 1, message.Data1), TrackNumber);
                            else
                                collection.AddEvent(new NM.NoteEvent(message.Ticks, message.Channel + 1, (NM.MidiCommandCode)message.Command, message.Data1, message.Data2),
                                    TrackNumber);
                        }
                        break;
                    case MessageType.TempoChange:
                        collection.AddEvent(new NM.TempoEvent(CalculateMicrosecondsPerQuaterNote((int)message.Tempo), message.Ticks), TrackNumber);
                        break;
                }
            }

            collection.PrepareForExport();
            Export(s, collection);
        }
        private static int CalculateMicrosecondsPerQuaterNote(int bpm)
        {
            return 60 * 1000 * 1000 / bpm;
        }

        private static uint SwapUInt32(uint i)
        {
            return ((i & 0xFF000000u) >> 24) | ((i & 0xFF0000) >> 8) | ((i & 0xFF00) << 8) | ((i & 0xFF) << 24);
        }

        private static ushort SwapUInt16(ushort i)
        {
            return (ushort)(((i & 0xFF00) >> 8) | ((i & 0xFF) << 8));
        }
        //
        // Summary:
        //     Exports a MIDI file
        //
        // Parameters:
        //   filename:
        //     Filename to export to
        //
        //   events:
        //     Events to export
        public static void Export(Stream s, NM.MidiEventCollection events)
        {
            if (events.MidiFileType == 0 && events.Tracks > 1)
            {
                throw new ArgumentException("Can't export more than one track to a type 0 file");
            }

            using BinaryWriter binaryWriter = new BinaryWriter(s);
            binaryWriter.Write(Encoding.UTF8.GetBytes("MThd"));
            binaryWriter.Write(SwapUInt32(6u));
            binaryWriter.Write(SwapUInt16((ushort)events.MidiFileType));
            binaryWriter.Write(SwapUInt16((ushort)events.Tracks));
            binaryWriter.Write(SwapUInt16((ushort)events.DeltaTicksPerQuarterNote));
            for (int i = 0; i < events.Tracks; i++)
            {
                IList<NM.MidiEvent> list = events[i];
                binaryWriter.Write(Encoding.UTF8.GetBytes("MTrk"));
                long position = binaryWriter.BaseStream.Position;
                binaryWriter.Write(SwapUInt32(0u));
                long absoluteTime = events.StartAbsoluteTime;
                MergeSort.Sort(list, new NM.MidiEventComparer());
                _ = list.Count;
                _ = 0;
                foreach (NM.MidiEvent item in list)
                {
                    item.Export(ref absoluteTime, binaryWriter);
                }

                uint num = (uint)((int)(binaryWriter.BaseStream.Position - position) - 4);
                binaryWriter.BaseStream.Position = position;
                binaryWriter.Write(SwapUInt32(num));
                binaryWriter.BaseStream.Position += num;
            }

        }
        public double Tempo = 120;
        public TrackInfo[] Tracks; 

        public MidiSong(MidiFile midiFile)
        {           
            Message? tmpo = midiFile.Messages.FirstOrDefault(m => m.Type == MessageType.TempoChange);
            Tempo = tmpo.Value.Type == MessageType.TempoChange ? (int)tmpo.Value.Tempo : 120;
            Resolution = midiFile.Resolution;
            LengthTicks = midiFile.Messages.Last().Ticks;
            int sixteenthRes = Resolution / 4;
            LengthSixteenths = LengthTicks / sixteenthRes;
            Build(midiFile);
        }        


        public MidiSong(TrackInfo[] tracks, int resolution, double tempo)
        {
            Resolution = resolution;
            Tempo = tempo;
            LengthTicks = tracks.Select(t => (t.Notes.LastOrDefault()?.startTicks + t.Notes.LastOrDefault()?.lengthTicks)??0).Max();
            Tracks = tracks;
            int sixteenthRes = Resolution / 4;
            LengthSixteenths = LengthTicks / sixteenthRes;
        }

        void Build(MidiFile midiFile)
        {
            var channelGroups = midiFile.Messages.Where(m => m.Channel < 16).GroupBy(m => m.Channel).
                OrderBy(g => g.Key);
            int numChannels = channelGroups.Count();
            List<TrackInfo> tracks = new List<TrackInfo>();
            for (int i = 0; i < numChannels; i++)
            {
                var kv = channelGroups.ElementAt(i);
                byte num = GetProgramNumber(kv);
                string instrument;
                if (kv.Key == 9)
                    GMInstruments.DrumKits.TryGetValue(num, out instrument);
                else
                    instrument = GMInstruments.Names[num];
                tracks.Add(new TrackInfo( kv.Key, instrument, kv.ToArray()));
            }
            Tracks = tracks.ToArray();

            foreach (var track in Tracks)
            {
                //track.Quantize(midiFile.Resolution / 4);
                track.AnalyzeForType(midiFile.Resolution, LengthTicks);
            }

            TrackInfo []mainMelodies = Tracks.Where(t => t.TrackType == TrackTypeDef.MainMelody).ToArray();
            if (mainMelodies.Length > 0)
            {
                double maxScore = mainMelodies.Max(t => t.MelodyScore);
                foreach (var m in mainMelodies) { if (m.MelodyScore < maxScore) m.TrackType = TrackTypeDef.Accompaniment; }
            }
        }

        int FirstNoteTick()
        {
            int firstNoteTick = int.MaxValue;
            foreach (TrackInfo t in Tracks)
            {
                if (t.Notes.Count() < 1) continue;
                firstNoteTick = Math.Min(firstNoteTick, t.Notes[0].startTicks);
            }
            return firstNoteTick;
        }

        void OffsetTicks(int tickOffset)
        {
            foreach (TrackInfo t in Tracks)
            {
                for (int i = 0; i < t.Notes.Count(); i++) 
                {
                    t.Notes[i].startTicks += tickOffset;
                }
            }
        }

        void TrimStart()
        {
            OffsetTicks(-FirstNoteTick());
        }
        public MidiSong ConvertToMelody()
        {
            TrackInfo ti = Tracks.FirstOrDefault(t => t.TrackType == TrackTypeDef.MainMelody);
            if (ti == null)
                return null;

            int firstMelodyTick = ti.Notes[0].startTicks;

            List<TrackInfo> tracks = new List<TrackInfo>();
            Note []newNotes = 
                ti.Notes.Select(n => new Note(n.startTicks - firstMelodyTick, n.lengthTicks, n.note, n.velocity)).ToArray();
            TrackInfo melodyTrack = new TrackInfo(0, ti.Instrument, newNotes);
            tracks.Add(melodyTrack);
            return new MidiSong(tracks.ToArray(), Resolution, Tempo);
        }


        int PitchBendToNoteOffset(int pitchBend)
        {
            int totalRange = 1 << 14;
            int noteRange = totalRange / 4; // 2 notes in either direction
            int halfRange = noteRange / 2;
            int roundedPitchOffset = (pitchBend + halfRange) / noteRange;
            return roundedPitchOffset;
        }
        List<Note> ConvertPitchBends(Note n)
        {
            List<Note> outNotes = new List<Note>();
            Note currentNote = new Note(n.startTicks, 0, n.note, n.velocity);
            int curOffset = 0;
            foreach (var pitch in n.pitchBends)
            {
                int noteOffset = PitchBendToNoteOffset(pitch.pitchOffset);
                if (noteOffset != curOffset)
                {
                    currentNote.lengthTicks = (n.startTicks + pitch.offsetTicks) - currentNote.startTicks;
                    outNotes.Add(currentNote);
                    curOffset = noteOffset;
                    currentNote = new Note((n.startTicks + pitch.offsetTicks), 0, (byte)(n.note + noteOffset), n.velocity);
                }
            }

            currentNote.lengthTicks = (n.startTicks + n.lengthTicks) - currentNote.startTicks;
            outNotes.Add(currentNote);
            return outNotes;
        }

        public MidiSong ConvertPitchBendsSong()
        {
            List<TrackInfo> tracks = new List<TrackInfo>();
            foreach (TrackInfo ti in Tracks)
            {
                List<Note> outNotes = new List<Note>();
                foreach (var note in ti.Notes)
                {
                    if (note.pitchBends?.Count() > 0 )
                        outNotes.AddRange(ConvertPitchBends(note));
                    else
                        outNotes.Add(note);
                }
                TrackInfo melodyTrack = new TrackInfo(ti.ChannelNum, ti.Instrument, outNotes.ToArray());
                melodyTrack.TrackType = ti.TrackType;
                melodyTrack.ProgramNum = ti.ProgramNum;
                tracks.Add(melodyTrack);
            }

            return new MidiSong(tracks.ToArray(), Resolution, Tempo);
        }

        public MidiSong ConvertToPianoSong()
        {
            MidiSong pitchBendsCnv = ConvertPitchBendsSong();
            return pitchBendsCnv.ConvertToPianoSongRaw();
        }

        MidiSong ConvertToPianoSongRaw()
        {
            List<Note> newNotes = new List<Note>();
            IEnumerable<TrackInfo> tiList = Tracks.Where(t => t.TrackType == TrackTypeDef.MainMelody);
            foreach (var ti in tiList)
            {
                AddNotesAsChannel0(newNotes, ti.Notes, 1000, null);
            }

            int measureTicks = Resolution * 4;
            int measuresInSong = LengthTicks / measureTicks;
            bool []measuresWithMelody = new bool[measuresInSong+1];
            foreach (var n in newNotes)
            {
                int startMeasure = n.startTicks / measureTicks;
                measuresWithMelody[startMeasure] = true;
            }

            tiList = tiList = Tracks.Where(t => t.TrackType != TrackTypeDef.MainMelody &&
                t.TrackType != TrackTypeDef.Drums);
            foreach (var ti in tiList)
            {
                int velocityMul = ti.TrackType == TrackTypeDef.Bass ? 1000 : 750;
                AddNotesAsChannel0(newNotes, ti.Notes, velocityMul, measuresWithMelody);
            }

            newNotes.Sort((n, b) => n.startTicks.CompareTo(b.startTicks));
            List<TrackInfo> tracks = new List<TrackInfo>();
            TrackInfo melodyTrack = new TrackInfo(0, "Piano", newNotes.ToArray());
            tracks.Add(melodyTrack);
            var pianoSong = new MidiSong(tracks.ToArray(), Resolution, Tempo);
            pianoSong.TrimStart();
            return pianoSong;
        }

        class NoteCmb
        {
            public bool isNew;
            public Note n;
        }
        void AddNotesAsChannel0(List<Note> songNotes, IEnumerable<Note> inNotes, int velocityMul, bool[]measuresWithMelody)
        {
            int measureTicks = Resolution * 4;
            Note[] currentNote = new Note[127];
            for (int i = 0; i < currentNote.Length; i++)
            {
                currentNote[i] = null;
            }

            List<NoteCmb> noteCmbs = inNotes.Select(n => new NoteCmb { n = n, isNew = true }).ToList();
            noteCmbs.AddRange(songNotes.Select(n => new NoteCmb { n = n, isNew = false }));
            noteCmbs.Sort((a, b) => { 
                int c = a.n.startTicks.CompareTo(b.n.startTicks);
                if (c != 0) return c;
                return a.isNew.CompareTo(b.isNew);
            });

            foreach (var note in noteCmbs)
            {
                int notekey = note.n.note;
                if (currentNote[notekey] == null ||
                    (currentNote[notekey].startTicks + currentNote[notekey].lengthTicks) < note.n.startTicks)
                {
                    currentNote[notekey] = note.n;
                    if (note.isNew)
                    {
                        bool hasMelody = measuresWithMelody != null && measuresWithMelody[note.n.startTicks / measureTicks];
                        byte newVelocity = hasMelody ? (byte)Math.Clamp((note.n.velocity * velocityMul) / 1000, 0, 127) : note.n.velocity;
                        songNotes.Add(new Note(note.n.startTicks, note.n.lengthTicks,
                            note.n.note, newVelocity));
                    }
                }
            }
        }

        byte GetProgramNumber(IEnumerable<MeltySynth.MidiFile.Message> _messages)
        {
            MeltySynth.MidiFile.Message var =
                _messages.FirstOrDefault((msg) => { return msg.Command == 0xC0; });
            return var.Data1;
        }

        void SetMessageTimes(Message[] messages)
        {
            var currentTick = 0;
            var currentTime = TimeSpan.Zero;

            var tempo = Tempo;

            for (int idx = 0; idx < messages.Length; ++idx)
            {
                var prevTick = idx > 0 ? messages[idx - 1].Ticks : 0;
                var deltaTick = messages[idx].Ticks - prevTick;
                var deltaTime = GetTimeSpanFromSeconds(60.0 / (Resolution * tempo) * deltaTick);

                currentTick += deltaTick;
                currentTime += deltaTime;

                if (messages[idx].Type == MessageType.TempoChange)
                    tempo = messages[idx].Tempo;

                messages[idx].Time = currentTime;
            }
        }

        internal static TimeSpan GetTimeSpanFromSeconds(double value)
        {
            return new TimeSpan((long)(TimeSpan.TicksPerSecond * value));
        }


    }
}

internal class MergeSort
{
    //
    // Summary:
    //     In-place and stable implementation of MergeSort
    private static void Sort<T>(IList<T> list, int lowIndex, int highIndex, IComparer<T> comparer)
    {
        if (lowIndex >= highIndex)
        {
            return;
        }

        int num = (lowIndex + highIndex) / 2;
        Sort(list, lowIndex, num, comparer);
        Sort(list, num + 1, highIndex, comparer);
        int num2 = num;
        int num3 = num + 1;
        while (lowIndex <= num2 && num3 <= highIndex)
        {
            if (comparer.Compare(list[lowIndex], list[num3]) <= 0)
            {
                lowIndex++;
                continue;
            }

            T value = list[num3];
            for (int num4 = num3 - 1; num4 >= lowIndex; num4--)
            {
                list[num4 + 1] = list[num4];
            }

            list[lowIndex] = value;
            lowIndex++;
            num2++;
            num3++;
        }
    }

    //
    // Summary:
    //     MergeSort a list of comparable items
    public static void Sort<T>(IList<T> list) where T : IComparable<T>
    {
        Sort(list, 0, list.Count - 1, Comparer<T>.Default);
    }

    //
    // Summary:
    //     MergeSort a list
    public static void Sort<T>(IList<T> list, IComparer<T> comparer)
    {
        Sort(list, 0, list.Count - 1, comparer);
    }
}