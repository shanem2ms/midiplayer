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
        public NoteVis(MeltySynth.MidiFile _midiFile)
        {
            midiFile = _midiFile;
            BuildPianoKeys();
        }

        public class ActiveNote
        {
            public List<Tuple<TimeSpan, bool>> times = new List<Tuple<TimeSpan, bool>>();

            public void AddTime(TimeSpan ts, bool onoff)
            {
                times.Add(new Tuple<TimeSpan, bool>(ts, onoff));
            }
        }

        Dictionary<uint, ActiveNote> activeNotes = new Dictionary<uint, ActiveNote>();
        public Dictionary<uint, ActiveNote> ActiveNotes => activeNotes;
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

        void GetNotes(TimeSpan now, TimeSpan delta)
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
                if (msg.Command == MidiSpec.NoteOff || msg.Command == MidiSpec.NoteOn)
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
        public List<NoteBlock> GetNoteBlocks(TimeSpan start, TimeSpan length)
        {
            GetNotes(start, length);
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
            public float xs;
            public float ys;
            public bool isBlack;
        }

        public PianoKey[] PianoKeys = new PianoKey[88];
        public float PianoTopY = 0;
        void BuildPianoKeys()
        {
            int nWhiteKeys = 52;
            float xscale = 2.0f / (float)(nWhiteKeys + 1);
            float yscale = xscale * 4;
            float xleft = -1;
            float yval = -1 + yscale / 2;
            float ytop = yval + yscale / 2;
            this.PianoTopY = ytop;
            bool[] hasBlackKey = { true, false, true, true, false, true, true };
            float yscaleBlk = xscale * 2;
            float byval = ytop - yscaleBlk / 2;

            int keyIdx = 0;
            for (int i = 0; i < nWhiteKeys; i++)
            {
                float xval = xleft + (i + 0.5f) * xscale;

                float xs = xscale * 0.4f;
                PianoKeys[keyIdx++] = new PianoKey { isBlack = false, x = xval, y = yval, xs = xs, ys = yscale };

                int note = i % 7;
                if (!hasBlackKey[note] || keyIdx >= 88)
                    continue;

                xval = xleft + (i + 1) * xscale;
                xs = xscale * 0.25f;
                PianoKeys[keyIdx++] = new PianoKey { isBlack = true, x = xval, y = byval, xs = xs, ys = yscaleBlk };
            }
        }
    }
}

