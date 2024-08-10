using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia;

namespace midilonia.Android
{
    [Activity(
        Label = "midilonia.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        static audioout.Droid.AudioOut audioOut = new audioout.Droid.AudioOut();

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            App.OnEngineCreate = audioOut.OnEngineCreate;
            IconProvider.Current
              .Register<FontAwesomeIconProvider>();

            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .UseReactiveUI();
        }
    }
}
