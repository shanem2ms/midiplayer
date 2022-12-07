using midilib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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
using System.Windows.Threading;

namespace PlayerWPF
{
    /// <summary>
    /// Interaction logic for ChannelOutput.xaml
    /// </summary>

    public partial class ChannelOutput : UserControl, INotifyPropertyChanged
    {        
        public new event PropertyChangedEventHandler? PropertyChanged;
        public int ChannelId
        {
            get;
            set;
        }

        public int DataValue { get; set; } = 0;
        DispatcherTimer dispatcherTimer;
        public int PatchNumber { get; set; }
        public string Instrument => GMInstruments.Names[PatchNumber];
        public ChannelOutput()
        {
            this.DataContext = this;
            this.dispatcherTimer = new DispatcherTimer();
            this.dispatcherTimer.Interval = new TimeSpan(10000);
            this.dispatcherTimer.Tick += dispatcherTimer_Tick;
            InitializeComponent();
            this.dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (this.DataValue > 0)
            {
                this.DataValue -= 8;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataValue)));
            }
        }

        public void SetMidiData(MidiPlayer.ChannelEvent e)
        {
            DataValue = 255;
            if (e.command == 0xC0)
            {
                PatchNumber = e.data1;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PatchNumber)));
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataValue)));
        }
    }

    public class IntToBrushConverter : IValueConverter
    {
        public static readonly IntToBrushConverter Instance = new();


        Color LerpColor(Color l, Color r, int v)
        {
            return Color.FromArgb(255,
                (byte)(((r.R * v) + (l.R * (255 - v))) / 255),
                (byte)(((r.G * v) + (l.G * (255 - v))) / 255),
                (byte)(((r.B * v) + (l.B * (255 - v))) / 255));
        }
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            Color OffColor = Colors.LightGray;
            Color OnColor = Colors.Green;
            if (value is int sourceInt
                && targetType.IsAssignableTo(typeof(Brush)))
            {
                Color c = LerpColor(OffColor, OnColor, sourceInt);
                return new SolidColorBrush(c);
            }
            // converter used for the wrong type
            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}