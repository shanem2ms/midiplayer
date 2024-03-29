﻿using System;
using midilib;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static midilib.Vis;
using static midilib.ActiveVis;

namespace midilib
{
    public abstract class Vis
    {
        protected MeltySynth.MidiFile midiFile;

      
        protected Vis(MeltySynth.MidiFile _midiFile)
        {
            midiFile = _midiFile;
        }

        public class Cube
        {
            public Matrix4x4 mat;
            public Vector4 color;
        }
        public abstract List<Cube> DoVis(TimeSpan visTimeSpan, MidiPlayer player);
    }


    public abstract class ActiveVis : Vis
    {
        protected TimeSpan now;
        protected TimeSpan currentLength;
        Dictionary<uint, ActiveNote> activeNotes = new Dictionary<uint, ActiveNote>();
        public Dictionary<uint, ActiveNote> ActiveNotes => activeNotes;

        protected ActiveVis(MeltySynth.MidiFile _midiFile) :
            base(_midiFile)
        { }

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

        protected void BuildAciveNotes(TimeSpan now, TimeSpan delta)
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

    }
    public class NoteVis : ActiveVis
    {
        List<NoteBlock> noteBlocks;
        static public Vector4[] ChannelColors;
        Piano piano = new Piano();

        public NoteVis(MeltySynth.MidiFile _midiFile)
            : base(_midiFile)
        {
            
        }

        static NoteVis()
        {
            BuildPalette();
        }
        static void BuildPalette()
        {
            ChannelColors = new Vector4[16];
            for (int i = 0; i < 16; ++i)
            {
                float h = (float)i / 16.0f;
                Colors.HSL hsl = new Colors.HSL() { H = h, S = 1, L = 0.5f };
                ChannelColors[i] = Colors.HSLToRGB(hsl).ToVector4();
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
            if (midi < GMInstruments.MidiStartIdx || midi > GMInstruments.MidiEndIdx)
                return -1;
            else return midi - GMInstruments.MidiStartIdx;
        }
        void UpdatePianoKeys()
        {
            foreach (var key in piano.PianoKeys)
            {
                key.channelsOn = 0;
            }

            foreach (var kv in ActiveNotes)
            {
                if (kv.Value.OnAtTime(now))
                {
                    byte channel = (byte)(kv.Key >> 16);
                    byte note = (byte)(kv.Key & 0xff);
                    int noteIdx = MidiToPiano(note);
                    if (noteIdx >= 0)
                    {
                        piano.PianoKeys[noteIdx].channelsOn |= (uint)(1 << channel);
                    }
                }
            }
        }

        public override List<Cube> DoVis(TimeSpan visTimeSpan, MidiPlayer player)
        {
            List<Cube> outCubes = new List<Cube>();
            float blockLength = (float)visTimeSpan.TotalMilliseconds;
            TimeSpan t = player.CurrentSongTime;
            Update(t, visTimeSpan);
            float noteYScale = piano.PianoTopY;
            foreach (var nb in noteBlocks)
            {
                if (nb.Length < 0)
                    continue;
                if (nb.Note < 21 || nb.Note > 108)
                    continue;

                int pianoKeyIdx = nb.Note - 21;
                Piano.PianoKey pianoKey = piano.PianoKeys[pianoKeyIdx];
                float x0 = pianoKey.x;
                float xs = pianoKey.isBlack ? piano.PianoBlackXs : piano.PianoWhiteXs * 0.75f;
                float ys = nb.Length / blockLength;
                float y0 = (nb.Start + nb.Length * 0.5f) / blockLength;
                ys *= noteYScale;
                y0 = noteYScale - y0;

                Matrix4x4 mat = Matrix4x4.CreateScale(new Vector3(xs, ys, 0.003f)) *
                    Matrix4x4.CreateTranslation(new Vector3(x0, y0, -2));
                outCubes.Add(new Cube() { mat = mat, color = ChannelColors[nb.Channel] });
            }


            Vector4 pianoWhiteColor = Vector4.One;
            Vector4 pianoBlackColor = new Vector4(0, 0, 0, 1);
            Vector4 pianoPlayingColor = new Vector4(0, 0.5f, 1, 1);
            Vector4 pianoBlackPlayingColor = new Vector4(0.35f, 0.75f, 1, 1);
            foreach (var key in piano.PianoKeys)
            {
                if (key.isBlack) continue;
                Matrix4x4 mat = Matrix4x4.CreateScale(new Vector3(piano.PianoWhiteXs, key.ys, 0.003f)) *
                    Matrix4x4.CreateTranslation(new Vector3(key.x, key.y, -2));
                outCubes.Add(new Cube() { mat = mat, color = key.channelsOn > 0 ? pianoPlayingColor : pianoWhiteColor });
            }
            foreach (var key in piano.PianoKeys)
            {
                if (!key.isBlack) continue;
                Matrix4x4 mat = Matrix4x4.CreateScale(new Vector3(piano.PianoBlackXs, key.ys, 0.003f)) *
                    Matrix4x4.CreateTranslation(new Vector3(key.x, key.y, -2));

                outCubes.Add(new Cube() { mat = mat, color = key.channelsOn > 0 ? pianoBlackPlayingColor : pianoBlackColor });
            }

            return outCubes;
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
                            float tt = MathF.Max(onTime, startTime);
                            NoteBlock nb = new NoteBlock()
                            {
                                Channel = channel,
                                Note = note,
                                Start = tt - startTime,
                                Length = (noteEndTime - tt)
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
                    float tt = MathF.Max(onTime, startTime);
                    NoteBlock nb = new NoteBlock()
                    {
                        Channel = channel,
                        Note = note,
                        Start = tt - startTime,
                        Length = (endTime - tt)
                    };
                    noteBlocks.Add(nb);
                }
            }
            return noteBlocks;
        }

       

    }
}

