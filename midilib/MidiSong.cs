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
        public class TrackInfo
        {
            public int ChannelNum;
            public int ProgramNum;
            public string Instrument;

            public MidiFile.Message[] Messages; 
        }

        MeltySynth.MidiFile midiFile;
        public int Resolution => midiFile.Resolution;
        public int LengthSixteenths;
        public int LengthTicks;
        public int NumChannels;

        public MidiFile GetMidiFile() { return midiFile; }

        public TrackInfo[] Tracks; 
        public MidiSong(MeltySynth.MidiFile _midiFile)
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
                tracks.Add(new TrackInfo { Instrument = instrument, ChannelNum = kv.Key, ProgramNum = num,
                    Messages = kv.ToArray() });
            }
            Tracks = tracks.ToArray();
        }

        void Quantize(MidiFile.Message[] messages, int ticks)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                messages[i].Ticks = (messages[i].Ticks / ticks) * ticks;
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
