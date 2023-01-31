using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Midi;
using System.Xml;
using MeltySynth;

namespace midilib
{
    public class MidiPlayer
    {
        string CacheDir => Path.Combine(homedir, "cache");

        MidiSampleProvider sampleProvider;

        MidiOut midiOut;
        string homedir;
        MidiDb db;
        public MidiDb Db => db;
        MidiDb.Fi currentPlayingSong;
        UserSettings userSettings;


        public MidiDb.Fi CurrentPlayingSong => currentPlayingSong;

        public delegate void OnAudioEngineCreateDel(MidiSampleProvider midiSampleProvider);
        public delegate void OnProcessMidiMessageDel(int channel, int command, int data1, int data2);
        public OnProcessMidiMessageDel OnProcessMidiMessage;

        int volume = 100;
        public static string AwsBucketUrl = "https://midisongs.s3.us-east-2.amazonaws.com/";
        public MidiDb.SoundFontDesc CurrentSoundFont
        {
            get => db.SFDescFromName(userSettings.CurrentSoundFont);
            set => userSettings.CurrentSoundFont = value.Name;
        }

        public class ChannelEvent
        {
            public int channel;
            public int command;
            public int data1;
            public int data2;
        }
        public event EventHandler<ChannelEvent> OnChannelEvent;
        public event EventHandler<TimeSpan> OnPlaybackTime;
        public event EventHandler<bool> OnPlaybackComplete;
        public struct PlaybackStartArgs
        {
            public MidiDb.Fi file;
            public MeltySynth.MidiFile midiFile;

        }
        public event EventHandler<PlaybackStartArgs> OnPlaybackStart;

        public TimeSpan CurrentSongTime => sampleProvider.Sequencer?.CurrentTime ?? new TimeSpan();

        public MidiPlayer(MidiDb dbin)
        {
            db = dbin;
            homedir = db.HomeDir;
            sampleProvider = new MidiSampleProvider();
            userSettings = UserSettings.FromFile(Path.Combine(homedir, "usersettings.json"));
        }

        public async Task<bool> ChangeSoundFont(MidiDb.SoundFontDesc soundFont)
        {
            TimeSpan prevSongTime = CurrentSongTime;
            sampleProvider.Stop();
            string soundFontCacheFile = await db.InstallSoundFont(soundFont);
            await sampleProvider.Initialize(soundFontCacheFile);
            SetSequencer(sampleProvider.Sequencer);
            userSettings.CurrentSoundFont = soundFont.Name;
            userSettings.Persist();
            if (currentPlayingSong != null)
            {
                PlaySong(currentPlayingSong);
                Seek(prevSongTime);
            }
            return true;
        }

        void SetSequencer(MidiFileSequencer sequencer)
        {
            sequencer.OnPlaybackTime += Sequencer_OnPlaybackTime;
            sequencer.OnPlaybackComplete += Sequencer_OnPlaybackComplete;
            sequencer.OnPlaybackStart += Sequencer_OnPlaybackStart;
            sampleProvider.Sequencer.OnProcessMidiMessage = OnProcessMidiMessageHandler;
        }

        private void Sequencer_OnPlaybackStart(object sender, MeltySynth.MidiFile midiFile)
        {
            OnPlaybackStart?.Invoke(sender, new PlaybackStartArgs() { file = this.currentPlayingSong,
            midiFile = midiFile });
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
            await ChangeSoundFont(
                db.SFDescFromName(userSettings.CurrentSoundFont));
            OnAudioEngineCreate(sampleProvider);
            if (userSettings.PlayHistory.Count > 0)
            {
                await db.Initialized;
                this.PlaySong(db.GetSongByName(userSettings.PlayHistory.Last()));
            }
            return true;
        }

        public MidiDb.Fi GetNextSong()
        {
            return db.GetRandomSong();
        }
        public void SetVolume(int volume)
        {
            sampleProvider.SetVolume(volume);
        }
        public async void PlaySong(MidiDb.Fi mfi)
        {
            currentPlayingSong = mfi;
            string cacheFile = await db.GetLocalFile(mfi);
            userSettings.PlayHistory.Add(mfi.Name);
            userSettings.Persist();
            MeltySynth.MidiFile midiFile = new MeltySynth.MidiFile(cacheFile);
            sampleProvider.Play(midiFile);
        }

        public void Seek(TimeSpan time)
        {
            sampleProvider.Sequencer.SeekTo(time);
        }

        void OnProcessMidiMessageHandler(int channel, int command, int data1, int data2)
        {
            OnChannelEvent?.Invoke(this, new ChannelEvent() { channel = channel, command = command,
                data1 = data1, data2 = data2});
            OnProcessMidiMessage?.Invoke(channel, command, data1, data2);
        }

    }

    public static class MidiSpec
    {
        public static int NoteOn = 0x90;
        public static int NoteOff = 0x80;
        public static int PatchChange = 0xC0;
    }

}
