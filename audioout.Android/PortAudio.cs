using NAudio.Wave;
using Android.App;
using Android.OS;
using Android.Media;
using Android.Runtime;
using System.Collections;

namespace audioout.Droid
{
    class PortAudio : IWavePlayer
    {
        private AudioTrack audioTrack;
        IWaveProvider m_WaveProvider;
        private bool isPlaying = true;
        private Thread playbackThread;
        public float Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public PlaybackState PlaybackState { get; private set; }
        public WaveFormat OutputWaveFormat => throw new NotImplementedException();

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Init(IWaveProvider waveProvider)
        {
            PlaybackState = PlaybackState.Stopped;
            m_WaveProvider = waveProvider;
            int sampleRate = m_WaveProvider.WaveFormat.SampleRate;            
            ChannelOut channelConfig = 
                m_WaveProvider.WaveFormat.Channels == 2 ? ChannelOut.Stereo : ChannelOut.Mono;            
            Encoding audioFormat = m_WaveProvider.WaveFormat.BitsPerSample == 32 ? Encoding.PcmFloat : Encoding.Pcm16bit;
            int bufferSize = AudioTrack.GetMinBufferSize(sampleRate, channelConfig, audioFormat);

            // Initialize AudioTrack
            audioTrack = new AudioTrack(
                // Using music stream type for playback
                Android.Media.Stream.Music,
                sampleRate,
                channelConfig,
                audioFormat,
                bufferSize,
                AudioTrackMode.Stream);

            // Start AudioTrack
            audioTrack.Play();
        }
        
        private void StartPlayback()
        {
            playbackThread = new Thread(() =>
            {
                // Example: Generate a sine wave for playback
                float[] flbuffer = new float[2048];
                byte[] buffer = new byte[flbuffer.Length * sizeof(float)];

                while (PlaybackState != PlaybackState.Stopped)
                {
                    if (PlaybackState == PlaybackState.Paused)
                    {
                        Thread.Sleep(5);
                    }
                    else
                    {
                        m_WaveProvider.Read(buffer, 0, buffer.Length);
                        Buffer.BlockCopy(buffer, 0, flbuffer, 0, buffer.Length);
                        audioTrack.Write(flbuffer, 0, flbuffer.Length, WriteMode.NonBlocking);
                    }
                }
            });

            playbackThread.Start();
        }
/*
        public void StartPlayback()
        {
            playbackThread = new Thread(() =>
            {
                // Example: Generate a stereo sine wave for playback
                float[] buffer = new float[2048]; // Stereo: two samples per frame
                double frequency = 440.0; // A4 note
                double sampleRate = 44100.0;
                double increment = 2.0 * Math.PI * frequency / sampleRate;
                double angle = 0.0;

                while (isPlaying)
                {
                    for (int i = 0; i < buffer.Length; i += 2)
                    {
                        float sample = (float)Math.Sin(angle) * 0.5f; // Volume scaling
                        buffer[i] = sample;     // Left channel
                        buffer[i + 1] = sample; // Right channel

                        angle += increment;
                        if (angle > 2.0 * Math.PI)
                        {
                            angle -= 2.0 * Math.PI;
                        }
                    }

                    audioTrack.Write(buffer, 0, buffer.Length, WriteMode.Blocking);
                }
            });

            playbackThread.Start();
        }
*/
        public void Pause()
        {
            PlaybackState = PlaybackState.Paused;
        }

        public void Play()
        {
            if (PlaybackState == PlaybackState.Stopped)
            {
                PlaybackState = PlaybackState.Playing;
                StartPlayback();
            }
        }

        public void Stop()
        {
            PlaybackState = PlaybackState.Stopped;
        }
    }
}
