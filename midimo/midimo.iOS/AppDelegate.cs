using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using Foundation;
using UIKit;
using midilib;
using OpenTK.Graphics.ES30;
using Xamarin.Forms;

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
            player.Initialize(OnEngineCreate);
            midiOut = new MidiOut();

            return base.FinishedLaunching(app, options);
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
            var toggle = new Switch { IsToggled = true };
            var button = new Button { Text = "Display" };

            view.HeightRequest = 300;
            view.WidthRequest = 300;

            float red = 0, green = 0, blue = 0;
            view.OnDisplay = r => {

                GL.ClearColor(red, green, blue, 1.0f);
                GL.Clear((ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

                red += 0.01f;
                if (red >= 1.0f)
                    red -= 1.0f;
                green += 0.02f;
                if (green >= 1.0f)
                    green -= 1.0f;
                blue += 0.03f;
                if (blue >= 1.0f)
                    blue -= 1.0f;
            };

            toggle.Toggled += (s, a) => {
                view.HasRenderLoop = toggle.IsToggled;
            };
            button.Clicked += (s, a) => view.Display();

            StackLayout stack = new StackLayout
            {
                Padding = new Size(20, 20),
                Children = { view, toggle, button }
            };

            return stack;
        }

    }
}

