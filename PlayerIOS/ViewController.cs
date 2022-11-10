using Foundation;
using System;
using UIKit;
using midiplayer;
using NAudio.Wave;

namespace PlayerIOS
{
    public partial class ViewController : UIViewController
    {
        MidiPlayer player;
        public ViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            // Perform any additional setup after loading the view, typically from a nib.
            player = new MidiPlayer(OnEngineCreate);
        }

        public override void DidReceiveMemoryWarning ()
        {
            base.DidReceiveMemoryWarning ();
            // Release any cached data, images, etc that aren't in use.
        }
        void OnEngineCreate(MidiSampleProvider midiSampleProvider)
        {
            AVAudioEngineOut aVAudioEngineOut = new AVAudioEngineOut();
            aVAudioEngineOut.Init(midiSampleProvider);
            aVAudioEngineOut.Play();
        }
    }   
}
