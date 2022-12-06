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

namespace midimo
{
    public partial class SongList : ContentView, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public IEnumerable<MidiDb.Fi> MidiFiles => db.FilteredMidiFiles;
        MidiDb db;
        MidiPlayer player;

        MidiDb.Fi selectedFile;
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
            player = App.Instance.player;
            this.BindingContext = this;
            InitializeComponent();
            Initialize();
        }

        void OnSelectedFile(MidiDb.Fi fi)
        {
            player.PlaySong(fi);
        }

        async void Initialize()
        {
            await db.Initialize();
            //midiFiles = new ObservableCollection<MidiDb.Fi>(db.FilteredMidiFiles);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MidiFiles)));
        }

        void Entry_TextChanged(System.Object sender, Xamarin.Forms.TextChangedEventArgs e)
        {
            db.SearchStr = e.NewTextValue;
            //midiFiles = new ObservableCollection<MidiDb.Fi>(db.FilteredMidiFiles);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MidiFiles)));
        }
    }
}

