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

public partial class ArtistsSongs : UserControl
{
    public ArtistsSongs()
    {
        DataContext = App.ViewModel;
        InitializeComponent();
    }
      
}
