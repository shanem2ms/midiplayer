using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MeltySynth;
using static MeltySynth.MidiFile;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace midilib
{
    public class MidiSong
    {

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
            public int ChannelNum;
            public int ProgramNum;
            public string Instrument;

            MidiFile.Message[] otherMessages;
            public Note[] Notes;

            public enum TrackTypeDef
            {
                Drums,
                Arpeggio,
                Chords,
                Vocals,
                Bass,
                Empty
            }

            TrackTypeDef trackType;
            public TrackTypeDef TrackType => trackType;

            public MidiFile.Message[] Messages { get
                {
                    List<Message> msgs = otherMessages.ToList();
                    foreach (var note in Notes)
                    {
                        byte channel = (byte)ChannelNum;
                        
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

            public TrackInfo(int channelNum, int programNum, string instrument, Message[] messages)
            {
                ChannelNum = channelNum;
                ProgramNum = programNum;
                Instrument = instrument;
                BuildNotes(messages);
            }

            public void AnalyzeForType(int resolution)
            {
                if (ChannelNum == 9)
                    trackType = TrackTypeDef.Drums;
                else if (Notes.Length > 0)
                {
                    double a = Notes.Select(n => (int)n.note).Average();
                    if (a < 45)
                        trackType = TrackTypeDef.Bass;
                    else
                    {
                        FindMeasurePatters(resolution);
                        trackType = TrackTypeDef.Chords;
                    }
                }
                else
                    trackType = TrackTypeDef.Empty;
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
            void FindMeasurePatters(int resolution)
            {
                int measureTicks = resolution * 4;

                var measureNotes = Notes.GroupBy(n => n.startTicks / measureTicks);
                Dictionary<ulong, int> measureHashes = new Dictionary<ulong, int>();
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
            }

            public void Quantize(int qticks)
            {
                for (int i = 0; i < Notes.Length; ++i)
                {
                    Notes[i].startTicks = ((Notes[i].startTicks + qticks/2) / qticks) * qticks;
                    Notes[i].lengthTicks = ((Notes[i].lengthTicks + qticks / 2) / qticks) * qticks;
                }
            }

            void BuildNotes(MidiFile.Message[] messages)
            {
                Note[] noteOnTick = new Note[127];
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
                            mostRecentNote= msg.Data1;
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
                    else
                        othMessages.Add(msg);
                }
                Notes = notes.ToArray();
                otherMessages = othMessages.ToArray();
            }
        }

        MidiFile midiFile;
        public int Resolution => midiFile.Resolution;
        public int LengthSixteenths;
        public int LengthTicks;
        public int NumChannels;

        public MidiFile GetMidiFile() 
        {
            var messages = Tracks.SelectMany(t => t.Messages).ToList();
            messages.Sort((a, b) => a.Ticks - b.Ticks);
            var msgarray = messages.ToArray();
            SetMessageTimes(msgarray);
            return new MidiFile(msgarray, Resolution);
        }

        public TrackInfo[] Tracks; 
        public MidiSong(MidiFile _midiFile)
        {
            midiFile = _midiFile;            
            Build();
        }        

        void Build()
        {
            int sixteenthRes = midiFile.Resolution / 4;
            LengthTicks = midiFile.Messages.Last().Ticks;
            LengthSixteenths = LengthTicks / sixteenthRes;

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
                tracks.Add(new TrackInfo( kv.Key, num, instrument, kv.ToArray()));
            }
            Tracks = tracks.ToArray();

            foreach (var track in Tracks)
            {
                track.Quantize(midiFile.Resolution / 4);
                track.AnalyzeForType(midiFile.Resolution);
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

            var tempo = 120.0;

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
