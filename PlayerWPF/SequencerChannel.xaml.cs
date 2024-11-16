using midilib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PlayerWPF
{
    /// <summary>
    /// Interaction logic for SequencerChannel.xaml
    /// </summary>
    public partial class SequencerChannel : UserControl
    {
        public SequencerChannel()
        {
            this.DataContext = this;
            this.SizeChanged += SequencerChannel_SizeChanged;
            InitializeComponent();
        }

        private void SequencerChannel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Relayout();
        }

        public string InstrumentName { get; set; } = "GM Inst";
        int gmNoteRange = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;
        MidiSong.TrackInfo trackInfo;

        int midiFileRes;
        int pixelsPerSixteenth;
        int numTicks;
        int channelIdx;

        public void Layout(int _channelIdx,
            MidiSong.TrackInfo _trackInfo,
            int _midiFileRes, int _pixelsPerSixteenth,
                int _numTicks)
        {
            midiFileRes = _midiFileRes;
            trackInfo = _trackInfo;
            numTicks = _numTicks;
            channelIdx = _channelIdx;
            pixelsPerSixteenth = _pixelsPerSixteenth;
            Relayout();
        }

        void Relayout()
        {
            mainCanvas.Children.Clear();
            int sixteenthRes = midiFileRes / 4;
            double pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
            int lastTick = numTicks;
            long lengthSixteenths = lastTick / sixteenthRes;
            int channelHeight = (int)mainCanvas.ActualHeight;
            if (channelHeight <= 0)
                return;
            double YnoteSizePixels = (1.0) / (double)gmNoteRange * channelHeight;

            mainCanvas.Width = lengthSixteenths * pixelsPerSixteenth;

            int[] noteOnTick = new int[127];
            for (int j = 0; j < noteOnTick.Length; j++)
                noteOnTick[j] = -1;

            foreach (var note in trackInfo.Notes)
            {

                int startTicks = note.startTicks;
                int endTicks = note.startTicks + note.lengthTicks;

                if (note.note >= GMInstruments.MidiStartIdx &&
                    note.note < GMInstruments.MidiEndIdx)
                {
                    double Y = (GMInstruments.MidiEndIdx - note.note - 1) / (double)gmNoteRange * channelHeight;

                    Line l = new Line();
                    l.X1 = startTicks * pixelsPerTick;
                    l.X2 = endTicks * pixelsPerTick;
                    l.Y1 = Y;
                    l.Y2 = Y;
                    l.StrokeThickness = YnoteSizePixels;
                    l.Stroke = Brushes.Blue;
                    mainCanvas.Children.Add(l);
                }
                else if (note.note == MidiSong.PedalNote)
                {
                    double Y = 0;

                    Line l = new Line();
                    l.X1 = startTicks * pixelsPerTick;
                    l.X2 = endTicks * pixelsPerTick;
                    l.Y1 = Y;
                    l.Y2 = Y + 5;
                    l.StrokeThickness = YnoteSizePixels;
                    l.Stroke = Brushes.Green;
                    mainCanvas.Children.Add(l);
                }
            }
        }
    }
}
