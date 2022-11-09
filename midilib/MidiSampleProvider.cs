using System;
using NAudio.Wave;
using MeltySynth;
namespace midiplayer
{
    public class MidiSampleProvider : ISampleProvider
    {
        private static WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        private Synthesizer synthesizer;
        private MidiFileSequencer sequencer;
        public MidiFileSequencer Sequencer => sequencer;


        private object mutex;

        public void SetVolume(int volume)
        {
            synthesizer.Volume = volume;
        }

        public MidiSampleProvider(string soundFontPath)
        {
            synthesizer = new Synthesizer(soundFontPath, format.SampleRate);
            synthesizer.MasterVolume = 1.0f;
            sequencer = new MidiFileSequencer(synthesizer);
            mutex = new object();
        }

        public void Play(MeltySynth.MidiFile midiFile)
        {
            lock (mutex)
            {
                sequencer.Play(midiFile, false);
            }
        }

        public void Stop()
        {
            lock (mutex)
            {
                sequencer.Stop();
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            lock (mutex)
            {
                sequencer.RenderInterleaved(buffer.AsSpan(offset, count));
            }

            return count;
        }

        public WaveFormat WaveFormat => format;
    }

}