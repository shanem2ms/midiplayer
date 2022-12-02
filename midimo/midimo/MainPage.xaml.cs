using System;
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

        //ObservableCollection<MidiDb.Fi> midiFiles;
        //public ObservableCollection<MidiDb.Fi> MidiFiles => midiFiles;
        public IEnumerable<MidiDb.Fi> MidiFiles => db.FilteredMidiFiles;

        MidiDb.Fi selectedFile;
        public MidiDb.Fi SelectedFile
        {
            get => selectedFile;
            set {
                selectedFile = value;
                OnSelectedFile(value);
            }
        }
        public MainPage(MidiDb _db, MidiPlayer _player)
        {
            this.BindingContext = this;
            db = _db;
            player = _player;
            Initialize();
            InitializeComponent();
            
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

