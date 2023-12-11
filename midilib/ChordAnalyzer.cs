using MeltySynth;
using NAudio.Midi;
using System.Drawing;
using System.Linq;
using System;

namespace midilib
{
    public class ChordAnalyzer
    {

        MeltySynth.MidiFile midiFile;
        int resolution;
        public ChordAnalyzer(MeltySynth.MidiFile _midiFile) 
        {
            midiFile = _midiFile;
        }

        public void Analyze()
        {
            int pixelsPerSixteenth = 10;
            int sixteenthRes = midiFile.Resolution / 4;
            double pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
            int lastTick = midiFile.Messages.Last().Ticks;
            long lengthSixteenths = lastTick / sixteenthRes;

            var channelGroups = midiFile.Messages.Where(m => m.Channel < 16).GroupBy(m => m.Channel).
                OrderBy(g => g.Key);
            int numChannels = channelGroups.Count();

            int gmNoteRange = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;

            Random r = new Random();
            int numSixteenths = (lastTick +1)/ sixteenthRes;
            for (int i = 0; i < numChannels; i++)
            {
                int[] noteOnTick = new int[127];
                for (int j = 0; j < noteOnTick.Length; j++)
                    noteOnTick[j] = -1;

                var grp = channelGroups.ElementAt(i);
                int msgIdx = 0;
                int msgLength = grp.Count();
                for (int sixteenthIdx = 0; sixteenthIdx < numSixteenths; sixteenthIdx++)
                {
                    int tickIdx = sixteenthIdx * sixteenthRes;
                    while (msgIdx < msgLength) 
                    {
                        var msg = grp.ElementAt(msgIdx);
                        if (msg.Ticks > tickIdx)
                            break;

                        if ((msg.Command & 0xF0) == 0x90 &&
                        msg.Data2 > 0)
                        {
                            if (noteOnTick[msg.Data1] == -1)
                                noteOnTick[msg.Data1] = msg.Ticks;
                        }
                        else if ((msg.Command & 0xF0) == 0x80 ||
                            ((msg.Command & 0xF0) == 0x90 &&
                            msg.Data2 == 0))
                        {
                            int startTicks = noteOnTick[msg.Data1];
                            int endTicks = msg.Ticks;
                            noteOnTick[msg.Data1] = -1;
                        }

                        msgIdx++;
                    }

                    int[] noteCnts = new int[12];
                    int chordCount = 0;
                    for (int noteIdx = 0; noteIdx < noteOnTick.Length; noteIdx++)
                    {
                        if (noteOnTick[noteIdx] >= 0)
                        {
                            if (noteCnts[noteIdx%12]++ == 0)
                                chordCount++;
                        }
                    }

                    if (chordCount >= 3)
                    {
                    }
                }

            }
        }
    }
}
