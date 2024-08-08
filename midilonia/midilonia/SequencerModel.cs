using Avalonia.Media;
using Avalonia.Threading;
using midilib;
using midilonia.Views;
using System.Collections.Generic;
using System.ComponentModel;

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

            /*

    int sixteenthRes = midiSong.Resolution / 4;
    pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
    int height = (int)TimeStep.Height;

    for (int i = 0; i < midiSong.LengthSixteenths; i += 4)
    {
        Line l = new Line();
        l.StartPoint = new Avalonia.Point(i * pixelsPerSixteenth - 1, 0);
        l.EndPoint = new Avalonia.Point(i * pixelsPerSixteenth, (i % 16) == 0 ? height : height / 2);
        l.Stroke = Brushes.Black;
        TimeStep.Children.Add(l);
    }
    for (int i = 0; i < midiSong.LengthSixteenths; i += 16)
    {
        TextBlock textBlock = new TextBlock();
        textBlock.Text = (i / 16 + 1).ToString();
        TimeStep.Children.Add(textBlock);
        Canvas.SetLeft(textBlock, i * pixelsPerSixteenth);
    }
    TimeStep.Width = midiSong.LengthSixteenths * pixelsPerSixteenth;
            */
            for (int i = 0; i < midiSong.Tracks.Length; i++)
            {
                MidiSong.TrackInfo track = midiSong.Tracks[i];

                channelCtrls.Add(
                    new ChannelCtrl(track));
                //Channels.Children.Add(sequencerChannel);
            }


            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChannelCtrls)));
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
        public ChannelCtrl(MidiSong.TrackInfo _track)
        {
            track = _track;

            int typeInt = (int)track.TrackType;
            int rsub = ((typeInt + 1) & 1) != 0 ? 25 : 0;
            int gsub = (((typeInt + 1) >> 1) & 1) != 0 ? 25 : 0;
            int bsub = (((typeInt + 1) >> 2) & 1) != 0 ? 25 : 0;
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
