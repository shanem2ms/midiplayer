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
        ObservableCollection<midiplayer.MidiFI> jazzMidiFiles = new ObservableCollection<midiplayer.MidiFI>();
        ObservableCollection<midiplayer.MidiFI> bitMidiFiles = new ObservableCollection<midiplayer.MidiFI>();
        
        public new event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<midiplayer.MidiFI> JazzMidiFiles => jazzMidiFiles;
        public ObservableCollection<midiplayer.MidiFI> BitmidiFiles => bitMidiFiles;
        public string CurrentSong { get; set; }
        ChannelOutput[] channelOutputs;
        midiplayer.MidiPlayer player;

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
            foreach (var midi in player.jazzMidiFiles)
            {
                jazzMidiFiles.Add(midi);
            }
            foreach (var midi in player.bitMidiFiles)
            {
                bitMidiFiles.Add(midi);
            }
            //NextSong();
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

        private void JazzMidi_SelectedItemsChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                PlaySong(e.AddedItems[0] as MidiFI);
            }
        }
        private void BitMidi_SelectedItemsChanged(object? sender, SelectionChangedEventArgs e)
        {

        }
        private void Prev_Click(object sender, RoutedEventArgs e)
        {

        }
        void NextSong()
        {
            Random r = new Random();
            int rVal = r.Next(player.jazzMidiFiles.Count);
            PlaySong(player.jazzMidiFiles[rVal]);
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