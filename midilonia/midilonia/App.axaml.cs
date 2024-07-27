using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using midilib;
using midilonia.Views;

namespace midilonia;

public partial class App : Application
{
    public static MidiDb Db { get; set; } = new MidiDb();
    public static MidiPlayer Player { get; set; } = new MidiPlayer(Db);

    public static MidiPlayer.OnAudioEngineCreateDel OnEngineCreate = null;
    
    public static MainViewModel ViewModel { get; } = new MainViewModel();
    public static SequencerModel SequencerMdl { get; set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
            };
        }

        string[] resources = typeof(App).Assembly.GetManifestResourceNames();
        base.OnFrameworkInitializationCompleted();
    }
}
