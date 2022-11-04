using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;
using System;
using Tmds.DBus;
using Avalonia;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Linq;
using System.Collections.ObjectModel;
using MeltySynth;
using System.Threading.Channels;
using midiplayer;

namespace PlayerAv
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler? PropertyChanged;        
        public ObservableCollection<midiplayer.MidiFI> MidiFiles => new
            ObservableCollection<midiplayer.MidiFI>(player.FilteredMidiFiles);
        public string CurrentSong { get; set; }
        ChannelOutput[] channelOutputs;
        midiplayer.MidiPlayer player;
        TimeSpan currentSongTime;

        public string SearchStr
        {
            get => player.SearchStr;
            set
            {
                player.SearchStr = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            }
        }

        public MainWindow()
        {
            player = new midiplayer.MidiPlayer();
            this.DataContext = this;
            InitializeComponent();

            channelOutputs = new ChannelOutput[] {
                Ch0, Ch1, Ch2, Ch3, Ch4, Ch5, Ch6, Ch7,
            Ch8, Ch9, Ch10, Ch11, Ch12, Ch13, Ch14, Ch15 };
            VolumeSlider.PropertyChanged += VolumeSlider_PropertyChanged;


            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MidiFiles"));
            player.OnChannelEvent += Player_OnChannelEvent;
            player.OnPlaybackTime += Player_OnPlaybackTime;
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackComplete += Player_OnPlaybackComplete;
            //NextSong();
        }

        private void Player_OnPlaybackComplete(object? sender, bool e)
        {
            NextSong();
        }

        private void Player_OnPlaybackStart(object? sender, TimeSpan e)
        {
            currentSongTime = e;
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

        private void Player_OnChannelEvent(object? sender, midiplayer.MidiPlayer.ChannelEvent e)
        {
            if (e.channel < channelOutputs.Length)
            {
                channelOutputs[e.channel].SetMidiData(e.data);
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
                PlaySong(e.AddedItems[0] as MidiFI);
            }
        }
        private void Prev_Click(object sender, RoutedEventArgs e)
        {

        }
        void NextSong()
        {
            PlaySong(player.GetNextSong());
        }
        void PlaySong(MidiFI midiFI)
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