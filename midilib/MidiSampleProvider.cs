using System;
using NAudio.Wave;
using MeltySynth;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

        public MidiSampleProvider()
        {
            mutex = new object();
        }

        public async Task<bool> Initialize(string soundFontPath)
        {
            //SoundFont
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(MidiPlayer.AwsBucketUrl + soundFontPath);
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            MemoryStream stream = new MemoryStream(bytes);
            SoundFont sf = new SoundFont(stream);

            synthesizer = new Synthesizer(sf, format.SampleRate);
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