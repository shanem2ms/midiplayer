using Avalonia.Controls;
using System.Net.NetworkInformation;

namespace midilonia.Views;

public partial class ArtistsSongs : UserControl
{
    public ArtistsSongs()
    {
        DataContext = App.ViewModel;
        InitializeComponent();
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        App.ViewModel.CurrentArtist = null;
    }

    private void PlayButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            string songname = btn.DataContext as string;
            App.ViewModel.CurrentSong = songname;
        }
    }
}
