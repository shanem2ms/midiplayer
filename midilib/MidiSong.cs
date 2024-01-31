using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MeltySynth;
using static MeltySynth.MidiFile;

namespace midilib
{
    public class MidiSong
    {

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
            
        }
        public class TrackInfo
        {
            public int ChannelNum;
            public int ProgramNum;
            public string Instrument;

            MidiFile.Message[] otherMessages;
            public Note[] Notes;

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

            public void Quantize(int qticks)
            {
                for (int i = 0; i < Notes.Length; ++i)
                {
                    Notes[i].startTicks = ((Notes[i].startTicks + qticks/2) / qticks) * qticks;
                }
            }

            void BuildNotes(MidiFile.Message[] messages)
            {
                int[] noteOnTick = new int[127];
                int[] noteOnVelocity = new int[127];
                for (int j = 0; j < noteOnTick.Length; j++)
                    noteOnTick[j] = -1;
                List<Message> othMessages = new List<Message>();
                List<Note> notes = new List<Note>();
                foreach (var msg in messages)
                {
                    if ((msg.Command & 0xF0) == 0x90 &&
                        msg.Data2 > 0)
                    {
                        if (noteOnTick[msg.Data1] == -1)
                        {
                            noteOnTick[msg.Data1] = msg.Ticks;
                            noteOnVelocity[msg.Data1] = msg.Data2;
                        }
                    }
                    else if ((msg.Command & 0xF0) == 0x80 ||
                        ((msg.Command & 0xF0) == 0x90 &&
                        msg.Data2 == 0))
                    {
                        int startTicks = noteOnTick[msg.Data1];
                        int endTicks = msg.Ticks;
                        noteOnTick[msg.Data1] = -1;

                        if (startTicks >= 0 && msg.Data1 >= GMInstruments.MidiStartIdx &&
                            msg.Data1 < GMInstruments.MidiEndIdx)
                        {
                            Note note = new Note(startTicks, endTicks - startTicks, msg.Data1, (byte)noteOnVelocity[msg.Data1]);
                            notes.Add(note);
                        }
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
            return new MidiFile(messages.ToArray(), Resolution);
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
                track.Quantize(midiFile.Resolution / 2);
            }
        }

        byte GetProgramNumber(IEnumerable<MeltySynth.MidiFile.Message> _messages)
        {
            MeltySynth.MidiFile.Message var =
                _messages.FirstOrDefault((msg) => { return msg.Command == 0xC0; });
            return var.Data1;
        }
    }
}
