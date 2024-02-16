using midilib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;
using midilib;
using System.Numerics;
using System;
using Amazon.S3.Model;
using Avalonia.Threading;

namespace midilonia
{
    public class MainViewModel : INotifyPropertyChanged
    {
        MidiDb db = App.Db;
        MidiPlayer player = App.Player;

        public new event PropertyChangedEventHandler? PropertyChanged;
        public IEnumerable<MidiDb.ArtistDef> Artists => db.Artists;

        MidiDb.ArtistDef currentArtist;
        public MidiDb.ArtistDef CurrentArtist
        {
            get => currentArtist;
            set
            {
                currentArtist = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArtistSongs)));
            }
        }
        public IEnumerable<string> ArtistSongs => CurrentArtist?.Songs;

        string currentSong;
        public string CurrentSong
        {
            get => currentSong;
            set { currentSong = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSong))); }
        }

        MeltySynth.MidiFile currentplayingSong = null;
        public long CurrentSongLength => (long)(currentplayingSong?.Length.TotalMilliseconds ?? 1);

        public long currentTime = 0;
        public long CurrentTime
        {
            get => currentTime;
            set
            {
                player.Seek(TimeSpan.FromMilliseconds(value));
                currentTime = value;
            }
        }


        public IEnumerable<SFDesc> SoundFonts => db.AllSoundFonts.Select(sf => new SFDesc(sf));

        SFDesc selectedSoundFont;
        public SFDesc SelectedSoundFont
        {
            get => selectedSoundFont;
            set {
                selectedSoundFont = value;
                player.ChangeSoundFont(value.Desc);
            }
        }

        public SFDesc CurrentSoundFont => new SFDesc(player.CurrentSoundFont);

        public MainViewModel()
        {
            Initialize();
        }
        private async Task<bool> Initialize()
        {
            //await db.UploadAWS();
            await db.InitializeMappings();
            db.InitSongList(false);

            await player.Initialize(OnEngineCreate);

            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackTime += Player_OnPlaybackTime;
            player.OnPlaybackComplete += Player_OnPlaybackComplete;
            player.OnSoundFontChanged += Player_OnSoundFontChanged;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Artists)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SoundFonts)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSoundFont)));
            return true;
        }

        private void Player_OnSoundFontChanged(object? sender, MidiDb.SoundFontDesc e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSoundFont)));
        }

        private void Player_OnPlaybackComplete(object? sender, bool e)
        {
        }

        private void Player_OnPlaybackTime(object? sender, MeltySynth.MidiSynthSequencer.PlaybackTimeArgs e)
        {
            currentTime = (long)e.timeSpan.TotalMilliseconds;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
        }

        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            currentplayingSong = e.midiFile;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSongLength)));
        }

        void OnEngineCreate(MidiSynthEngine midiSynthEngine)
        {
            App.OnEngineCreate(midiSynthEngine);
        }
    }

    public class SFDesc
    {
        MidiDb.SoundFontDesc sfd;
        public SFDesc(MidiDb.SoundFontDesc _sfd)
        {
            sfd = _sfd;
        }

        public string Name => sfd.Name;
        public MidiDb.SoundFontDesc Desc => sfd;
        public bool IsCached => sfd.IsCached;
    }
}
