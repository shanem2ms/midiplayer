using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Commons.Music.Midi;
using CoreMidi;
using MeltySynth;
using NAudio.Midi;
using NAudio.Wave;

namespace midiplayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        WaveOut waveOut;
        MidiSampleProvider player;
        MeltySynth.MidiFile currentMidiFile;
        HttpClient httpClient = new HttpClient();
        List<string> midiFiles = new List<string>();

        public IEnumerable<string> MidiFiles => midiFiles;
        public string CurrentSong { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;


        async Task<MeltySynth.MidiFile> PlayFile()
        {
            Uri uri = new Uri(@"https://bushgrafts.com/jazz/AintMisbehavin.MID");

            var response = await httpClient.GetAsync(uri);
            Stream stream = response.Content.ReadAsStream();
            return new MeltySynth.MidiFile(stream);
        }
        string homedir = @"C:\homep4\midiplayer";
        string PlaylistDir => Path.Combine(homedir, "Playlist");
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            for (int device = 0; device < MidiOut.NumberOfDevices; device++)
            {
                var midiOut = MidiOut.DeviceInfo(device);
            }

            player = new MidiSampleProvider(Path.Combine(homedir, "TimGM6mb.sf2"));
            player.Sequencer.OnPlaybackTime += Sequencer_OnPlaybackTime;
            player.Sequencer.OnPlaybackComplete += Sequencer_OnPlaybackComplete;

            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.Init(player);
            waveOut.Play();


            DirectoryInfo di = new DirectoryInfo(PlaylistDir);
            foreach (FileInfo fi in di.GetFiles("*.mid"))
            {
                midiFiles.Add(fi.Name);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            NextSong();
        }
        private void Sequencer_OnPlaybackComplete(object? sender, bool e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                NextSong();
            });
        }

        private void Sequencer_OnPlaybackTime(object? sender, TimeSpan e)
        {
            double percentDone = e.TotalMilliseconds / currentMidiFile.Length.TotalMilliseconds;
            Dispatcher.BeginInvoke(() =>
            {
                CurrentPosSlider.Value = CurrentPosSlider.Minimum +
                    percentDone * (CurrentPosSlider.Maximum - CurrentPosSlider.Minimum);
            });
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player != null)
                player.SetVolume((int)e.NewValue);
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {

        }
        void NextSong()
        {
            Random r = new Random();
            int rVal = r.Next(midiFiles.Count());
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
