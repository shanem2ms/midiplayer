using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AVAudioEngineOut = NAudio.Wave.WaveOut;
using NAudio.Wave;
using NAudio.Midi;
using midiplayer;

namespace midiplayer
{
    using PlayerAv;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using Tmds.DBus;

    public class MidiFI
    {
        public string Name { get; }
        // https://bitmidi.com/
        public MidiFI(string name) :
            this(name, null)
        {
        }

        public MidiFI(string name, string url)
        {
            Name = name;
        }
    }

    public class MidiPlayer
    {
        string GetHomeDir()
        {
            DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (di.Name.ToLower() != "midiplayer")
            {
                di = di.Parent;
            }
            return di.FullName;
        }
        string PlaylistDir => Path.Combine(homedir, "Playlist");

        //NAudio.Wave.AVAudioEngineOut aVAudioEngineOut;
        MidiSampleProvider player;
        AVAudioEngineOut aVAudioEngineOut;

        MidiOut midiOut;
        string homedir;
        public List<MidiFI> bitMidiFiles = new List<MidiFI>();
        int volume = 100;
        public List<MidiFI> jazzMidiFiles = new List<MidiFI>();
        HttpClient httpClient = new HttpClient();

        public class ChannelEvent
        {
            public int channel;
            public int data;
        }
        public event EventHandler<ChannelEvent> OnChannelEvent;
        public event EventHandler<TimeSpan> OnPlaybackTime;
        public event EventHandler<bool> OnPlaybackComplete;
        public MidiPlayer()
        {
            homedir = GetHomeDir();
            player = new MidiSampleProvider(Path.Combine(homedir, "TimGM6mb.sf2"));
            player.Sequencer.OnPlaybackTime += OnPlaybackTime;
            player.Sequencer.OnPlaybackComplete += OnPlaybackComplete;
            player.Sequencer.OnProcessMidiMessage = OnProcessMidiMessage;

            var filestream = File.OpenRead(Path.Combine(homedir, "mappings.json"));
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(filestream);
            foreach (var item in result!.Keys)
            {
                bitMidiFiles.Add(new MidiFI(item, "https://bitmidi.com/"));
            }

#if WIN
            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.Init(player);
            waveOut.Play();
            
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                var devinfo = MidiOut.DeviceInfo(device).ProductName;
            }
#else
            //midiOut = new MidiOut(0);
            aVAudioEngineOut = new AVAudioEngineOut();
            aVAudioEngineOut.Init(player);
            aVAudioEngineOut.Play();

#endif
            DirectoryInfo di = new DirectoryInfo(PlaylistDir);
            foreach (FileInfo fi in di.GetFiles("*.mid"))
            {
                jazzMidiFiles.Add(new MidiFI(fi.Name));
            }

        }

        private void Sequencer_OnPlaybackTime(object? sender, TimeSpan e)
        {
            throw new NotImplementedException();
        }

        private void Sequencer_OnPlaybackComplete(object? sender, bool e)
        {
            throw new NotImplementedException();
        }

        public void SetVolume(int volume)
        {
            player.SetVolume(volume);
        }
        public void PlaySong(MidiFI mfi)
        {
            string path = Path.Combine(PlaylistDir, mfi.Name);
            // Load the MIDI file.
            var midiFile = new MeltySynth.MidiFile(path);
            player.Play(midiFile);
        }

        async Task<MeltySynth.MidiFile> PlayFile()
        {
            Uri uri = new Uri(@"https://bushgrafts.com/jazz/AintMisbehavin.MID");

            var response = await httpClient.GetAsync(uri);
            Stream stream = response.Content.ReadAsStream();
            return new MeltySynth.MidiFile(stream);
        }


        void OnProcessMidiMessage(int channel, int command, int data1, int data2)
        {
            //var channelInfo = channels[channel];
            switch (command)
            {
                case 0x80: // Note Off
                    {
                        int cmd = channel | command | (data1 << 8) | (data2 << 16);
                        midiOut?.Send(cmd);
                    }
                    break;

                case 0x90: // Note On
                    {
                        int vol = (data2 * volume) / 100;
                        int cmd = channel | command | (data1 << 8) | (vol << 16);
                        midiOut?.Send(cmd);
                        OnChannelEvent?.Invoke(this, new ChannelEvent() { channel = channel, data = data1 });
                        break;
                    }
                default:
                    {
                        int cmd = channel | command | (data1 << 8) | (data2 << 16);
                        midiOut?.Send(cmd);
                    }
                    break;

            }

        }

    }
}
