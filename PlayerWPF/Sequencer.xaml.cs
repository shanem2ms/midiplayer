using midilib;
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

        public event PropertyChangedEventHandler? PropertyChanged;

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
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            currentPosLine.X1 = currentPosLine.X2 = CursorPosX;
        }

        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            midiFile = e.midiFile;
            Relayout();
        }
        private void Player_OnPlaybackTime(object? sender, PlaybackTimeArgs e)
        {
            double pixels = pixelsPerTick * e.ticks;
            CursorPosX = pixels;
        }

        void Relayout()
        {
            Channels.Children.Clear();
            TimeStep.Children.Clear();

            int sixteenthRes = midiFile.Resolution / 4;
            pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
            int lastTick = midiFile.Messages.Last().Ticks;
            long lengthSixteenths = lastTick / sixteenthRes;
            int height = (int)TimeStep.Height;

            int channelHeight = 500;
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

            int gmNoteRange = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;
            double YnoteSizePixels = (1.0) / (double)gmNoteRange * channelHeight;

            Random r = new Random();
            for (int i = 0; i < numChannels; i++)
            {
                Canvas channelCanvas = new Canvas();
                channelCanvas.Height = channelHeight;
                channelCanvas.Width = lengthSixteenths * pixelsPerSixteenth;
                int rsub = ((i + 1) & 1) != 0 ? 25 : 0;
                int gsub = (((i + 1) >> 1) & 1) != 0 ? 25 : 0;
                int bsub = (((i + 1) >> 2) & 1) != 0 ? 25 : 0;
                channelCanvas.Background = new SolidColorBrush(
                    Color.FromRgb((byte)(255 - rsub), (byte)(255 - gsub), (byte)(255 - bsub)));

                var grp = channelGroups.ElementAt(i);
                int[] noteOnTick = new int[127];
                for (int j = 0; j < noteOnTick.Length; j++)
                    noteOnTick[j] = -1;

                foreach (var msg in grp)
                {
                    if ((msg.Command & 0xF0) == 0x90 &&
                        msg.Data2 > 0)
                    {
                        if (noteOnTick[msg.Data1] == -1)
                            noteOnTick[msg.Data1] = msg.Ticks;
                    }
                    else if ((msg.Command & 0xF0) == 0x80 ||
                        ((msg.Command & 0xF0) == 0x90 &&
                        msg.Data2 == 0))
                    {
                        int startTicks = noteOnTick[msg.Data1];
                        int endTicks = msg.Ticks;
                        noteOnTick[msg.Data1] = -1;

                        if (startTicks >= 0 && msg.Data1 >= GMInstruments.MidiStartIdx &&
                            msg.Data1 < GMInstruments.MidiEndIdx)
                        {
                            double Y = (GMInstruments.MidiEndIdx - msg.Data1 - 1) / (double)gmNoteRange * channelHeight;

                            Line l = new Line();
                            l.X1 = startTicks * pixelsPerTick;
                            l.X2 = endTicks * pixelsPerTick;
                            l.Y1 = Y;
                            l.Y2 = Y;
                            l.StrokeThickness = YnoteSizePixels;
                            l.Stroke = Brushes.Blue;
                            channelCanvas.Children.Add(l);
                        }
                    }
                }
                Channels.Children.Add(channelCanvas);
            }
        }
    }
}
