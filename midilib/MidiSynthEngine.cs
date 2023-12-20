using MeltySynth;
using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace midilib
{
    public class MidiSynthEngine : ISampleProvider
    {
        private static WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        private Synthesizer midiFileSynthesizer;
        private Synthesizer userSynthesizer;
        private MidiFileSequencer sequencer;

        public MidiFileSequencer Sequencer => sequencer;
        private float rms = 0;
        public float RMS => rms;


        private object mutex;

        public void SetVolume(int volume)
        {
            midiFileSynthesizer.Volume = volume;
            userSynthesizer.Volume = volume;
        }

        public MidiSynthEngine()
        {
            mutex = new object();
        }

        public async Task<bool> Initialize(string cacheFile)
        {                        
            SoundFont sf = new SoundFont(cacheFile);
            SynthesizerSettings settings = new SynthesizerSettings(format.SampleRate);
            //settings.EnableReverbAndChorus = false;
            midiFileSynthesizer = new Synthesizer(sf, settings);
            midiFileSynthesizer.MasterVolume = 1.0f;
            sequencer = new MidiFileSequencer(midiFileSynthesizer);
            userSynthesizer = new Synthesizer(sf, settings);
            userSynthesizer.MasterVolume = 1.0f;
            return true;
        }
        public void Play(MeltySynth.MidiFile midiFile, bool startPaused)
        {
            lock (mutex)
            {
                sequencer.Play(midiFile, startPaused);
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

        float[] tempBuffer = null; 
        public int Read(float[] buffer, int offset, int count)
        {
            if (tempBuffer == null || tempBuffer.Length < count)
            {
                tempBuffer = new float[count];
            }
            lock (mutex)
            {
                sequencer.RenderInterleaved(buffer.AsSpan(offset, count));
                userSynthesizer.RenderInterleaved(tempBuffer);
                for (int i = 0; i < count; ++i)
                {
                    buffer[i+offset] += tempBuffer[i];
                }
            }

            rms = CalculateRms(buffer, offset, count);
            return count;
        }

        public void NoteOn(int key, int velocity)
        {
            userSynthesizer.NoteOn(0, key, velocity);
        }

        public void NoteOff(int key)
        {
            userSynthesizer.NoteOff(0, key);
        }

        public WaveFormat WaveFormat => format;
    }

}