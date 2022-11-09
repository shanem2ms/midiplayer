using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data;
using System.Globalization;
using System;
using Avalonia.Media;
using Avalonia.Threading;
using System.ComponentModel;

namespace PlayerAv
{
    public partial class ChannelOutput : UserControl, INotifyPropertyChanged
    {
        public static readonly StyledProperty<int> ChannelIdProperty =
            AvaloniaProperty.Register<ChannelOutput, int>(nameof(ChannelId));
        
        public new event PropertyChangedEventHandler? PropertyChanged;
        public int ChannelId
        {
            get { return GetValue(ChannelIdProperty); }
            set { SetValue(ChannelIdProperty, value); }
        }

        public int DataValue { get; set; } = 0;
        DispatcherTimer dispatcherTimer;
        public ChannelOutput()
        {
            this.DataContext = this;
            this.dispatcherTimer = new DispatcherTimer(new TimeSpan(10000), DispatcherPriority.Normal, dispatcherTimer_Tick);
            InitializeComponent();
            this.dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        { 
            if (this.DataValue > 0)
            {
                this.DataValue-= 8;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DataValue"));
            }
        }

        public void SetMidiData(int data)
        {
            DataValue = 255;
            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DataValue"));
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
                && targetType.IsAssignableTo(typeof(Avalonia.Media.IBrush)))
            {
                Color c = LerpColor(OffColor, OnColor, sourceInt);
                return new SolidColorBrush(c);
            }
            // converter used for the wrong type
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
