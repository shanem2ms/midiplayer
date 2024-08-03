using midilib;
using NAudio.Midi;
using NAudio.Wave;

namespace audiooutandroid
{
    public class AudioOut
    {
        bool enableMidi = false;
        MidiOut midiOut;
        NAudio.Wave.AVAudioEngineOut aVAudioEngineOut;
        public void OnEngineCreate(MidiSynthEngine midiSynthEngine)
        {
            if (enableMidi && MidiOut.NumberOfDevices > 0)
            {
                midiOut = new MidiOut(MidiOut.NumberOfDevices - 1);
                midiSynthEngine.SetMidiOut(OnProcessMidiMessage);
            }
            aVAudioEngineOut = new AVAudioEngineOut();
            aVAudioEngineOut.Init(midiSynthEngine);
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

        public void Dispose()
        {
            aVAudioEngineOut.Dispose();
        }

    }
}
