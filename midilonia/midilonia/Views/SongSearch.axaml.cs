using Avalonia.Controls;
using midilib;
using System.Security.Cryptography;

namespace midilonia.Views
{
    public partial class SongSearch : UserControl
    {
        public SongSearch()
        {
            DataContext = App.ViewModel;
            InitializeComponent();
        }

        private void PlayButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                MidiDb.Fi midifile = btn.DataContext as MidiDb.Fi;
                App.ViewModel.CurrentSong = midifile.NmLwr;
            }
        }

    }
}
