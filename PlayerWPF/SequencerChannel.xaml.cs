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
            InitializeComponent();
        }


        public string InstrumentName { get; set; } = "GM Inst";
        int channelHeight = 50;
        int gmNoteRange = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;
        Canvas channelCanvas = null;
        IGrouping<byte, MeltySynth.MidiFile.Message> messages;
        
        int midiFileRes;
        int pixelsPerSixteenth;
        int numTicks;
        int channelIdx;

        public void Layout(int _channelIdx,
            IGrouping<byte, MeltySynth.MidiFile.Message> _messages,
            int _midiFileRes, int _pixelsPerSixteenth,
                int _numTicks)
        {
            midiFileRes = _midiFileRes;
            messages = _messages;
            numTicks = _numTicks;
            channelIdx = _channelIdx;
            pixelsPerSixteenth = _pixelsPerSixteenth;
            Relayout();
        }

        void Relayout()
        {
            if (channelCanvas != null)
                mainGrid.Children.Remove(channelCanvas);

            int sixteenthRes = midiFileRes / 4;
            double pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
            int lastTick = numTicks;
            long lengthSixteenths = lastTick / sixteenthRes;
            double YnoteSizePixels = (1.0) / (double)gmNoteRange * channelHeight;

            channelCanvas = new Canvas();
            channelCanvas.Height = channelHeight;
            channelCanvas.Width = lengthSixteenths * pixelsPerSixteenth;
            int rsub = ((channelIdx + 1) & 1) != 0 ? 25 : 0;
            int gsub = (((channelIdx + 1) >> 1) & 1) != 0 ? 25 : 0;
            int bsub = (((channelIdx + 1) >> 2) & 1) != 0 ? 25 : 0;
            channelCanvas.Background = new SolidColorBrush(
                Color.FromRgb((byte)(255 - rsub), (byte)(255 - gsub), (byte)(255 - bsub)));
            Grid.SetColumn(channelCanvas, 1);

            int[] noteOnTick = new int[127];
            for (int j = 0; j < noteOnTick.Length; j++)
                noteOnTick[j] = -1;

            foreach (var msg in messages)
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
            mainGrid.Children.Add(channelCanvas);
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            channelHeight = 500;
            Relayout();
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            channelHeight = 50;
            Relayout();
        }
    }
}
