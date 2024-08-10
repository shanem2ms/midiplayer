using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using midilib;
using midilonia.Views;
using System;

namespace midilonia;

public partial class App : Application
{
    public static MidiDb Db { get; set; } = new MidiDb();
    public static MidiPlayer Player { get; set; } = new MidiPlayer(Db);

    public static MidiPlayer.OnAudioEngineCreateDel OnEngineCreate = null;
    
    public static MainViewModel ViewModel { get; } = new MainViewModel();
    public static SequencerModel SequencerMdl { get; set; } = new SequencerModel();
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    public static bool IsDarkTheme()
    {
        var app = Application.Current;
        if (app is null)
        {
            throw new InvalidOperationException("Application instance is not available.");
        }

        var themeVariant = app.ActualThemeVariant;

        // Check if the theme is dark
        return themeVariant == ThemeVariant.Dark;
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
