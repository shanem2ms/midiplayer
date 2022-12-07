using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using midilib;
using Xamarin.Forms;

namespace midimo
{
    public partial class ChannelOutput : ContentView, INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler PropertyChanged;

        public static readonly BindableProperty ChannelIdProperty =
           BindableProperty.CreateAttached("ChannelId", typeof(int), typeof(ChannelOutput),
              0);

        public int ChannelId
        {
            get { return (int)GetValue(ChannelIdProperty); }
            set { SetValue(ChannelIdProperty, value); }
        }

        public static readonly BindableProperty DataColorProperty =
           BindableProperty.CreateAttached("DataColor", typeof(Brush), typeof(ChannelOutput),
              Brush.Black);

        int dataValue = 0;
        public Brush DataColor
        {
            get { return (Brush)GetValue(DataColorProperty); }
            set { SetValue(DataColorProperty, value); }
        }

        public int PatchNumber { get; set; }
        public string Instrument => ChannelId == 9 ? "Drums" : GMInstruments.Names[PatchNumber];
        bool isEnabled = true;
        public ChannelOutput()
        {
            this.BindingContext = this;
            InitializeComponent();
            Device.StartTimer(new TimeSpan(100000),
                dispatcherTimer_Tick);
        }

        private bool dispatcherTimer_Tick()
        {
            if (this.dataValue > 0)
            {
                this.dataValue -= 8;
                this.DataColor = LerpColor(Color.LightGray, Color.Green, this.dataValue);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataColor)));
            }
            return true;
        }

        public void ResetForSong()
        {
            Dispatcher.BeginInvokeOnMainThread(() => IsVisible = false);
            PatchNumber = 0;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PatchNumber)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Instrument)));
            isEnabled = false;
        }

        public void SetMidiData(MidiPlayer.ChannelEvent e)
        {
            if (e.command == MidiSpec.PatchChange)
            {
                PatchNumber = e.data1;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PatchNumber)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Instrument)));
            }
            else if (e.command == MidiSpec.NoteOff || e.command == MidiSpec.NoteOn)
            {
                dataValue = 255;
                this.DataColor = Color.Green;
                if (!isEnabled)
                {
                    Dispatcher.BeginInvokeOnMainThread(() => IsVisible = true);
                    isEnabled = true;
                }
            }
        }

        Color LerpColor(Color l, Color r, int v)
        {
            return Color.FromRgba(
                (byte)(((r.R * 255 * v) + (l.R * 255 * (255 - v))) / 255),
                (byte)(((r.G * 255 * v) + (l.G * 255 * (255 - v))) / 255),
                (byte)(((r.B * 255 * v) + (l.B * 255 * (255 - v))) / 255),
                255);
        }
    }

    public class IntToBrushConverter : IValueConverter
    {
        public static readonly IntToBrushConverter Instance = new IntToBrushConverter();


        Color LerpColor(Color l, Color r, int v)
        {
            return Color.FromRgba(255,
                (byte)(((r.R * v) + (l.R * (255 - v))) / 255),
                (byte)(((r.G * v) + (l.G * (255 - v))) / 255),
                (byte)(((r.B * v) + (l.B * (255 - v))) / 255));
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color OffColor = Color.LightGray;
            Color OnColor = Color.Green;
            if (value is int sourceInt
                && typeof(Brush).IsAssignableFrom(targetType))
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

