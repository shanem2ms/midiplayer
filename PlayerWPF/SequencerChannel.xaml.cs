using midilib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static midilib.MidiSong;

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
            int rsub = ((channelIdx + 1) & 1) != 0 ? 25 : 0;
            int gsub = (((channelIdx + 1) >> 1) & 1) != 0 ? 25 : 0;
            int bsub = (((channelIdx + 1) >> 2) & 1) != 0 ? 25 : 0;
            mainCanvas.Background = new SolidColorBrush(
                Color.FromRgb((byte)(255 - rsub), (byte)(255 - gsub), (byte)(255 - bsub)));

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
            }
        }
    }
}
