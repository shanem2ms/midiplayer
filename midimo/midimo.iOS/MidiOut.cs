using CoreMidi;
using System;
using midilib;

namespace midimo.iOS
{
	public class MidiOut
	{
        MidiClient client;
        MidiEndpoint midiOut;
        MidiPort outputPort;
		MidiPlayer player;
        int volume = 5;

        public MidiOut()
		{
            player = App.Instance.player;
            if (Midi.DestinationCount > 0)
            {
                client = new MidiClient("Midimouse Client");
                midiOut = MidiEndpoint.GetDestination(Midi.DestinationCount - 1);
                outputPort = client.CreateOutputPort("Midimouse Output Port");
                player.OnProcessMidiMessage = OnProcessMidiMessage;
            }
        }

        void OnProcessMidiMessage(int channelwsa
    }
}


