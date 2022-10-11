using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using midiplayer;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using System;
using Tmds.DBus;
using NAudio.Wave;
using NAudio.Midi;
using Avalonia;

namespace PlayerAv
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        WaveOut aVAudioEngineOut;
        //NAudio.Wave.AVAudioEngineOut aVAudioEngineOut;
        MidiSampleProvider player;
        MeltySynth.MidiFile currentMidiFile;
        HttpClient httpClient = new HttpClient();
        List<string> midiFiles = new List<string>();
        int volume = 100;
        
        public new event PropertyChangedEventHandler? PropertyChanged;
        public IEnumerable<string> MidiFiles => midiFiles;
        public string CurrentSong { get; set; }
        MidiOut midiOut;

        async Task<MeltySynth.MidiFile> PlayFile()
        {
            Uri uri = new Uri(@"https://bushgrafts.com/jazz/AintMisbehavin.MID");

            var response = await httpClient.GetAsync(uri);
            Stream stream = response.Content.ReadAsStream();
            return new MeltySynth.MidiFile(stream);
        }
        string homedir = @"/Users/shanemorrison/Perforce/shanemac/midiplayer";
        string PlaylistDir => Path.Combine(homedir, "Playlist");
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            VolumeSlider.PropertyChanged += VolumeSlider_PropertyChanged;
            player = new MidiSampleProvider(Path.Combine(homedir, "TimGM6mb.sf2"));
            player.Sequencer.OnPlaybackTime += Sequencer_OnPlaybackTime;
            player.Sequencer.OnPlaybackComplete += Sequencer_OnPlaybackComplete;
            player.Sequencer.OnProcessMidiMessage = OnProcessMidiMessage;


#if WIN
            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.Init(player);
            waveOut.Play();
            
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                var devinfo = MidiOut.DeviceInfo(device).ProductName;
            }
            midiOut = new MidiOut(1);
#else
            aVAudioEngineOut = new WaveOut();
            aVAudioEngineOut.Init(player);              
            aVAudioEngineOut.Play();
               
#endif
            DirectoryInfo di = new DirectoryInfo(PlaylistDir);
            foreach (FileInfo fi in di.GetFiles("*.mid"))
            {
                midiFiles.Add(fi.Name);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            //NextSong();
        }

        private void VolumeSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Slider.ValueProperty)
            {
                volume = (int)(double)e.NewValue;
                if (player != null)
                    player.SetVolume(volume);
            }
        }

        void OnProcessMidiMessage(int channel, int command, int data1, int data2)
        {
            //var channelInfo = channels[channel];
            if (midiOut == null) return;
            switch (command)
            {
                case 0x80: // Note Off
                    {
                        int cmd = channel | command | (data1 << 8) | (data2 << 16);
                        midiOut.Send(cmd);
                    }
                    break;

                case 0x90: // Note On
                    {
                        int vol = (data2 * volume) / 100;
                        int cmd = channel | command | (data1 << 8) | (vol << 16);
                        midiOut.Send(cmd);
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
        private void Sequencer_OnPlaybackComplete(object? sender, bool e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                NextSong();
            });
        }

        private void Sequencer_OnPlaybackTime(object? sender, TimeSpan e)
        {
            double percentDone = e.TotalMilliseconds / currentMidiFile.Length.TotalMilliseconds;
            Dispatcher.UIThread.Post(() =>
            {
                CurrentPosSlider.Value = CurrentPosSlider.Minimum +
                    percentDone * (CurrentPosSlider.Maximum - CurrentPosSlider.Minimum);
            });
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {

        }
        void NextSong()
        {
            Random r = new Random();
            int rVal = r.Next(midiFiles.Count);
            PlaySong(rVal);
        }
        void PlaySong(int idx)
        {
            string path = Path.Combine(PlaylistDir, midiFiles[idx]);
            // Load the MIDI file.
            var midiFile = new MeltySynth.MidiFile(path);
            player.Play(midiFile);
            currentMidiFile = midiFile;
            CurrentSong = midiFiles[idx];
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSong"));

        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            NextSong();
        }

    }
}