﻿using Avalonia.Media;
using Avalonia.Threading;
using midilib;
using midilonia.Views;
using System.Collections.Generic;
using System.ComponentModel;
using static midilib.MidiSong;

namespace midilonia
{
    public class SequencerModel : INotifyPropertyChanged
    {
        MidiPlayer player = App.Player;
        const int pixelsPerSixteenth = 10;
        double pixelsPerTick;
        DispatcherTimer dispatcherTimer;
        double CursorPosX { get; set; }
        public string SongKey { get; set; }
        ChordAnalyzer chordAnalyzer;
        MidiSong midiSong;
        public event PropertyChangedEventHandler? PropertyChanged;
        int currentTicks = 0;

        List<ChannelCtrl> channelCtrls = null;
        public List<ChannelCtrl> ChannelCtrls => channelCtrls;

        public SequencerModel()
        {
            player.OnSongLoaded += Player_OnSongLoaded;
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
                Relayout();
            });
        }

        void Relayout()
        {
            channelCtrls = new List<ChannelCtrl>();
    
            for (int i = 0; i < midiSong.Tracks.Length; i++)
            {
                MidiSong.TrackInfo track = midiSong.Tracks[i];
                
                channelCtrls.Add(
                    new ChannelCtrl(midiSong, track));
                //Channels.Children.Add(sequencerChannel);
            }


            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChannelCtrls)));
        }

    }
    public class ChannelCtrl : INotifyPropertyChanged
    {
        bool expanded = false;
        MidiSong.TrackInfo track;
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
    }
}
