using System;
using midilib;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace midimo
{
    public partial class App : Application
    {
        public MidiDb db;
        public MidiPlayer player;

        public static App Instance;

        public App (MidiDb _db, MidiPlayer _player)
        {
            Instance = this;
            InitializeComponent();
            db = _db;
            player = _player;
            MainPage = new MainPage();
        }

        protected override void OnStart ()
        {
        }

        protected override void OnSleep ()
        {
        }

        protected override void OnResume ()
        {
        }
    }
}

