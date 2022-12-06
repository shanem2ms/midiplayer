using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using midilib;
using System.ComponentModel;

namespace midimo
{
    public partial class SynthList : ContentView, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<string> Synths =>
            new  ObservableCollection<string>(db.AllSoundFonts);
        MidiDb db;
        MidiPlayer player;

        string selectedSynth;
        public string SelectedSynth
        {
            get => selectedSynth;
            set
            {
                selectedSynth = value;
                player.ChangeSoundFont(selectedSynth);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSynth)));
            }
        }

        public SynthList()
        {
            db = App.Instance.db;
            db.OnIntialized += Db_OnIntialized;
            player = App.Instance.player;
            this.BindingContext = this;
            this.selectedSynth = player.CurrentSoundFont;
            InitializeComponent();
            
        }

        private void Db_OnIntialized(object sender, bool e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Synths)));
        }
    }
}

