using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using midilib;
using System.Collections.ObjectModel;
using System.Numerics;
using static midilib.MidiDb;

namespace midimo
{
    public partial class SongList : ContentView, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public IEnumerable<MidiDb.Fi> MidiFiles => db.FilteredMidiFiles;
        MidiDb db;
        MidiPlayer player;

        MidiDb.Fi selectedFile;
        public bool PianoMode { get; set; } = false;
        public MidiDb.Fi SelectedFile
        {
            get => selectedFile;
            set
            {
                selectedFile = value;
                OnSelectedFile(value);
            }
        }

        public SongList()
        {
            db = App.Instance.db;
            db.OnSearchResults += Db_OnSearchResults;
            player = App.Instance.player;
            this.BindingContext = this;
            InitializeComponent();
            Initialize();
        }

        private void Db_OnSearchResults(object sender, bool e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MidiFiles)));
        }

        void OnSelectedFile(MidiDb.Fi fi)
        {
            player.PlaySong(fi, PianoMode);
        }


        public void RestartSong()
        {            
            player.PlaySong(player.CurrentPlayingSong, PianoMode);
        }

        async void Initialize()
        {
            await db.Initialized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MidiFiles)));
        }

        void Entry_TextChanged(System.Object sender, Xamarin.Forms.TextChangedEventArgs e)
        {
            db.SearchStr = e.NewTextValue;
            //midiFiles = new ObservableCollection<MidiDb.Fi>(db.FilteredMidiFiles);
        }
    }
}

