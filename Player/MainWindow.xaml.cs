using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        HttpClient httpClient = new HttpClient();
        List<string> midiFiles = new List<string>();

        public IEnumerable<string> MidiFiles => midiFiles;

        public event PropertyChangedEventHandler? PropertyChanged;

        async Task<MeltySynth.MidiFile> PlayFile()
        {
            Uri uri = new Uri(@"https://bushgrafts.com/jazz/AintMisbehavin.MID");

            var response = await httpClient.GetAsync(uri);
            Stream stream = response.Content.ReadAsStream();
            return new MeltySynth.MidiFile(stream);
        }
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            string homedir = @"C:\homep4\midiplayer";

            player = new MidiSampleProvider(Path.Combine(homedir, "TimGM6mb.sf2"));

            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.Init(player);
            waveOut.Play();


            string playlistDir = Path.Combine(homedir, "Playlist");
            DirectoryInfo di = new DirectoryInfo(playlistDir);
            foreach (FileInfo fi in di.GetFiles("*.mid"))
            {
                midiFiles.Add(fi.Name);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));

            Random r = new Random();
            int rVal = r.Next(midiFiles.Count());
            string path = Path.Combine(playlistDir, midiFiles[rVal]);
            // Load the MIDI file.
            var midiFile = new MeltySynth.MidiFile(path);
            player.Play(midiFile, true);
            /*
            PlayFile().ContinueWith((Task<MidiFile> task) =>
            {
                // Play the MIDI file.
                player.Play(task.Result, true);
            });*/

        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player != null)
                player.SetVolume((int)e.NewValue);
        }
    }
}
