using System;
using midilib;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static midilib.NoteVis;

namespace midilib
{
    public class NoteVis
    {
        MeltySynth.MidiFile midiFile;

        TimeSpan now;
        TimeSpan currentLength;
        List<NoteBlock> noteBlocks;
        public List<NoteBlock> NoteBlocks => noteBlocks;
        Dictionary<uint, ActiveNote> activeNotes = new Dictionary<uint, ActiveNote>();
        public Dictionary<uint, ActiveNote> ActiveNotes => activeNotes;
        public Colors.RGB[] ChannelColors;

        public NoteVis(MeltySynth.MidiFile _midiFile)
        {
            midiFile = _midiFile;
            BuildPianoKeys();
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

        public class ActiveNote
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

        uint ToIndex(byte channel, byte note)
        {
            return (uint)(channel << 16) | note;
        }


        void AddNoteData(byte channel, byte note, bool on, TimeSpan time)
        {
            uint idx = ToIndex(channel, note);
            ActiveNote an;
            if (!activeNotes.TryGetValue(idx, out an))
            {
                an = new ActiveNote();
                activeNotes.Add(idx, an);
            }
            an.AddTime(time, on);
        }

        void BuildAciveNotes(TimeSpan now, TimeSpan delta)
        {
            activeNotes = new Dictionary<uint, ActiveNote>();
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
                    && msg.Channel != 9)
                {
                    if (msg.Command == MidiSpec.NoteOff ||
                        msg.Data2 == 0)
                    {
                        AddNoteData(msg.Channel, msg.Data1, false, msg.Time);
                    }
                    else
                    {
                        AddNoteData(msg.Channel, msg.Data1, true, msg.Time);
                    }

                }
            }
        }

        public class NoteBlock
        {
            public byte Channel;
            public byte Note;
            public float Start;
            public float Length;
        }

        public void Update(TimeSpan start, TimeSpan length)
        {
            now = start;
            currentLength = length;
            noteBlocks = GetNoteBlocks(start, length);
            UpdatePianoKeys();
        }

        public static int MidiToPiano(int midi)
        {
            if (midi < 21 || midi > 108)
                return -1;
            else return midi - 21;
        }
        void UpdatePianoKeys()
        {
            foreach (var key in PianoKeys)
            {
                key.channelsOn = 0;
            }

            foreach (var kv in activeNotes)
            {
                if (kv.Value.OnAtTime(now))
                {
                    byte channel = (byte)(kv.Key >> 16);
                    byte note = (byte)(kv.Key & 0xff);
                    int noteIdx = MidiToPiano(note);
                    if (noteIdx >= 0)
                    {
                        PianoKeys[noteIdx].channelsOn |= (uint)(1 << channel);
                    }
                }
            }
        }
        List<NoteBlock> GetNoteBlocks(TimeSpan start, TimeSpan length)
        {
            BuildAciveNotes(start, length);
            List<NoteBlock> noteBlocks = new List<NoteBlock>();
            float endTime = (float)(start + length).TotalMinutes;
            float startTime = (float)start.TotalMilliseconds;
            foreach (var kv in ActiveNotes)
            {
                byte channel = (byte)(kv.Key >> 16);
                byte note = (byte)(kv.Key & 0xff);
                var list = kv.Value;
                bool ison = !list.times.First().Item2;
                float onTime = startTime;
                foreach (var notetime in list.times)
                {
                    if (notetime.Item2 == ison)
                        continue;
                    if (!notetime.Item2)
                    {
                        if (notetime.Item1 > start)
                        {
                            float noteEndTime = (float)notetime.Item1.TotalMilliseconds;
                            NoteBlock nb = new NoteBlock()
                            {
                                Channel = channel,
                                Note = note,
                                Start = onTime - startTime,
                                Length = (noteEndTime - onTime)
                            };
                            noteBlocks.Add(nb);
                        }
                    }
                    else
                        onTime = (float)notetime.Item1.TotalMilliseconds;
                    ison = notetime.Item2;
                }
                if (ison)
                {
                    NoteBlock nb = new NoteBlock()
                    {
                        Channel = channel,
                        Note = note,
                        Start = onTime - startTime,
                        Length = (endTime - onTime)
                    };
                    noteBlocks.Add(nb);
                }
            }
            return noteBlocks;
        }


        public class PianoKey
        {
            public float x;
            public float y;
            public float ys;
            public bool isBlack;
            public uint channelsOn = 0;
        }

        public PianoKey[] PianoKeys = new PianoKey[88];
        public float PianoTopY = 0;
        public float PianoWhiteXs = 0;
        public float PianoBlackXs = 0;
        void BuildPianoKeys()
        {
            int nWhiteKeys = 52;
            float xscale = 1.0f / (float)(nWhiteKeys + 1);
            float xleft = 0;
            bool[] hasBlackKey = { true, false, true, true, false, true, true };

            PianoWhiteXs = xscale * 0.8f;
            PianoBlackXs = PianoWhiteXs * 0.5f;

            int keyIdx = 0;
            for (int i = 0; i < nWhiteKeys; i++)
            {
                float xval = xleft + (i + 0.5f) * xscale;

                PianoKeys[keyIdx++] = new PianoKey { isBlack = false, x = xval, y = 0.5f, ys = 1 };

                int note = i % 7;
                if (!hasBlackKey[note] || keyIdx >= 88)
                    continue;

                xval = xleft + (i + 1) * xscale;
                PianoKeys[keyIdx++] = new PianoKey { isBlack = true, x = xval, y = 0.2f, ys = 0.4f };
            }

            float yscale = 0.1f;
            this.PianoTopY = 1 - yscale;
            for (int i = 0; i < PianoKeys.Length; ++i)
            {
                PianoKeys[i].y = 1 - PianoKeys[i].y;
                PianoKeys[i].y *= yscale;
                PianoKeys[i].ys *= yscale;
                PianoKeys[i].y = 1 - PianoKeys[i].y;
            }
        }
    }
}

