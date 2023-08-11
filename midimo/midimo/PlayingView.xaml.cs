using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using midilib;
using System.ComponentModel;

namespace midimo
{
    public partial class PlayingView : ContentView, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        MidiPlayer player = App.Instance.player;
        ChannelOutput[] channelOutputs;

        public PlayingView()
        {
            InitializeComponent();
            channelOutputs = new ChannelOutput[] {
                Ch0, Ch1, Ch2, Ch3, Ch4, Ch5, Ch6, Ch7,
            Ch8, Ch9, Ch10, Ch11, Ch12, Ch13, Ch14, Ch15 };
            for (int i = 0; i < channelOutputs.Length; ++i)
            {
                channelOutputs[i].ChannelColor = Color.FromRgb(
                    (int)(NoteVis.ChannelColors[i].X * 255),
                    (int)(NoteVis.ChannelColors[i].Y * 255),
                    (int)(NoteVis.ChannelColors[i].Z * 255));
            }
            player.OnChannelEvent += Player_OnChannelEvent;
            player.OnPlaybackStart += Player_OnPlaybackStart;
            MainStack.Children.Insert(0, App.Instance.OnAddGlView());
        }

        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            foreach (ChannelOutput c in channelOutputs)
            {
                c.ResetForSong();
            }
        }

        private void Player_OnChannelEvent(object? sender, MidiPlayer.ChannelEvent e)
        {
            if (e.channel < channelOutputs.Length)
            {
                Dispatcher.BeginInvokeOnMainThread(() =>
                    channelOutputs[e.channel].SetMidiData(e));
            }
        }
    }
}

