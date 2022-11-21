using System;
using UIKit;
using NAudio.Wave;
using midilib;

namespace midimouse
{
	public partial class MainViewController : UITabBarController
	{
        MidiDb db = new MidiDb();
        MidiPlayer player;
        AVAudioEngineOut aVAudioEngineOut;

        public MainViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
            player = new MidiPlayer(db);

            FirstViewController fc = (ViewControllers[0] as FirstViewController);
            fc.Db = db;
            fc.OnSongSelected += Fc_OnSongSelected;
            UIView view1 = fc.View;
            SecondViewController sc = (ViewControllers[1] as SecondViewController);
            sc.Player = player;
            UIView view2 = sc.View;

            player.Initialize(OnEngineCreate).ContinueWith((action) =>
            {
                sc.OnPlayerInitialized();
            });
        }

        private void Fc_OnSongSelected(object sender, MidiDb.Fi e)
        {
            player.PlaySong(e);
        }

        public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
		}

        void OnEngineCreate(MidiSampleProvider midiSampleProvider)
        {
            aVAudioEngineOut = new AVAudioEngineOut();
            aVAudioEngineOut.Init(midiSampleProvider);
            aVAudioEngineOut.Play();
        }
    }
}


