﻿using System;
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
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return documents;
        }
        string CacheDir => Path.Combine(homedir, "cache");

        MidiSampleProvider player;

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
            player = new MidiSampleProvider();
        }

        async void ChangeSoundFont(string soundFont)
        {
            player.Stop();
            await player.Initialize(soundFont, homedir);
            SetSequencer(player.Sequencer);
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

            ChangeSoundFont(currentSoundFont);
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
