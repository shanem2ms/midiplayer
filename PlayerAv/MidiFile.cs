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
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using Tmds.DBus;

    public class MidiFI
    {
        public string Name { get; }

        string nameLower;
        public string NmLwr => nameLower;
        public Uri Url { get; }
        public MidiFI(string name) :
            this(name, null)
        {
        }

        public MidiFI(string name, Uri url)
        {
            Name = name;
            nameLower = name.ToLower();
            Url = url;
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
        MidiFI[] midiFiles;

        public IEnumerable<MidiFI> FilteredMidiFiles
        {
            get
            {
                if (searchStr != null &
                    searchStr?.Trim().Length > 2)
                {
                    string ssLower = searchStr.ToLower();
                    return midiFiles.Where(fi => fi.NmLwr.Contains(ssLower));
                }
                else
                    return midiFiles;
            }

        }

        int volume = 100;
        HttpClient httpClient = new HttpClient();
        public string searchStr;
        public string SearchStr
        {
            get => searchStr;
            set
            {
                searchStr = value;
            }
        }

        public class ChannelEvent
        {
            public int channel;
            public int data;
        }
        public event EventHandler<ChannelEvent> OnChannelEvent;
        public event EventHandler<TimeSpan> OnPlaybackTime
        {
            add { player.Sequencer.OnPlaybackTime += value; }
            remove { player.Sequencer.OnPlaybackTime -= value; }
        }

        public event EventHandler<bool> OnPlaybackComplete
        {
            add { player.Sequencer.OnPlaybackComplete += value; }
            remove { player.Sequencer.OnPlaybackComplete -= value; }
        }
        public event EventHandler<TimeSpan> OnPlaybackStart
        {
            add { player.Sequencer.OnPlaybackStart += value; }
            remove { player.Sequencer.OnPlaybackStart -= value; }
        }

        public MidiFI GetNextSong()
        {
            Random r = new Random();
            int rVal = r.Next(this.midiFiles.Length);
            return this.midiFiles[rVal];
        }
        public MidiPlayer()
        {
            homedir = GetHomeDir();
            player = new MidiSampleProvider(Path.Combine(homedir, "TimGM6mb.sf2"));
            player.Sequencer.OnProcessMidiMessage = OnProcessMidiMessage;

            var filestream = File.OpenRead(Path.Combine(homedir, "mappings.json"));
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(filestream);
            string bitmididir = Path.Combine(PlaylistDir, "bitmidi");
            List<MidiFI> midFileLsit = new List<MidiFI>();
            foreach (var kv in result)
            {
                string name = kv.Key.Substring(1);
                string url = "https://bitmidi.com" + kv.Value;
                string filename = Path.GetFileName(kv.Value);
                midFileLsit.Add(new MidiFI(name, new Uri(Path.Combine(bitmididir, filename))));
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
            var filelist = di.GetFiles("*.mid");
            foreach (FileInfo fi in filelist)
            {
                midFileLsit.Add(new MidiFI(fi.Name));
            }

            this.midiFiles = midFileLsit.ToArray();
        }
        public void SetVolume(int volume)
        {
            player.SetVolume(volume);
        }
        public void PlaySong(MidiFI mfi)
        {
            if (mfi.Url != null)
            {
                if (mfi.Url.IsFile)
                {
                    var midiFile = new MeltySynth.MidiFile(mfi.Url.LocalPath);
                    player.Play(midiFile);
                }
                else
                {
                    PlayFile(mfi.Url).ContinueWith((action) =>
                        {
                            var midifile = action.Result;
                            player.Play(midifile);
                        });
                }
            }
            else
            {
                string path = Path.Combine(PlaylistDir, mfi.Name);
                // Load the MIDI file.
                var midiFile = new MeltySynth.MidiFile(path);
                player.Play(midiFile);
            }
        }

        async Task<MeltySynth.MidiFile> PlayFile(Uri url)
        {
            var response = await httpClient.GetAsync(url);
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            MemoryStream stream = new MemoryStream(bytes);
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
