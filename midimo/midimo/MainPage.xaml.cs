﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using midilib;
using System.Collections.ObjectModel;

namespace midimo
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    { 
        MidiDb db;
        MidiPlayer player;
        public event PropertyChangedEventHandler PropertyChanged;
        TimeSpan songLength = new TimeSpan();
        public string CurrentSong { get; set; }

        public MidiDb Db
        {
            get => db;
            set => db = value;
        }
        public MidiPlayer Player => player;

        public MainPage()
        {
            this.BindingContext = this;
            db = App.Instance.db;
            player = App.Instance.player;
            player.OnPlaybackComplete += Player_OnPlaybackComplete;
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackTime += Player_OnPlaybackTime;
            InitializeComponent();

            SongPosSlider.ValueChanged += S_ValueChanged;
            
        }

        bool noValChangeEvent = false;
        private void Player_OnPlaybackTime(object sender, MeltySynth.MidiSynthSequencer.PlaybackTimeArgs e)
        {
            double lerp = ((double)e.timeSpan.Ticks / (double)songLength.Ticks);
            Dispatcher.BeginInvokeOnMainThread(() =>
            {
                noValChangeEvent = true;
                SongPosSlider.Value = (SongPosSlider.Maximum - SongPosSlider.Minimum) * lerp +
                    (SongPosSlider.Minimum);
                noValChangeEvent = false;
            });
        }

        private void Player_OnPlaybackStart(object sender, MidiPlayer.PlaybackStartArgs e)
        {
            this.CurrentSong = e.file.Name;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSong)));
            songLength = e.midiFile.Length;
        }

        private void Player_OnPlaybackComplete(object sender, bool e)
        {

        }

        private void S_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (noValChangeEvent)
                return;
            double lerp = (e.NewValue - SongPosSlider.Minimum) /
                (SongPosSlider.Maximum - SongPosSlider.Minimum);
            TimeSpan ts = songLength * lerp;
            player.Seek(ts);
        }

        void RefreshTabs(object sender)
        {
            foreach (var child in TabButtonsStack.Children)
            {
                if (child is Button btn)
                {
                    btn.BackgroundColor = sender == child ?
                        Color.DarkBlue :
                        Color.Transparent;
                }
            }
        }

        void Songs_Pressed(System.Object sender, System.EventArgs e)
        {
            SongList.IsVisible = true;
            SynthList.IsVisible = false;
            PlayingView.IsVisible = false;
            RefreshTabs(sender);
        }

        void Synths_Pressed(System.Object sender, System.EventArgs e)
        {
            SongList.IsVisible = false;
            SynthList.IsVisible = true;
            PlayingView.IsVisible = false;
            RefreshTabs(sender);
        }

        void Playing_Pressed(System.Object sender, System.EventArgs e)
        {
            SongList.IsVisible = false;
            SynthList.IsVisible = false;
            PlayingView.IsVisible = true;
            RefreshTabs(sender);
        }

        private void PianoMode_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            SongList.PianoMode = e.Value;
            SongList.RestartSong();
        }
    }
}

