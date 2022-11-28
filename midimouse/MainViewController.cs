using System;
using UIKit;
using NAudio.Wave;
using midilib;
using CoreMidi;
using System.Diagnostics;
using System.Security.Cryptography;
using static midilib.MidiPlayer;

namespace midimouse
{
	public partial class MainViewController : UITabBarController
	{
        MidiDb db = new MidiDb();
        MidiPlayer player;
        AVAudioEngineOut aVAudioEngineOut;
        MidiClient client;
        MidiEndpoint midiOut;
        MidiPort outputPort;
        int volume = 100;

        public MainViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
            if (Midi.DestinationCount > 0)
            {
                client = new MidiClient("Midimouse Client");
                midiOut = MidiEndpoint.GetDestination(Midi.DestinationCount - 1);
                outputPort = client.CreateOutputPort("Midimouse Output Port");
            }

            player = new MidiPlayer(db);
            player.OnProcessMidiMessage = OnProcessMidiMessage;

            //midiClient.CreateOutputPort("USB Midi");
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

        void OnProcessMidiMessage(int channel, int command, int data1, int data2)
        {

            //var channelInfo = channels[channel];
            switch (command)
            {
                case 0x80: // Note Off
                    {
                        int cmd = channel | command | (data1 << 8) | (data2 << 16);
                        outputPort.Send(midiOut, new MidiPacket[] { new MidiPacket(0, BitConverter.GetBytes(cmd)) });
                        //midiOut?.Send(cmd);
                    }
                    break;

                case 0x90: // Note On
                    {
                        int vol = (data2 * volume) / 100;
                        int cmd = channel | command | (data1 << 8) | (vol << 16);                       
                        outputPort.Send(midiOut, new MidiPacket[] { new MidiPacket(0, BitConverter.GetBytes(cmd)) });
                        //OnChannelEvent?.Invoke(this, new ChannelEvent() { channel = channel, data = data1 });
                        break;
                    }
                default:
                    {
                        int cmd = channel | command | (data1 << 8) | (data2 << 16);
                        outputPort.Send(midiOut, new MidiPacket[] { new MidiPacket(0, BitConverter.GetBytes(cmd)) });
                    }
                    break;

            }
        }
    }
}


