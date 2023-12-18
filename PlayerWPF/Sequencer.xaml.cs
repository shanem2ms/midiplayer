﻿using midilib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static MeltySynth.MidiFileSequencer;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace PlayerWPF
{
    /// <summary>
    /// Interaction logic for Sequencer.xaml
    /// </summary>
    public partial class Sequencer : UserControl, INotifyPropertyChanged
    {
        MidiPlayer player = App.Player;
        MeltySynth.MidiFile midiFile;
        const int pixelsPerSixteenth = 10;
        double pixelsPerTick;
        DispatcherTimer dispatcherTimer;
        double CursorPosX { get; set; }
        public string SongKey { get; set; }
        ChordAnalyzer chordAnalyzer;
        public event PropertyChangedEventHandler? PropertyChanged;

        class ChannelCtrl
        {
            bool expanded = false;
            public ChannelCtrl(Button channelBtn,
                SequencerChannel seq)
            {
                ChannelBtn = channelBtn;
                Seq = seq;
                ChannelBtn.Click += ChannelBtn_Click;
            }

            private void ChannelBtn_Click(object sender, RoutedEventArgs e)
            {
                if (!expanded)
                    ChannelBtn.Height = Seq.Height = 500;
                else
                    ChannelBtn.Height = Seq.Height = 50;

                expanded = !expanded;
            }

            public Button ChannelBtn;
            public SequencerChannel Seq;
        }

        List<ChannelCtrl> channelCtrls = new List<ChannelCtrl>();

        public Sequencer()
        {
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackTime += Player_OnPlaybackTime;

            //  DispatcherTimer setup
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            dispatcherTimer.Start();

            this.DataContext = this;
            InitializeComponent();
            //BuildPiano();
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            currentPosLine.X1 = currentPosLine.X2 = CursorPosX;
        }

        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            midiFile = e.midiFile;
            chordAnalyzer = new ChordAnalyzer(midiFile);
            chordAnalyzer.Analyze();
            SongKey = ChordAnalyzer.KeyNames[chordAnalyzer.SongKey];
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SongKey)));
            Relayout();
        }
        private void Player_OnPlaybackTime(object? sender, PlaybackTimeArgs e)
        {
            double pixels = pixelsPerTick * e.ticks;
            CursorPosX = pixels;
        }
       
        void Relayout()
        {
            channelCtrls.Clear();
            Channels.Children.Clear();
            ChannelNames.Children.Clear();
            TimeStep.Children.Clear();

            int sixteenthRes = midiFile.Resolution / 4;
            pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
            int lastTick = midiFile.Messages.Last().Ticks;
            long lengthSixteenths = lastTick / sixteenthRes;
            int height = (int)TimeStep.Height;

            for (int i = 0; i < lengthSixteenths; i += 4)
            {
                Line l = new Line();
                l.X1 = i * pixelsPerSixteenth - 1;
                l.X2 = i * pixelsPerSixteenth;
                l.Y1 = 0;
                l.Y2 = (i % 16) == 0 ? height : height / 2;
                l.Stroke = Brushes.Black;
                TimeStep.Children.Add(l);
            }
            for (int i = 0; i < lengthSixteenths; i += 16)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = (i / 16 + 1).ToString();
                TimeStep.Children.Add(textBlock);
                Canvas.SetLeft(textBlock, i * pixelsPerSixteenth);
            }
            TimeStep.Width = lengthSixteenths * pixelsPerSixteenth;

            var channelGroups = midiFile.Messages.Where(m => m.Channel < 16).GroupBy(m => m.Channel).
                OrderBy(g => g.Key);
            int numChannels = channelGroups.Count();

            Random r = new Random();
            for (int i = 0; i < numChannels; i++)
            {
                SequencerChannel sequencerChannel = new SequencerChannel();
                sequencerChannel.Height = 50;
                sequencerChannel.Layout(i,
                    channelGroups.ElementAt(i),
                    midiFile.Resolution,
                    pixelsPerSixteenth,
                    lastTick
                    );
                Button btn = new Button();
                btn.Content = $"Channel{i + 1}";
                btn.Height = 50;
                ChannelNames.Children.Add(btn);
                Channels.Children.Add(sequencerChannel);
                channelCtrls.Add(
                    new ChannelCtrl(btn, sequencerChannel));
            }
        }

        void BuildPiano()
        {
            PianoCanvas.Children.Clear();
            double len = PianoCanvas.ActualWidth;
            double h = PianoCanvas.ActualHeight;
            Piano piano = new Piano(false);
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
            player.Seek(ticks);
        }
    }
}
