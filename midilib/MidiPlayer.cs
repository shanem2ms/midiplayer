﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Midi;
using System.Xml;

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
        UserSettings userSettings;

        public delegate void OnAudioEngineCreateDel(MidiSampleProvider midiSampleProvider);
        public delegate void OnProcessMidiMessageDel(int channel, int command, int data1, int data2);
        public OnProcessMidiMessageDel OnProcessMidiMessage;

        int volume = 100;
        public static string AwsBucketUrl = "https://midisongs.s3.us-east-2.amazonaws.com/";
        public string CurrentSoundFont
        {
            get => userSettings.CurrentSoundFont;
            set => userSettings.CurrentSoundFont = value;
        }

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
            userSettings = UserSettings.FromFile(Path.Combine(homedir, "usersettings.json"));
        }

        public async Task<bool> ChangeSoundFont(string soundFont)
        {
            player.Stop();
            await player.Initialize(soundFont, homedir);
            SetSequencer(player.Sequencer);
            userSettings.CurrentSoundFont = soundFont;
            userSettings.Persist();
            return true;
        }

        void SetSequencer(MeltySynth.MidiFileSequencer sequencer)
        {
            sequencer.OnPlaybackTime += Sequencer_OnPlaybackTime;
            sequencer.OnPlaybackComplete += Sequencer_OnPlaybackComplete;
            sequencer.OnPlaybackStart += Sequencer_OnPlaybackStart;
            player.Sequencer.OnProcessMidiMessage = OnProcessMidiMessageHandler;
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
            await ChangeSoundFont(userSettings.CurrentSoundFont);
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

        public void Seek(TimeSpan time)
        {
            player.Sequencer.SeekTo(time);
        }

        void OnProcessMidiMessageHandler(int channel, int command, int data1, int data2)
        {
            OnProcessMidiMessage?.Invoke(channel, command, data1, data2);
        }

    }
}