using midilib;
using NAudio.Midi;
using NAudio.Wave;

namespace audioout.Droid
{
    public class AudioOut
    {
        bool enableMidi = false;
        MidiOut midiOut;
        PortAudio portAudio;
        public void OnEngineCreate(MidiSynthEngine midiSynthEngine)
        {
            if (enableMidi && MidiOut.NumberOfDevices > 0)
            {
                midiOut = new MidiOut(MidiOut.NumberOfDevices - 1);
                midiSynthEngine.SetMidiOut(OnProcessMidiMessage);
            }

            portAudio = new PortAudio();
            portAudio.Init(midiSynthEngine);
            portAudio.Play();
            //waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            // waveOut.Init(midiSynthEngine);
            //waveOut.Play();
        }
        void OnProcessMidiMessage(int channel, int command, int data1, int data2)
        {
            //var channelInfo = channels[channel];
            switch (command)
            {
                case 0x80: // Note Off
                    {
                        int cmd = channel | command | (data1 << 8) | (data2 << 16);
                        midiOut.Send(cmd);
                        //midiOut?.Send(cmd);
                    }
                    break;

                case 0x90: // Note On
                    {
                        int cmd = channel | command | (data1 << 8) | (data2 << 16);
                        midiOut.Send(cmd);
                        //OnChannelEvent?.Invoke(this, new ChannelEvent() { channel = channel, data = data1 });
                        break;
                    }
                default:
                    {
                        int cmd = channel | command | (data1 << 8) | (data2 << 16);
                        midiOut.Send(cmd);
                    }
                    break;

            }
        }

    }
}
