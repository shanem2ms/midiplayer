using midilib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Xps.Serialization;
using static MeltySynth.MidiSynthSequencer;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace PlayerWPF
{
    /// <summary>
    /// Interaction logic for Sequencer.xaml
    /// </summary>
    public partial class Sequencer : UserControl, INotifyPropertyChanged
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


        List<ChannelCtrl> channelCtrls = null;
        public IEnumerable<ChannelCtrl> ChannelCtrls => channelCtrls;

        public Sequencer()
        {
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackTime += Player_OnPlaybackTime;
            player.OnSongLoaded += Player_OnSongLoaded;

            //  DispatcherTimer setup
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            dispatcherTimer.Start();

            this.DataContext = this;
            InitializeComponent();
            //BuildPiano();
        }

        private void Player_OnSongLoaded(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            midiSong = new MidiSong(e.midiFile);
            chordAnalyzer = new ChordAnalyzer(e.midiFile);
            chordAnalyzer.Analyze();
            SongKey = ChordAnalyzer.KeyNames[chordAnalyzer.SongKey];
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SongKey)));
            currentTicks = 0;
            Relayout();
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            currentPosLine.X1 = currentPosLine.X2 = CursorPosX;
        }

        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
        }
        private void Player_OnPlaybackTime(object? sender, PlaybackTimeArgs e)
        {
            currentTicks = e.ticks;
            double pixels = pixelsPerTick * e.ticks;
            CursorPosX = pixels;
        }

        void Relayout()
        {
            channelCtrls = new List<ChannelCtrl>();
            Channels.Children.Clear();
            TimeStep.Children.Clear();

            int sixteenthRes = midiSong.Resolution / 4;
            pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
            int height = (int)TimeStep.Height;

            for (int i = 0; i < midiSong.LengthSixteenths; i += 4)
            {
                Line l = new Line();
                l.X1 = i * pixelsPerSixteenth - 1;
                l.X2 = i * pixelsPerSixteenth;
                l.Y1 = 0;
                l.Y2 = (i % 16) == 0 ? height : height / 2;
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

            for (int i = 0; i < midiSong.Tracks.Length; i++)
            {
                MidiSong.TrackInfo track = midiSong.Tracks[i];
                SequencerChannel sequencerChannel = new SequencerChannel();
                sequencerChannel.Height = 150;
                sequencerChannel.Layout(i,
                    track,
                    midiSong.Resolution,
                    pixelsPerSixteenth,
                    midiSong.LengthTicks
                    );
                channelCtrls.Add(
                    new ChannelCtrl(track, sequencerChannel));
                Channels.Children.Add(sequencerChannel);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChannelCtrls)));
        }

        void BuildPiano()
        {
            PianoCanvas.Children.Clear();
            double len = PianoCanvas.ActualWidth;
            double h = PianoCanvas.ActualHeight;
            Piano piano = new Piano();
            for (int i = 0; i < piano.PianoKeys.Length; i++)
            {
                bool isBlack = piano.PianoKeys[i].isBlack;

                Rectangle r = new Rectangle();
                r.Width = piano.PianoWhiteXs * len;
                r.Height = isBlack ? h / 2 : h;
                r.Stroke = Brushes.DarkBlue;
                r.StrokeThickness = 2;
                r.Fill = isBlack ? Brushes.Black : Brushes.White;
                Canvas.SetLeft(r, piano.PianoKeys[i].x * len);
                Canvas.SetTop(r, 0);
                r.MouseDown += R_MouseDown;
                r.MouseUp += R_MouseUp;
                r.Tag = i + GMInstruments.MidiStartIdx;
                PianoCanvas.Children.Add(r);
            }
        }

        Brush prevBrush = null;
        int curNoteDown = -1;
        private void R_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Rectangle r = sender as Rectangle;
            r.Fill = prevBrush;
            player.SynthEngine.NoteOff(curNoteDown);
            curNoteDown = 0;
            prevBrush = null;
            Mouse.Capture(null);
        }

        private void R_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle r = sender as Rectangle;
            curNoteDown = (int)r.Tag;
            player.SynthEngine.NoteOn(curNoteDown, 100);
            prevBrush = r.Fill;
            r.Fill = Brushes.Red;
            Mouse.Capture(r);
        }

        private void PianoCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BuildPiano();
        }

        private void TimeStep_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(TimeStep);
            int ticks = (int)(p.X / pixelsPerTick);
            currentTicks = ticks;
            double pixels = pixelsPerTick * ticks;
            CursorPosX = pixels;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0)
            {
                TopScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            if (e.VerticalChange != 0)
            {
                LeftScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            player.SynthEngine.Play(midiSong.GetMidiFile(), false, currentTicks);
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            player.SynthEngine.Stop();
        }


        Dictionary<Key, int> KeyNoteDict = new Dictionary<Key, int>()
        {
            { Key.A, 0 },
            { Key.W, 1 },
            { Key.S, 2 },
            { Key.E, 3 },
            { Key.D, 4 },
            { Key.F, 5 },
            { Key.T, 6 },
            { Key.G, 7 },
            { Key.Y, 8 },
            { Key.H, 9 },
            { Key.U, 10 },
            { Key.J, 11 },
            { Key.K, 12 },
            { Key.O, 13 },
            { Key.L, 14 }
        };
        protected override void OnKeyDown(KeyEventArgs e)
        {
            int val;
            if (KeyNoteDict.TryGetValue(e.Key, out val))
            {
                player.SynthEngine.NoteOn(60 + val, 100);
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            int val;
            if (KeyNoteDict.TryGetValue(e.Key, out val))
            {
                player.SynthEngine.NoteOff(60 + val);
            }
            base.OnKeyUp(e);
        }

        private void ChannelExpand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                ChannelCtrl ctrl = (ChannelCtrl)btn.DataContext;
                ctrl.ExpandCollapse();
            }
        }

        private void ChannelMuteSolo_Click(object sender, RoutedEventArgs e)
        {
            bool isSoloMode = channelCtrls.Any(c => c.IsSolo);
            foreach (var channel in channelCtrls)
            {
                bool channelEnabled = (isSoloMode && channel.IsSolo) ||
                    (!isSoloMode && !channel.IsMute);
                player.SynthEngine.SetChannelEnabled(channel.ChannelNum, channelEnabled);
            }
        }

        private void ToMelody_Click(object sender, RoutedEventArgs e)
        {
            midiSong = midiSong.ConvertToMelody();
            Relayout();
        }
    }

}
