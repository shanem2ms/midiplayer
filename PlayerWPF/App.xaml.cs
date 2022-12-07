using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using midilib;

namespace PlayerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MidiDb Db { get; set; } = new MidiDb();
        public static MidiPlayer Player { get; set; } = new MidiPlayer(Db);

    }
}
