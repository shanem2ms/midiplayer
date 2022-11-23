using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using Avalonia;
using System.Collections.ObjectModel;
using midilib;
using NAudio.Wave;
using Avalonia.Media;

namespace PlayerAv
{
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
            get => new ObservableCollection<string>(player.AllSoundFonts);
        }
        public string CurrentSoundFont
        {
            get => player.CurrentSoundFont;
            set
            {
                FontReadyRect.Fill = Brushes.Red;
                player.ChangeSoundFont(value).ContinueWith((action) =>
                {
                    Dispatcher.UIThread.Post(() => FontReadyRect.Fill = Brushes.Green);
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
            VolumeSlider.PropertyChanged += VolumeSlider_PropertyChanged;


            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            //NextSong();
            player.Initialize(OnEngineCreate).ContinueWith((action) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SoundFonts"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSoundFont"));                
                player.OnChannelEvent += Player_OnChannelEvent;
                player.OnPlaybackTime += Player_OnPlaybackTime;
                player.OnPlaybackStart += Player_OnPlaybackStart;
                player.OnPlaybackComplete += Player_OnPlaybackComplete;
            });
            db.Initialize().ContinueWith((action) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
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


        private void Player_OnPlaybackTime(object? sender, TimeSpan e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                double lerp = e.TotalMilliseconds / currentSongTime.TotalMilliseconds;
                CurrentPosSlider.Value =
                    (CurrentPosSlider.Maximum - CurrentPosSlider.Minimum) * lerp +
                        CurrentPosSlider.Minimum;
            });
        }

        private void Player_OnChannelEvent(object? sender, MidiPlayer.ChannelEvent e)
        {
            if (e.channel < channelOutputs.Length)
            {
                Dispatcher.UIThread.Post(() =>
                    channelOutputs[e.channel].SetMidiData(e.data));
            }
        }

        private void VolumeSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Slider.ValueProperty)
            {
                if (player != null)
                    player.SetVolume((int)(double)e.NewValue);
            }
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