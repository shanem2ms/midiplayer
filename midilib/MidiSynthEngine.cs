using MeltySynth;
using NAudio.Wave;
using System;
using System.Threading.Tasks;
using static midilib.MidiPlayer;

namespace midilib
{
    public class MidiSynthEngine : ISampleProvider
    {
        private static WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        private Synthesizer midiFileSynthesizer;
        private Synthesizer userSynthesizer;
        private MidiSynthSequencer sequencer;
        MidiOutSequencer midiOutSequencer;
        public delegate void OnProcessMidiMessageDel(int channel, int command, int data1, int data2);
        OnProcessMidiMessageDel onProcessMidiMessage;
        
        public MidiSynthSequencer Sequencer => sequencer;
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

        public void Dispose()
        {
            if (midiOutSequencer != null)
            {
                midiOutSequencer.Dispose();
            }
        }
        public async Task<bool> Initialize(string cacheFile)
        {
            SoundFont sf = new SoundFont(cacheFile);
            SynthesizerSettings settings = new SynthesizerSettings(format.SampleRate);
            //settings.EnableReverbAndChorus = false;
            midiFileSynthesizer = new Synthesizer(sf, settings);
            midiFileSynthesizer.MasterVolume = 1.0f;
            sequencer = new MidiSynthSequencer(midiFileSynthesizer);
            userSynthesizer = new Synthesizer(sf, settings);
            userSynthesizer.MasterVolume = 1.0f;
            return true;
        }
        public void Play(MeltySynth.MidiFile midiFile, bool startPaused, int currentTicks)
        {
            lock (mutex)
            {
                sequencer.Play(midiFile, startPaused, currentTicks);
                if (midiOutSequencer != null)
                {
                    midiOutSequencer.Play(midiFile, startPaused);
                }
            }
        }

        public void SetChannelEnabled(int channel, bool enabled) 
        {
            midiFileSynthesizer.SetChannelEnabled(channel, enabled);
        }
        public bool Pause(bool pause)
        {
            bool oldState = sequencer.IsPaused;
            if (pause != sequencer.IsPaused)
            {
                lock (mutex)
                {
                    sequencer.Pause(pause);
                }
            }
            return oldState;
        }

        public void Stop()
        {
            lock (mutex)
            {
                sequencer?.Stop();
            }
        }

        public void SetMidiOut(OnProcessMidiMessageDel del)
        {
            onProcessMidiMessage = del;
            midiOutSequencer = new MidiOutSequencer(OnProcessMidiMessageHandler);
        }

        void OnProcessMidiMessageHandler(int channel, int command, int data1, int data2)
        {
            if (onProcessMidiMessage != null)
                onProcessMidiMessage(channel, command, data1, data2);
            /*
            OnChannelEvent?.Invoke(this, new ChannelEvent()
            {
                channel = channel,
                command = command,
                data1 = data1,
                data2 = data2
            });*/

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
                    buffer[i + offset] += tempBuffer[i];
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

        public void SetPatch(int patch)
        {
            userSynthesizer.SetChannelPatch(0, patch);
        }

        public WaveFormat WaveFormat => format;
    }

}