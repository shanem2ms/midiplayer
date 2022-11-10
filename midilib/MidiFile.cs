using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Midi;
using Newtonsoft.Json;

namespace midiplayer
{

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
            return di.FullName;
        }
        string CacheDir => Path.Combine(homedir, "cache");

        MidiSampleProvider player;
        //AVAudioEngineOut aVAudioEngineOut;

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

        public delegate void OnAudioEngineCreateDel(MidiSampleProvider midiSampleProvider);
        int volume = 100;
        HttpClient httpClient = new HttpClient();
        public string searchStr;
        public static string AwsBucketUrl = "https://midisongs.s3.us-east-2.amazonaws.com/";
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
            player = new MidiSampleProvider();
        }

        public async Task<bool> Initialize(OnAudioEngineCreateDel OnAudioEngineCreate)
        {
            await player.Initialize("TimGM6mb.sf2", homedir);
            player.Sequencer.OnProcessMidiMessage = OnProcessMidiMessage;
            OnAudioEngineCreate(player);

            var response = await httpClient.GetAsync(AwsBucketUrl + "mappings.json");
            string jsonstr = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonstr);
            List<MidiFI> midFileLsit = new List<MidiFI>();
            foreach (var kv in result)
            {
                string name = kv.Key;
                string url = AwsBucketUrl + kv.Value;
                string filename = Path.GetFileName(kv.Value);
                midFileLsit.Add(new MidiFI(name, new Uri(url)));
            }
           
            this.midiFiles = midFileLsit.ToArray();
            return true;
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
                string path = Path.Combine(CacheDir, mfi.Name);
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
