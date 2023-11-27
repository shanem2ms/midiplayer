using MeltySynth;
using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace midilib
{
    public class MidiSampleProvider : ISampleProvider
    {
        private static WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        private Synthesizer synthesizer;
        private MidiFileSequencer sequencer;
        public MidiFileSequencer Sequencer => sequencer;
        private float rms = 0;
        public float RMS => rms;


        private object mutex;

        public void SetVolume(int volume)
        {
            synthesizer.Volume = volume;
        }

        public MidiSampleProvider()
        {
            mutex = new object();
        }

        public async Task<bool> Initialize(string cacheFile)
        {                        
            SoundFont sf = new SoundFont(cacheFile);
            SynthesizerSettings settings = new SynthesizerSettings(format.SampleRate);
            //settings.EnableReverbAndChorus = false;
            synthesizer = new Synthesizer(sf, settings);
            synthesizer.MasterVolume = 1.0f;
            sequencer = new MidiFileSequencer(synthesizer);
            return true;
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
                sequencer?.Stop();
            }
        }

        float maxval = 0;
        float CalculateRms(float[] buffer, int offset, int count)
        {
            float total = 0;
            for (int i = 0; i < count; ++i)
            {
                float v = buffer[i];
                maxval = MathF.Max(maxval, MathF.Abs(v));
                total += v * v; 
            }
            return MathF.Sqrt(total /= count);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            lock (mutex)
            {
                sequencer.RenderInterleaved(buffer.AsSpan(offset, count));
            }

            rms = CalculateRms(buffer, offset, count);
            return count;
        }

        public WaveFormat WaveFormat => format;
    }

}