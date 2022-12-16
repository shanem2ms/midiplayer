using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using Foundation;
using UIKit;
using midilib;
using OpenTK.Graphics.ES30;
using Xamarin.Forms;
using System.Threading.Tasks;

namespace midimo.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        MidiDb db = new MidiDb();
        MidiPlayer player;
        NAudio.Wave.AVAudioEngineOut aVAudioEngineOut;
        MidiOut midiOut;
        GlView glView;
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            player = new MidiPlayer(db);
            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App(db, player, InitOpenGlView));
            Init();

            return base.FinishedLaunching(app, options);
        }

        async Task<bool> Init()
        {
            await db.InitializeMappings();
            await player.Initialize(OnEngineCreate);
            midiOut = new MidiOut();
            return true;
        }

        void OnEngineCreate(MidiSampleProvider midiSampleProvider)
        {
            aVAudioEngineOut = new NAudio.Wave.AVAudioEngineOut();
            aVAudioEngineOut.Init(midiSampleProvider);
            aVAudioEngineOut.Play();
        }

        View InitOpenGlView()
        {
            var view = new OpenGLView { HasRenderLoop = true };

            glView = new GlView(player);

            view.HeightRequest = 300;
            view.WidthRequest = 300;

            view.OnDisplay = r => {
                glView.OnRender();
            };

            StackLayout stack = new StackLayout
            {
                Padding = new Size(20, 20),
                Children = { view }
            };

            return stack;
        }

    }
}

