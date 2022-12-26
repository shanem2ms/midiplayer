using System;
using midilib;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static midilib.DrumVis;

namespace midilib
{
    public class DrumVis
    {
        MeltySynth.MidiFile midiFile;

        TimeSpan now;
        TimeSpan currentLength;
        List<DrumBlock> drumBlocks;
        public List<DrumBlock> DrumBlocks => drumBlocks;
        Dictionary<uint, ActiveDrum> activeDrums = new Dictionary<uint, ActiveDrum>();
        public Dictionary<uint, ActiveDrum> ActiveDrums => activeDrums;
        public Colors.RGB[] ChannelColors;

        public DrumVis(MeltySynth.MidiFile _midiFile)
        {
            midiFile = _midiFile;
            BuildPalette();
        }

        void BuildPalette()
        {
            ChannelColors = new Colors.RGB[16];
            for (int i = 0; i < 16; ++i)
            {
                float h = (float)i / 16.0f;
                Colors.HSL hsl = new Colors.HSL() { H = h, S = 1, L = 0.5f };
                ChannelColors[i] = Colors.HSLToRGB(hsl);
            }
        }

        public class ActiveDrum
        {
            public List<Tuple<TimeSpan, bool>> times = new List<Tuple<TimeSpan, bool>>();

            public void AddTime(TimeSpan ts, bool onoff)
            {
                times.Add(new Tuple<TimeSpan, bool>(ts, onoff));
            }

            public bool OnAtTime(TimeSpan ts)
            {
                bool ison = false;
                foreach (var t in times)
                {
                    if (ts < t.Item1)
                        return ison;
                    ison = t.Item2;
                }
                return false;
            }
        }

        uint ToIndex(byte channel, byte drum)
        {
            return (uint)(channel << 16) | drum;
        }


        void AddDrumData(byte channel, byte drum, bool on, TimeSpan time)
        {
            uint idx = ToIndex(channel, drum);
            ActiveDrum an;
            if (!activeDrums.TryGetValue(idx, out an))
            {
                an = new ActiveDrum();
                activeDrums.Add(idx, an);
            }
            an.AddTime(time, on);
        }

        void BuildAciveDrums(TimeSpan now, TimeSpan delta)
        {
            activeDrums = new Dictionary<uint, ActiveDrum>();
            int startIndex = Array.BinarySearch(midiFile.Times, now - delta);
            if (startIndex < 0)
            {
                startIndex = ~startIndex;
            }
            int endIndex = Array.BinarySearch(midiFile.Times, now + delta);
            if (endIndex < 0)
            {
                endIndex = ~endIndex;
            }

            for (int idx = startIndex; idx < endIndex; ++idx)
            {
                MeltySynth.MidiFile.Message msg = midiFile.Messages[idx];
                if (msg.Channel >= 16)
                    continue;
                if ((msg.Command == MidiSpec.NoteOff || msg.Command == MidiSpec.NoteOn)
                    && msg.Channel == 9)
                {
                    if (msg.Command == MidiSpec.NoteOff ||
                        msg.Data2 == 0)
                    {
                        AddDrumData(msg.Channel, msg.Data1, false, msg.Time);
                    }
                    else
                    {
                        AddDrumData(msg.Channel, msg.Data1, true, msg.Time);
                    }

                }
            }
        }

        public class DrumBlock
        {
            public byte Channel;
            public byte Drum;
            public float Start;
            public float Length;
        }

        public void Update(TimeSpan start, TimeSpan length)
        {
            now = start;
            currentLength = length;
            drumBlocks = GetDrumBlocks(start, length);
        }

        public static int MidiToPiano(int midi)
        {
            if (midi < 21 || midi > 108)
                return -1;
            else return midi - 21;
        }

        List<DrumBlock> GetDrumBlocks(TimeSpan start, TimeSpan length)
        {
            BuildAciveDrums(start, length);
            List<DrumBlock> drumBlocks = new List<DrumBlock>();
            float endTime = (float)(start + length).TotalMinutes;
            float startTime = (float)start.TotalMilliseconds;
            foreach (var kv in ActiveDrums)
            {
                byte channel = (byte)(kv.Key >> 16);
                byte drum = (byte)(kv.Key & 0xff);
                var list = kv.Value;
                bool ison = !list.times.First().Item2;
                float onTime = startTime;
                foreach (var drumtime in list.times)
                {
                    if (drumtime.Item2 == ison)
                        continue;
                    if (!drumtime.Item2)
                    {
                        if (drumtime.Item1 > start)
                        {
                            float drumEndTime = (float)drumtime.Item1.TotalMilliseconds;
                            float tt = MathF.Max(onTime, startTime);
                            DrumBlock nb = new DrumBlock()
                            {
                                Channel = channel,
                                Drum = drum,
                                Start = tt - startTime,
                                Length = (drumEndTime - tt)
                            };
                            drumBlocks.Add(nb);
                        }
                    }
                    else
                        onTime = (float)drumtime.Item1.TotalMilliseconds;
                    ison = drumtime.Item2;
                }
                if (ison)
                {
                    float tt = MathF.Max(onTime, startTime);
                    DrumBlock nb = new DrumBlock()
                    {
                        Channel = channel,
                        Drum = drum,
                        Start = tt - startTime,
                        Length = (endTime - tt)
                    };
                    drumBlocks.Add(nb);
                }
            }
            return drumBlocks;
        }
    }
}

