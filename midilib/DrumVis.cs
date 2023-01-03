using System;
using midilib;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static midilib.DrumVis;
using MeltySynth;

namespace midilib
{
    public class DrumVis : Vis
    {
        public DrumVis(MidiFile _midiFile) : base(_midiFile)
        {
        }

        public override List<Cube> DoVis(TimeSpan visTimeSpan, MidiPlayer player)
        {
            return new List<Cube>();
        }
    }
}

