using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace midilib
{
    public class MidiSong
    {
        public class TrackInfo
        {
            public int ChannelNum;
            public int ProgramNum;
            public string Instrument;
        }

        MeltySynth.MidiFile midiFile;

        public TrackInfo[] Tracks; 
        public MidiSong(MeltySynth.MidiFile _midiFile)
        {
            midiFile = _midiFile;
        }

        void Build()
        {
            int sixteenthRes = midiFile.Resolution / 4;
            int lastTick = midiFile.Messages.Last().Ticks;
            long lengthSixteenths = lastTick / sixteenthRes;


            var channelGroups = midiFile.Messages.Where(m => m.Channel < 16).GroupBy(m => m.Channel).
                OrderBy(g => g.Key);
            int numChannels = channelGroups.Count();
            List<TrackInfo> tracks = new List<TrackInfo>();
            for (int i = 0; i < numChannels; i++)
            {
                var kv = channelGroups.ElementAt(i);
                byte num = GetProgramNumber(kv);
                string instrument = kv.Key == 9 ? GMInstruments.DrumKits[num] : GMInstruments.Names[num];
                tracks.Add(new TrackInfo { Instrument = instrument, ChannelNum = kv.Key, ProgramNum = num });
            }
            Tracks = tracks.ToArray();
        }
        byte GetProgramNumber(IEnumerable<MeltySynth.MidiFile.Message> _messages)
        {
            MeltySynth.MidiFile.Message var =
                _messages.FirstOrDefault((msg) => { return msg.Command == 0xC0; });
            return var.Data1;
        }
    }
}
