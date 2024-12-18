using Amazon.S3.Model;
using Avalonia.Media;
using Avalonia.Threading;
using midilib;
using midilonia.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static midilib.MidiSong;

namespace midilonia
{
    public class SequencerModel : INotifyPropertyChanged
    {
        MidiPlayer player = App.Player;
        static public int PixelsPerSixteenth = 10;        
        double pixelsPerTick;
        DispatcherTimer dispatcherTimer;
        double CursorPosX { get; set; }
        public string SongKey { get; set; }
        ChordAnalyzer chordAnalyzer;
        MidiSong midiSong;
        public event PropertyChangedEventHandler? PropertyChanged;
        int currentTicks = 0;

        List<ChannelCtrl> channelCtrls = null;
        public MidiSong MidiSong => midiSong;
        public List<ChannelCtrl> ChannelCtrls => channelCtrls;
        public int NoteViewChannel { get; set; } = -1;
        public bool IsNoteViewMode => NoteViewChannel >= 0;
        int playbackCursorPos = 0;

        bool autoscrollActive = true;
        public bool AutoscrollActive { get => autoscrollActive;
            set
            {
                autoscrollActive = value;
                PropertyChanged?.Invoke(this, 
                    new PropertyChangedEventArgs(nameof(AutoscrollActive)));
            } } 
        public int PlaybackCursorPos
        {
            get => playbackCursorPos;
            set
            {
                playbackCursorPos = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaybackCursorPos)));
            }
        }
        public SequencerModel()
        {
            player.OnSongLoaded += Player_OnSongLoaded;
            player.OnPlaybackTime += Player_OnPlaybackTime;
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackComplete += Player_OnPlaybackComplete;
        }

        private void Player_OnPlaybackComplete(object? sender, bool e)
        {
        }

        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
        }

        private void Player_OnPlaybackTime(object? sender, MeltySynth.MidiSynthSequencer.PlaybackTimeArgs e)
        {
            if (ChannelCtrls != null)
            {
                foreach (var channel in ChannelCtrls)
                {
                    channel.PlaybackCursorPos = e.ticks;
                }
            }
            this.PlaybackCursorPos = e.ticks;
        }

        private void Player_OnSongLoaded(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            midiSong = new MidiSong(e.midiFile);
            chordAnalyzer = new ChordAnalyzer(e.midiFile);
            chordAnalyzer.Analyze();
            SongKey = ChordAnalyzer.KeyNames[chordAnalyzer.SongKey];
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SongKey)));
            currentTicks = 0;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddChannels();
            });
        }

        void AddChannels()
        {
            channelCtrls = new List<ChannelCtrl>();
            for (int i = 0; i < midiSong.Tracks.Length; i++)
            {
                TrackInfo track = midiSong.Tracks[i];

                channelCtrls.Add(
                    new ChannelCtrl(midiSong, track));
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChannelCtrls)));
        }

        public void SetNoteViewMode(int channel)
        {
            this.NoteViewChannel = channel;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NoteViewChannel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNoteViewMode)));
        }
    }
    public class ChannelCtrl : INotifyPropertyChanged
    {
        bool expanded = false;
        TrackInfo track;
        MidiSong song;
        public double Height { get => expanded ? 600 : 150; }
        public int ChannelNum => track.ChannelNum;
        public string Instrument => track.Instrument;

        public float Unique => track.UniqueMeasures;
        public float FilledMeasures => track.FilledMeasures;
        public double AverageNoteLength => track.AverageNoteLength;
        public double AverageNotePitch => track.AverageNotePitch;

        public double AverageNoteOverlap => track.AverageNoteOverlap;

        public string TrackType => track.TrackType.ToString();
        public bool IsSolo { get; set; }
        public bool IsMute { get; set; }
        public int LengthSixteenths => song.LengthSixteenths;
        public Note[] Notes => track.Notes;

        int playbackCursorPos = 0;
        public int PlaybackCursorPos
        {
            get => playbackCursorPos;
            set
            {
                playbackCursorPos = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaybackCursorPos)));
                OnPlaybackCursorChanged?.Invoke(this, value);
            }
        }

        public int Resolution => song.Resolution;
        public SolidColorBrush Background { get; }
        public ChannelCtrl(MidiSong _song, MidiSong.TrackInfo _track)
        {
            track = _track;
            song = _song;
            int typeInt = (int)track.TrackType;
            int rsub = ((typeInt + 1) & 1) != 0 ? 25 : 0;
            int gsub = (((typeInt + 1) >> 1) & 1) != 0 ? 25 : 0;
            int bsub = (((typeInt + 1) >> 2) & 1) != 0 ? 25 : 0;
            if (!App.IsDarkTheme())
            {
                rsub = 255 - rsub; gsub = 255 - gsub; bsub = 255 - bsub;
            }
            Background = new SolidColorBrush(
                Color.FromRgb((byte)(rsub), (byte)(gsub), (byte)(bsub)));
        }

        public void ExpandCollapse()
        {
            expanded = !expanded;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
            Seq.Height = Height;
        }

        public SequencerChannel Seq;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<int> OnPlaybackCursorChanged;
    }
}

