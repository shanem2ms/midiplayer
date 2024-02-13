using Avalonia.Controls;
using midilib;
using NAudio.Midi;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static midilib.MidiDb;

namespace midilonia.Views;

public partial class MainView : UserControl, INotifyPropertyChanged
{
    MidiDb db = App.Db;
    MidiPlayer player = App.Player;
    
    public MainView()
    {
        DataContext = this;
        InitializeComponent();
        Initialize();
    }

    public string Greeting => "MidiLonia";

    public new event PropertyChangedEventHandler? PropertyChanged;
    public IEnumerable<MidiDb.ArtistDef> Artists => db.Artists;

    MidiDb.ArtistDef currentArtist;
    public MidiDb.ArtistDef CurrentArtist
    {
        get => currentArtist;
        set
        {
            currentArtist = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArtistSongs)));
        }
    }
    public IEnumerable<string> ArtistSongs => CurrentArtist?.Songs;
    
    string currentSong;
    public string CurrentSong { get => currentSong; set { currentSong = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSong))); } }

    private async Task<bool> Initialize()
    {
        //await db.UploadAWS();
        await db.InitializeMappings();
        db.InitSongList(false);
        //player.OnPlaybackTime += Player_OnPlaybackTime;
        //player.OnPlaybackStart += Player_OnPlaybackStart;
        //player.OnPlaybackComplete += Player_OnPlaybackComplete;

        await player.Initialize(OnEngineCreate);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Artists"));
        return true;
    }

    void OnEngineCreate(MidiSynthEngine midiSynthEngine)
    {
        App.OnEngineCreate(midiSynthEngine);
    }

    private void PlayButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            switch (btn.Name)
            {
                case "PlayBtn":
                    {
                        MidiDb.Fi fi = db.AllMidiFiles.First(m => m.NmLwr == CurrentSong);
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
