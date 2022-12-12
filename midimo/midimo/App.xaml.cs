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
        public delegate View OnAddGlViewDel();
        public OnAddGlViewDel OnAddGlView;

        public App (MidiDb _db, MidiPlayer _player, OnAddGlViewDel glView)
        {
            Instance = this;
            InitializeComponent();
            db = _db;
            player = _player;
            OnAddGlView = glView;
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

