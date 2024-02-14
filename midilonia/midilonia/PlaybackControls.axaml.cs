using Avalonia.Controls;
using midilib;
using System.Linq;
using System.Numerics;

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
}
