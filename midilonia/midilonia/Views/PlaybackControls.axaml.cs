using Avalonia.Controls;
using Avalonia.Data.Converters;
using midilib;
using System;
using System.Globalization;
using System.Linq;

namespace midilonia.Views
{
    public partial class PlaybackControls : UserControl
    {
        MidiDb db = App.Db;
        MidiPlayer player = App.Player;
        public PlaybackControls()
        {
            DataContext = App.ViewModel;
            InitializeComponent();
        }

        private void PlayButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                switch (btn.Name)
                {
                    case "PlayBtn":
                        {
                            MidiDb.Fi fi = db.AllMidiFiles.First(m => m.NmLwr == App.ViewModel.CurrentSong);
                            player.PlaySong(fi, false);
                            player.PauseOrUnPause(false);
                        }
                        break;
                    case "StopBtn":
                        {
                            player.PauseOrUnPause(true);
                        }
                        break;
                    case "RewindBtn":
                        break;
                }
            }
        }
    }


    public class TimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long ms)
            {
                return TimeSpan.FromMilliseconds(ms).ToString(@"mm\:ss");
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not needed for this scenario
            throw new NotImplementedException();
        }
    }
}
