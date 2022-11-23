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

namespace midilib
{


    public class MidiPlayer
    {
        string CacheDir => Path.Combine(homedir, "cache");

        MidiSampleProvider player;

        MidiOut midiOut;
        string homedir;
        MidiDb db;
        public MidiDb Db => db;
        MidiDb.Fi currentPlayingSong;

        public delegate void OnAudioEngineCreateDel(MidiSampleProvider midiSampleProvider);
        int volume = 100;
        public static string AwsBucketUrl = "https://midisongs.s3.us-east-2.amazonaws.com/";
        public List<string> AllSoundFonts { get; } = new List<string>();

        public string CurrentSoundFont
        {
            get => currentSoundFont;
            set
            {
                currentSoundFont = value;
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
        public struct PlaybackStartArgs
        {
            public TimeSpan timeSpan;
            public MidiDb.Fi file;
        }
        public event EventHandler<PlaybackStartArgs> OnPlaybackStart;

        public MidiPlayer(MidiDb dbin)
        {
            db = dbin;
            homedir = db.HomeDir;
            player = new MidiSampleProvider();
        }

        public async Task<bool> ChangeSoundFont(string soundFont)
        {
            player.Stop();
            await player.Initialize(soundFont, homedir);
            SetSequencer(player.Sequencer);
            this.currentSoundFont = soundFont;
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
            OnPlaybackStart?.Invoke(sender, new PlaybackStartArgs() { file = this.currentPlayingSong,
            timeSpan = e});
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
                HttpClient httpClient = new HttpClient();
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
            return true;
        }

        public void SetVolume(int volume)
        {
            player.SetVolume(volume);
        }
        public async void PlaySong(MidiDb.Fi mfi)
        {
            currentPlayingSong = mfi;
            string cacheFile = await db.GetLocalFile(mfi);
            MeltySynth.MidiFile midiFile = new MeltySynth.MidiFile(cacheFile);
            player.Play(midiFile);
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
