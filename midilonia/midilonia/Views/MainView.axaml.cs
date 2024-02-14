using Avalonia.Controls;
using midilib;
using NAudio.Midi;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static midilib.MidiDb;

namespace midilonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        DataContext = App.ViewModel;
        InitializeComponent();
    }
      
}
