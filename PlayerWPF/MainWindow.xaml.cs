using midilib;
using NAudio.Wave;
using NAudio.Midi;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static MeltySynth.MidiSynthSequencer;

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
        public string CurrentSong => player.CurrentPlayingSong?.Name;
        MidiDb db = App.Db;
        MidiPlayer player = App.Player;
        WaveOut waveOut;
        MidiOut midiOut;
        TimeSpan currentSongTime;
        bool enableMidi = false;

        public bool PianoMode { get; set; } = false;

        public ObservableCollection<MidiDb.SoundFontDesc> SoundFonts
        {
            get => new ObservableCollection<MidiDb.SoundFontDesc>(db.AllSoundFonts);
        }
        public MidiDb.SoundFontDesc CurrentSoundFont
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
            this.DataContext = this;
            InitializeComponent();

            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
            CurrentPosSlider.ValueChanged += CurrentPosSlider_ValueChanged;

            SequencerCtrl.Visibility = Visibility.Visible;
            PlayingCtrl.Visibility = Visibility.Collapsed;
            SongsGrid.Visibility = Visibility.Collapsed;

            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            Initialize();            
        }

        private async Task<bool> Initialize()
        {
            //await db.UploadAWS();
            await db.InitializeMappings();
            db.InitSongList(false);
            player.OnPlaybackTime += Player_OnPlaybackTime;
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackComplete += Player_OnPlaybackComplete;
            
            await player.Initialize(OnEngineCreate);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SoundFonts"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSoundFont"));
            return true;
        }
        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            currentSongTime = e.midiFile.Length;
        }

        void OnEngineCreate(MidiSynthEngine midiSynthEngine)
        {
            if (enableMidi && MidiOut.NumberOfDevices > 0)
            {
                midiOut = new MidiOut(MidiOut.NumberOfDevices-1);
                midiSynthEngine.SetMidiOut(OnProcessMidiMessage);
            }
            waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            waveOut.Init(midiSynthEngine);
            waveOut.Play();
        }
        private void Player_OnPlaybackComplete(object? sender, bool e)
        {
            NextSong();
        }


        bool isChanging = false;
        private void Player_OnPlaybackTime(object? sender, PlaybackTimeArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                double lerp = e.timeSpan.TotalMilliseconds / currentSongTime.TotalMilliseconds;
                isChanging = true;
                CurrentPosSlider.Value =
                    (CurrentPosSlider.Maximum - CurrentPosSlider.Minimum) * lerp +
                        CurrentPosSlider.Minimum;
                isChanging = false;
            });
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
        private void CurrentPosSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isChanging)
                return;
            double lerp = 
                (CurrentPosSlider.Value - CurrentPosSlider.Minimum) / (CurrentPosSlider.Maximum - CurrentPosSlider.Minimum);
            TimeSpan t = new TimeSpan((long)(currentSongTime.Ticks * lerp));
            player.Seek(t);
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
            SongsGrid.Visibility = Visibility.Collapsed;
            SequencerCtrl.Visibility = Visibility.Collapsed;
        }

        private void Sequencer_Click(object sender, RoutedEventArgs e)
        {
            SequencerCtrl.Visibility = Visibility.Visible;
            PlayingCtrl.Visibility = Visibility.Collapsed;
            SongsGrid.Visibility = Visibility.Collapsed;
        }

        private void Songs_Click(object sender, RoutedEventArgs e)
        {
            PlayingCtrl.Visibility = Visibility.Collapsed;
            SongsGrid.Visibility = Visibility.Visible;
            SequencerCtrl.Visibility = Visibility.Collapsed;
        }

        void NextSong()
        {
            PlaySong(player.GetNextSong());
        }
        void PlaySong(MidiDb.Fi midiFI)
        {
            player.PlaySong(midiFI, PianoMode);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSong"));

        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            NextSong();
        }
        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
            player.PauseOrUnPause(!player.IsPaused);
        }

        protected override void OnClosed(EventArgs e)
        {
            player.Dispose();
            base.OnClosed(e);
        }

    }
}
