using midilib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using midilib;
using System.ComponentModel;
using Avalonia.Threading;
using Avalonia.Media;
using midilonia.Views;
using Avalonia.Controls.Shapes;
using Avalonia.Controls;
using static midilonia.SequencerModel;
using System.Threading.Channels;

namespace midilonia
{
    public class SequencerModel
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
        public IEnumerable<ChannelCtrl> ChannelCtrls => channelCtrls;

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
            //Relayout();
        }

   
    }
    public class ChannelCtrl : INotifyPropertyChanged
    {
        bool expanded = false;
        MidiSong.TrackInfo track;
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

        public SolidColorBrush Background { get; }
        public ChannelCtrl(MidiSong.TrackInfo _track,
            SequencerChannel seq)
        {
            track = _track;
            Seq = seq;

            int typeInt = (int)track.TrackType;
            int rsub = ((typeInt + 1) & 1) != 0 ? 25 : 0;
            int gsub = (((typeInt + 1) >> 1) & 1) != 0 ? 25 : 0;
            int bsub = (((typeInt + 1) >> 2) & 1) != 0 ? 25 : 0;
            Background = new SolidColorBrush(
                Color.FromRgb((byte)(255 - rsub), (byte)(255 - gsub), (byte)(255 - bsub)));
            seq.Background = Background;
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
