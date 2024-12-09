using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using MeltySynth;
using NAudio.Midi;
using NAudio.Wave;
using System;
using System.Collections;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static MeltySynth.MidiSynthSequencer;

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

        public MidiSynthEngine SynthEngine => synthEngine;

        int volume = 100;
        public static string RootBucketUrl = "https://shanem.ddns.net/midisongs/";
        public static string MidiBucketUrl = RootBucketUrl + "midifiles/";
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
        public event EventHandler<MidiDb.SoundFontDesc> OnSoundFontChanged;

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
                PlaySong(currentPlayingSong, false, IsPaused);
                Seek(prevSongTime);
            }
            OnSoundFontChanged?.Invoke(this, soundFont);
            return true;
        }
     
        void SetSequencer(MidiSynthSequencer sequencer)
        {
            sequencer.OnPlaybackTime += Sequencer_OnPlaybackTime;
            sequencer.OnPlaybackComplete += Sequencer_OnPlaybackComplete;
            sequencer.OnPlaybackStart += Sequencer_OnPlaybackStart;
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
                this.LoadSong(db.GetSongByLocation(userSettings.PlayHistory.Last()), false);
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
            userSettings.PlayHistory.Add(mfi.Location);
            userSettings.Persist();
            try
            {
                currentPlayerMidifile = new MeltySynth.MidiFile(cacheFile);
                if (pianoMode)
                {
                    MidiSong song = new MidiSong(currentPlayerMidifile);
                    MidiSong pianoSong = song.ConvertToPianoSong();
                    currentPlayerMidifile = pianoSong.GetMidiFile();                   
                }
                OnSongLoaded?.Invoke(this, new PlaybackStartArgs() { file = mfi, midiFile = currentPlayerMidifile });
                synthEngine.Play(currentPlayerMidifile, false, 0);
                IsPaused = true;
                synthEngine.Sequencer.Pause(IsPaused);
            }
            catch (Exception e)
            {
            }
            return true;
        }

        public async void PlaySong(MidiDb.Fi mfi, bool pianoMode, bool startPaused)
        {
            await LoadSong(mfi, pianoMode);
            PauseOrUnPause(startPaused);
            //synthEngine.Play(currentPlayerMidifile, false);
        }

        public async void PlayExternalSong(MidiDb.Fi mfi)
        {
            string cacheFile = await db.GetLocalFile(mfi);
            userSettings.PlayHistory.Add(mfi.Location);
            userSettings.Persist();
            currentPlayerMidifile = new MeltySynth.MidiFile(cacheFile);
            MidiSong song = new MidiSong(currentPlayerMidifile);
            MemoryStream ms = new MemoryStream();
            if (song.Tracks.Length > 1)
            {
                MidiSong pianoSong = song.ConvertToPianoSong();
                pianoSong.SaveToStream(ms);
            }
            else
            {
                song.SaveToStream(ms);
            }
            ms.Close();
            byte[] data = ms.ToArray();
            string url = "http://192.168.1.6:18080";
            HttpClient client = new HttpClient();
            await client.GetAsync(url + "/stop");
            ByteArrayContent byteContent = new ByteArrayContent(data);
            byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            HttpResponseMessage response = await client.PostAsync(url+ "/midi_file", byteContent);
            currentPlayingSong = mfi;
        }

        public async void StopExternal()
        {
            string url = "http://192.168.1.6:18080";
            HttpClient client = new HttpClient();
            await client.GetAsync(url + "/stop");
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

     
        public void Dispose()
        {
            synthEngine.Dispose();
        }
    }

    public static class MidiSpec
    {
        public static int NoteOn = 0x90;
        public static int NoteOff = 0x80;
        public static int PatchChange = 0xC0;
    }

}
