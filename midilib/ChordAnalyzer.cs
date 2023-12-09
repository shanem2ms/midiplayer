using MeltySynth;
using NAudio.Midi;

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


    }
}
