using System;
using midilib;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static midilib.DrumVis;
using MeltySynth;
using static MeltySynth.MidiFile;
using static midilib.ActiveVis;

namespace midilib
{
    public class DrumVis : Vis
    {
        public DrumVis(MidiFile _midiFile) : base(_midiFile)
        {
        }

        TimeSpan prev = TimeSpan.Zero;

        class LiveNote
        {
            public float duration;
            public bool active;
        }

        LiveNote[] liveNotes = new LiveNote[88];
        public override List<Cube> DoVis(TimeSpan visTimeSpan, MidiPlayer player)
        {
            List<Cube> drumCubes = new List<Cube>();
            TimeSpan now = player.CurrentSongTime;
            int startIndex = Array.BinarySearch(midiFile.Times, prev);
            if (startIndex < 0)
            {
                startIndex = ~startIndex;
            }
            int endIndex = Array.BinarySearch(midiFile.Times, now);
            if (endIndex < 0)
            {
                endIndex = ~endIndex;
            }

            for (int idx = startIndex; idx < endIndex; ++idx)
            {
                MeltySynth.MidiFile.Message msg = midiFile.Messages[idx];
                if (msg.Channel == 9 && msg.Command == MidiSpec.NoteOn)
                {
                    int note = msg.Data1 - 21;
                    if (liveNotes[msg.Data1 - 21] == null)
                        liveNotes[msg.Data1 - 21] = new LiveNote();
                    liveNotes[msg.Data1 - 21].active = true;
                    liveNotes[msg.Data1 - 21].duration = 0;
                }
            }
            prev = now;
            float totalLen = 24;
            for (int i = 0; i < liveNotes.Length; ++i)
            {
                LiveNote ln = liveNotes[i];
                if (ln == null || !ln.active)
                    continue;
                float alpha = 1 - (ln.duration / totalLen);
                drumCubes.Add(new Cube()
                {
                    color = new Vector4(0.5f, 0.5f, 0.5f, 1) * alpha,
                    mat =
                    Matrix4x4.CreateScale(new Vector3(0.02f, 0.02f, 0.003f)) * Matrix4x4.CreateTranslation(new Vector3((float)i / liveNotes.Length, 0.5f, -2f))
                });
                ln.duration += 1.0f;

                if (ln.duration >= totalLen)
                    ln.active = false;
            }
            return drumCubes;
        }
    }
}

