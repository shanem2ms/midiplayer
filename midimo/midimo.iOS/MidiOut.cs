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


