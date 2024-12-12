using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using midilib;
using System;
using static midilib.MidiSong;

namespace midilonia.Views
{
    public partial class SequencerChannel : UserControl
    {
        ChannelCtrl channelCtrl;
        const int pixelsPerSixteenth = 10;
        Line playbackCursor = null;
        double pixelsPerTick = 0;
        public SequencerChannel()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            if (DataContext is ChannelCtrl)
            {
                channelCtrl = DataContext as ChannelCtrl;
                channelCtrl.OnPlaybackCursorChanged += ChannelCtrl_OnPlaybackCursorChanged;
            }
            base.OnDataContextChanged(e);
        }

        private void ChannelCtrl_OnPlaybackCursorChanged(object? sender, int ticks)
        {
            Dispatcher.UIThread.InvokeAsync( () => UpdateCursor(ticks));
        }

        void UpdateCursor(int ticks)
        {
            if (playbackCursor == null)
            {
                playbackCursor = new Line
                {
                    StrokeThickness = 5,
                    Stroke = Brushes.Red // Avalonia.Media.Brushes
                };
                mainCanvas.Children.Add(playbackCursor);
            }

            playbackCursor.StartPoint = new Avalonia.Point(ticks * pixelsPerTick, 0);
            playbackCursor.EndPoint = new Avalonia.Point(ticks * pixelsPerTick, mainCanvas.Bounds.Height);
        }

        int gmNoteRange = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (channelCtrl != null)
            {
                Relayout();
            }
        }
        void Relayout()
        {
            mainCanvas.Children.Clear();
            int midiFileRes = channelCtrl.Resolution;

            int sixteenthRes = midiFileRes / 4;
            pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
            long lengthSixteenths = channelCtrl.LengthSixteenths;
            int channelHeight = (int)mainCanvas.Bounds.Height;
            if (channelHeight <= 0)
                return;

            double YnoteSizePixels = (1.0) / (double)gmNoteRange * channelHeight;

            mainCanvas.Width = lengthSixteenths * pixelsPerSixteenth;

            int[] noteOnTick = new int[127];
            for (int j = 0; j < noteOnTick.Length; j++)
                noteOnTick[j] = -1;

            foreach (var note in channelCtrl.Notes)
            {
                int startTicks = note.startTicks;
                int endTicks = note.startTicks + note.lengthTicks;

                if (note.note >= GMInstruments.MidiStartIdx &&
                    note.note < GMInstruments.MidiEndIdx)
                {
                    double Y = (GMInstruments.MidiEndIdx - note.note - 1) / (double)gmNoteRange * channelHeight;

                    var line = new Line
                    {
                        StartPoint = new Avalonia.Point(startTicks * pixelsPerTick, Y),
                        EndPoint = new Avalonia.Point(endTicks * pixelsPerTick, Y),
                        StrokeThickness = YnoteSizePixels,
                        Stroke = Brushes.Blue // Avalonia.Media.Brushes
                    };
                    mainCanvas.Children.Add(line);
                }
                else if (note.note == MidiSong.PedalNote)
                {
                    double Y = 0;

                    var line = new Line
                    {
                        StartPoint = new Avalonia.Point(startTicks * pixelsPerTick, Y),
                        EndPoint = new Avalonia.Point(endTicks * pixelsPerTick, Y + 5),
                        StrokeThickness = YnoteSizePixels,
                        Stroke = Brushes.Green
                    };
                    mainCanvas.Children.Add(line);
                }
            }

            playbackCursor = null;
        }
    }
}
