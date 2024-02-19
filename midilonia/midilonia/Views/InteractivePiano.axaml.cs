using Avalonia.Controls;
using Avalonia.Media;
using midilib;
using Avalonia.Controls.Shapes;

namespace midilonia.Views
{
    public partial class InteractivePiano : UserControl
    {
        public InteractivePiano()
        { 
            InitializeComponent();
            PianoCanvas.SizeChanged += PianoCanvas_SizeChanged;
        }

        private void PianoCanvas_SizeChanged(object? sender, SizeChangedEventArgs e)
        {            
            BuildPiano(e.NewSize);
        }

        void BuildPiano(Avalonia.Size size)
        {
            PianoCanvas.Children.Clear();
            double len = size.Width;
            double h = size.Height;
            Piano piano = new Piano(false);
            for (int i = 0; i < piano.PianoKeys.Length; i++)
            {
                bool isBlack = piano.PianoKeys[i].isBlack;

                Rectangle r = new Rectangle();
                r.Width = piano.PianoWhiteXs * len;
                r.Height = isBlack ? h / 2 : h;
                r.Stroke = Brushes.DarkBlue;
                r.StrokeThickness = 2;
                r.Fill = isBlack ? Brushes.Black : Brushes.White;
                Canvas.SetLeft(r, piano.PianoKeys[i].x * len);
                Canvas.SetTop(r, 0);
                //r.MouseDown += R_MouseDown;
                //r.MouseUp += R_MouseUp;
                r.Tag = i + GMInstruments.MidiStartIdx;
                PianoCanvas.Children.Add(r);
            }
        }
    }
}
