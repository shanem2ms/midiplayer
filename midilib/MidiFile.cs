using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Midi;
using Newtonsoft.Json;
using System.Xml;
using System.Numerics;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace midiplayer
{

    public class MidiFI
    {
        public string Name { get; }

        string nameLower;
        public string NmLwr => nameLower;
        public string Location { get; }
        public MidiFI(string name) :
            this(name, null)
        {
        }

        public MidiFI(string name, string location)
        {
            Name = name;
            nameLower = name.ToLower();
            Location = location;
        }
    }

    public class MidiPlayer
    {
        string GetHomeDir()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return documents;
        }
        string CacheDir => Path.Combine(homedir, "cache");

        MidiSampleProvider player;

        MidiOut midiOut;
        string homedir;
        string midiCacheDir;
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
        public List<string> AllSoundFonts { get; } = new List<string>();
        public string SearchStr
        {
            get => searchStr;
            set
            {
                searchStr = value;
            }
        }

        public string CurrentSoundFont
        {
            get => currentSoundFont;
            set
            {
                currentSoundFont = value;
                ChangeSoundFont(currentSoundFont);
            }
        }

        string currentSoundFont = "TimGM6mb.sf2";

        public class ChannelEvent
        {
            public int channel;
            public int data;
        }
        public event EventHandler<ChannelEvent> OnChannelEvent;
        public event EventHandler<TimeSpan> OnPlaybackTime;
        public event EventHandler<bool> OnPlaybackComplete;
        public event EventHandler<TimeSpan> OnPlaybackStart;

        public MidiFI GetNextSong()
        {
            Random r = new Random();
            int rVal = r.Next(this.midiFiles.Length);
            return this.midiFiles[rVal];
        }
        public MidiPlayer()
        {
            homedir = GetHomeDir();
            midiCacheDir = Path.Combine(homedir, "midi");
            if (!Directory.Exists(midiCacheDir))
                Directory.CreateDirectory(midiCacheDir);
            player = new MidiSampleProvider();
        }

        async Task<bool> ChangeSoundFont(string soundFont)
        {
            player.Stop();
            await player.Initialize(soundFont, homedir);
            SetSequencer(player.Sequencer);
            return true;
        }

        void SetSequencer(MeltySynth.MidiFileSequencer sequencer)
        {
            sequencer.OnPlaybackTime += Sequencer_OnPlaybackTime;
            sequencer.OnPlaybackComplete += Sequencer_OnPlaybackComplete;
            sequencer.OnPlaybackStart += Sequencer_OnPlaybackStart;
            player.Sequencer.OnProcessMidiMessage = OnProcessMidiMessage;
        }

        private void Sequencer_OnPlaybackStart(object sender, TimeSpan e)
        {
            OnPlaybackStart?.Invoke(sender, e);
        }

        private void Sequencer_OnPlaybackComplete(object sender, bool e)
        {
            OnPlaybackComplete?.Invoke(sender, e);
        }

        private void Sequencer_OnPlaybackTime(object sender, TimeSpan e)
        {
            OnPlaybackTime?.Invoke(sender, e);
        }

        public async Task<bool> Initialize(OnAudioEngineCreateDel OnAudioEngineCreate)
        {
            {
                var listResponse = await httpClient.GetAsync("https://midisongs.s3.us-east-2.amazonaws.com/?list-type=2&prefix=sf/");
                string xmlstr = await listResponse.Content.ReadAsStringAsync();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlstr);
                var root = doc.DocumentElement;
                foreach (XmlElement node in root.ChildNodes)
                {
                    if (node.Name == "Contents")
                    {
                        AllSoundFonts.Add(
                            node.FirstChild.InnerText.Substring(3));
                    }
                }
            }

            await ChangeSoundFont(currentSoundFont);
            OnAudioEngineCreate(player);

            string mappingsFile = Path.Combine(homedir, "mappings.json");
            if (!File.Exists(mappingsFile))
            {
                var response = await httpClient.GetAsync(AwsBucketUrl + "mappings.json");
                Stream inputstream = await response.Content.ReadAsStreamAsync();
                inputstream.Seek(0, SeekOrigin.Begin);
                FileStream fs = File.OpenWrite(mappingsFile);
                inputstream.CopyTo(fs);
                fs.Close();
            }
            string jsonstr = await File.ReadAllTextAsync(mappingsFile);
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonstr);
            List<MidiFI> midFileLsit = new List<MidiFI>();
            foreach (var kv in result)
            {
                string name = kv.Key;
                string url = kv.Value;
                midFileLsit.Add(new MidiFI(name, kv.Value));
            }
           
            this.midiFiles = midFileLsit.ToArray();
            return true;
        }
        public void SetVolume(int volume)
        {
            player.SetVolume(volume);
        }
        public async void PlaySong(MidiFI mfi)
        {
            string cacheFile = Path.Combine(midiCacheDir, mfi.Location);
            if (!File.Exists(cacheFile))
            {
                Path.GetDirectoryName(cacheFile);
                if (!Directory.Exists(cacheFile))
                    Directory.CreateDirectory(cacheFile);

                var response = await httpClient.GetAsync(AwsBucketUrl + mfi.Location);
                Stream inputstream = await response.Content.ReadAsStreamAsync();
                inputstream.Seek(0, SeekOrigin.Begin);
                FileStream fs = File.OpenWrite(cacheFile);
                inputstream.CopyTo(fs);
                fs.Close();
            }

            player.Play()
            else
            {
                PlayHttpFile(new Uri(AwsBucketUrl + mfi.Location)).ContinueWith((action) =>
                    {
                        var midifile = action.Result;
                        player.Play(midifile);
                    });
            }
        }

        async Task<MeltySynth.MidiFile> PlayHttpFile(Uri url)
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
