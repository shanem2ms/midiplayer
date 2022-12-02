using System;
using midilib;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace midimo
{
    public partial class App : Application
    {
        MidiDb db;
        MidiPlayer player;

        public App (MidiDb _db, MidiPlayer _player)
        {
            InitializeComponent();
            db = _db;
            player = _player;
            MainPage = new MainPage(db, player);
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

