using midilib;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<MidiDb.Fi> MidiFiles => db.FilteredMidiFiles != null ? new
            ObservableCollection<MidiDb.Fi>(db.FilteredMidiFiles) : null;
        public string CurrentSong { get; set; }
        ChannelOutput[] channelOutputs;
        MidiDb db = new MidiDb();
        MidiPlayer player;
        WaveOut waveOut;
        TimeSpan currentSongTime;

        public ObservableCollection<string> SoundFonts
        {
            get => new ObservableCollection<string>(db.AllSoundFonts);
        }
        public string CurrentSoundFont
        {
            get => player.CurrentSoundFont;
            set
            {
                FontReadyRect.Fill = Brushes.Red;
                player.ChangeSoundFont(value).ContinueWith((action) =>
                {
                    Dispatcher.BeginInvoke(() => FontReadyRect.Fill = Brushes.Green);
                });
            }
        }

        public string SearchStr
        {
            get => db.SearchStr;
            set
            {
                db.SearchStr = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            }
        }

        public MainWindow()
        {
            player = new MidiPlayer(db);
            this.DataContext = this;
            InitializeComponent();

            channelOutputs = new ChannelOutput[] {
                Ch0, Ch1, Ch2, Ch3, Ch4, Ch5, Ch6, Ch7,
            Ch8, Ch9, Ch10, Ch11, Ch12, Ch13, Ch14, Ch15 };
            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            CurrentPosSlider.ValueChanged += CurrentPosSlider_ValueChanged;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            //NextSong();
            player.Initialize(OnEngineCreate).ContinueWith((action) =>
            {
                player.OnChannelEvent += Player_OnChannelEvent;
                player.OnPlaybackTime += Player_OnPlaybackTime;
                player.OnPlaybackStart += Player_OnPlaybackStart;
                player.OnPlaybackComplete += Player_OnPlaybackComplete;
            });
            db.Initialize().ContinueWith((action) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SoundFonts"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSoundFont"));
            });
        }

        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            currentSongTime = e.timeSpan;
        }

        void OnEngineCreate(MidiSampleProvider midiSampleProvider)
        {
            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.Init(midiSampleProvider);
            waveOut.Play();
        }
        private void Player_OnPlaybackComplete(object? sender, bool e)
        {
            NextSong();
        }


        bool isChanging = false;
        private void Player_OnPlaybackTime(object? sender, TimeSpan e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                double lerp = e.TotalMilliseconds / currentSongTime.TotalMilliseconds;
                isChanging = true;
                CurrentPosSlider.Value =
                    (CurrentPosSlider.Maximum - CurrentPosSlider.Minimum) * lerp +
                        CurrentPosSlider.Minimum;
                isChanging = false;
            });
        }

        private void CurrentPosSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isChanging)
                return;
            double lerp = 
                (CurrentPosSlider.Value - CurrentPosSlider.Minimum) / (CurrentPosSlider.Maximum - CurrentPosSlider.Minimum);
            TimeSpan t = new TimeSpan((long)(currentSongTime.Ticks * lerp));
            player.Seek(t);
        }

        private void Player_OnChannelEvent(object? sender, MidiPlayer.ChannelEvent e)
        {
            if (e.channel < channelOutputs.Length)
            {
                Dispatcher.BeginInvoke(() =>
                    channelOutputs[e.channel].SetMidiData(e));
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player != null)
                player.SetVolume((int)(double)e.NewValue);
        }

        private void Midi_SelectedItemsChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                PlaySong(e.AddedItems[0] as MidiDb.Fi);
            }
        }
        private void Prev_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Playing_Click(object sender, RoutedEventArgs e)
        {
            PlayingCtrl.Visibility = Visibility.Visible;
            SongsLb.Visibility = Visibility.Collapsed;
        }

        private void Songs_Click(object sender, RoutedEventArgs e)
        {
            PlayingCtrl.Visibility = Visibility.Collapsed;
            SongsLb.Visibility = Visibility.Visible;
        }

        void NextSong()
        {
            //PlaySong(player.GetNextSong());
        }
        void PlaySong(MidiDb.Fi midiFI)
        {
            player.PlaySong(midiFI);
            CurrentSong = midiFI.Name;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSong"));

        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            NextSong();
        }

    }
}
