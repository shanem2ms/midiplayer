using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Commons.Music.Midi;
using CoreMidi;
using MeltySynth;
using NAudio.Wave;

namespace midiplayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WaveOut waveOut;
        public MainWindow()
        {
            InitializeComponent();

            var player = new MidiSampleProvider(@"C:\homep4\midiplayer\TimGM6mb.sf2");

            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.Init(player);
            waveOut.Play();

            // Load the MIDI file.
            var midiFile = new MeltySynth.MidiFile(@"C:\Users\shane\Downloads\Movie_Themes_-_Ghostbusters.mid");

            // Play the MIDI file.
            player.Play(midiFile, true);            
        }
    }
}
