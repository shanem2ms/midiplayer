using midilib;
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

namespace PlayerWPF
{
    /// <summary>
    /// Interaction logic for Sequencer.xaml
    /// </summary>
    public partial class Sequencer : UserControl
    {
        MidiPlayer player = App.Player;
        MeltySynth.MidiFile midiFile;
        public Sequencer()
        {
            player.OnPlaybackStart += Player_OnPlaybackStart;
            InitializeComponent();
        }

        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            midiFile = e.midiFile;
        }

        void Relayout()
        {
            midiFile.Length
        }
    }
}
