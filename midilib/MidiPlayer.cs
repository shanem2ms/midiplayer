using MeltySynth;
using NAudio.Midi;
using NAudio.Wave;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static MeltySynth.MidiFileSequencer;

namespace midilib
{
    public class MidiPlayer
    {
        string CacheDir => Path.Combine(homedir, "cache");

        MidiSynthEngine synthEngine;

        MidiOut midiOut;
        string homedir;
        MidiDb db;
        public MidiDb Db => db;
        MidiDb.Fi currentPlayingSong;
        MeltySynth.MidiFile currentPlayerMidifile;
        UserSettings userSettings;
        public bool IsPaused { get; set; }

        public MidiDb.Fi CurrentPlayingSong => currentPlayingSong;

        public delegate void OnAudioEngineCreateDel(MidiSynthEngine midisynthEngine);
        public delegate void OnProcessMidiMessageDel(int channel, int command, int data1, int data2);
        public OnProcessMidiMessageDel OnProcessMidiMessage;

        public MidiSynthEngine SynthEngine => synthEngine;

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
        public event EventHandler<PlaybackTimeArgs> OnPlaybackTime;
        public event EventHandler<bool> OnPlaybackComplete;
        public struct PlaybackStartArgs
        {
            public MidiDb.Fi file;
            public MeltySynth.MidiFile midiFile;

        }
        public event EventHandler<PlaybackStartArgs> OnPlaybackStart;
        public event EventHandler<PlaybackStartArgs> OnSongLoaded;

        public TimeSpan CurrentSongTime => synthEngine.Sequencer?.CurrentTime ?? new TimeSpan();

        public MidiPlayer(MidiDb dbin)
        {
            db = dbin;
            homedir = db.HomeDir;
            synthEngine = new MidiSynthEngine();
            userSettings = UserSettings.FromFile(Path.Combine(homedir, "usersettings.json"));
        } 

        public async Task<bool> ChangeSoundFont(MidiDb.SoundFontDesc soundFont)
        {
            TimeSpan prevSongTime = CurrentSongTime;
            synthEngine.Stop();
            string soundFontCacheFile = await db.InstallSoundFont(soundFont);
            await synthEngine.Initialize(soundFontCacheFile);
            SetSequencer(synthEngine.Sequencer);
            userSettings.CurrentSoundFont = soundFont.Name;
            userSettings.Persist();
            if (currentPlayingSong != null)
            {
                PlaySong(currentPlayingSong, false);
                Seek(prevSongTime);
            }
            return true;
        }

        void SetSequencer(MidiFileSequencer sequencer)
        {
            sequencer.OnPlaybackTime += Sequencer_OnPlaybackTime;
            sequencer.OnPlaybackComplete += Sequencer_OnPlaybackComplete;
            sequencer.OnPlaybackStart += Sequencer_OnPlaybackStart;
            synthEngine.Sequencer.OnProcessMidiMessage = OnProcessMidiMessageHandler;
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

        private void Sequencer_OnPlaybackTime(object sender, PlaybackTimeArgs e)
        {
            OnPlaybackTime?.Invoke(sender, e);
        }

        public async Task<bool> Initialize(OnAudioEngineCreateDel OnAudioEngineCreate)
        {
            await ChangeSoundFont(
                db.SFDescFromName(userSettings.CurrentSoundFont));
            OnAudioEngineCreate(synthEngine);
            if (userSettings.PlayHistory.Count > 0)
            {
                await db.Initialized;
                this.LoadSong(db.GetSongByName(userSettings.PlayHistory.Last()), true);
            }
            return true;
        }

        public MidiDb.Fi GetNextSong()
        {
            return db.GetRandomSong();
        }
        public void SetVolume(int volume)
        {
            synthEngine.SetVolume(volume);
        }
        public async Task<bool> LoadSong(MidiDb.Fi mfi, bool pianoMode)
        {
            currentPlayingSong = mfi;
            string cacheFile = await db.GetLocalFile(mfi);
            userSettings.PlayHistory.Add(mfi.Name);
            userSettings.Persist();
            try
            {
                currentPlayerMidifile = new MeltySynth.MidiFile(cacheFile, pianoMode ? MeltySynth.MidiFile.InstrumentType.Piano :
                    MeltySynth.MidiFile.InstrumentType.Original);
                OnSongLoaded?.Invoke(this, new PlaybackStartArgs() { file = mfi, midiFile = currentPlayerMidifile });
                synthEngine.Play(currentPlayerMidifile, true);
                IsPaused = true;
            }
            catch (Exception e)
            {
            }
            return true;
        }
        public async void PlaySong(MidiDb.Fi mfi, bool pianoMode)
        {
            await LoadSong(mfi, pianoMode);
            synthEngine.Play(currentPlayerMidifile, false);
        }

        public void PauseOrUnPause(bool pause)
        {
            IsPaused = pause;
            if (pause)
            {
                synthEngine.Sequencer.Pause(true);
            }
            else
            {
                synthEngine.Sequencer.Pause(false);
            }
        }

        public void Seek(TimeSpan time)
        {
            synthEngine.Sequencer.SeekTo(time);
        }

        public void Seek(int ticks)
        {
            TimeSpan time = synthEngine.Sequencer.TicksToTime(ticks);
            Seek(time);
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
