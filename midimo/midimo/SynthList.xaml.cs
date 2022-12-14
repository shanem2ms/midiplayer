using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using midilib;
using System.ComponentModel;
using System.Globalization;

namespace midimo
{
    public partial class SynthList : ContentView, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<MidiDb.SoundFontDesc> Synths =>
            new  ObservableCollection<MidiDb.SoundFontDesc>(db.AllSoundFonts);
        MidiDb db;
        MidiPlayer player;

        MidiDb.SoundFontDesc selectedSynth;
        public MidiDb.SoundFontDesc SelectedSynth
        {
            get => selectedSynth;
            set
            {
                selectedSynth = value;
                player.ChangeSoundFont(selectedSynth);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSynth)));
            }
        }

        public SynthList()
        {
            db = App.Instance.db;
            db.OnIntialized += Db_OnIntialized;
            player = App.Instance.player;
            this.BindingContext = this;
            this.selectedSynth = player.CurrentSoundFont;
            InitializeComponent();
            
        }

        private void Db_OnIntialized(object sender, bool e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Synths)));
        }
    }


    public class BoolToColorConverter : IValueConverter
    {
        public static readonly BoolToColorConverter Instance = new BoolToColorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color OffColor = Color.LightGray;
            Color OnColor = Color.Green;
            if (value is bool sourceInt
                && typeof(Color).IsAssignableFrom(targetType))
            {
                return sourceInt ? OnColor : OffColor;
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

