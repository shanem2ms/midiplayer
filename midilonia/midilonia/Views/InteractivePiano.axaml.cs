using Avalonia.Controls;
using Avalonia.Media;
using midilib;
using Avalonia.Controls.Shapes;
using System.Collections.Generic;

namespace midilonia.Views
{
    public partial class InteractivePiano : UserControl
    {
        class UIKey
        {
            public Rectangle r;
            public bool isBlack;

            public void SetHit(bool hit)
            {
                r.Fill = hit ? Brushes.Red : isBlack ? Brushes.Black : Brushes.White;
            }
        }

        UIKey[] uikeys;
        UIKey pressedKey = null;
        public InteractivePiano()
        { 
            InitializeComponent();
            PianoCanvas.SizeChanged += PianoCanvas_SizeChanged;
            PianoCanvas.PointerPressed += PianoCanvas_PointerPressed;
            PianoCanvas.PointerReleased += PianoCanvas_PointerReleased;
            PianoCanvas.PointerMoved += PianoCanvas_PointerMoved;
        }

        private void PianoCanvas_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            var point = e.GetCurrentPoint(sender as Control);
            UIKey hitkey = null;
            foreach (var key in uikeys)
            {
                if (!key.isBlack)
                    continue;
                if (key.r.Bounds.Contains(point.Position))
                {
                    hitkey = key;
                    break;
                }
            }

            if (hitkey == null)
            {
                foreach (var key in uikeys)
                {
                    if (key.isBlack)
                        continue;
                    if (key.r.Bounds.Contains(point.Position))
                        hitkey = key;
                }
            }

            if (hitkey == pressedKey)
                return;
            if (pressedKey != null)
            {
                pressedKey.SetHit(false);
            }
            pressedKey = hitkey;
            if (pressedKey != null)
            {
                pressedKey.SetHit(true);
            }
        }

        private void PianoCanvas_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
        }

        private void PianoCanvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
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
            List<UIKey> keys = new List<UIKey>();
            for (int i = 0; i < piano.PianoKeys.Length; i++)
            {
                bool isBlack = piano.PianoKeys[i].isBlack;
                UIKey ui = new UIKey();
                ui.r = new Rectangle();
                ui.r.Width = piano.PianoWhiteXs * len;
                ui.r.Height = isBlack ? h / 2 : h;
                ui.r.Stroke = Brushes.DarkBlue;
                ui.r.StrokeThickness = 2;
                ui.isBlack = isBlack;
                ui.r.Fill = isBlack ? Brushes.Black : Brushes.White;
                Canvas.SetLeft(ui.r, piano.PianoKeys[i].x * len);
                Canvas.SetTop(ui.r, 0);
                //r.MouseDown += R_MouseDown;
                //r.MouseUp += R_MouseUp;
                ui.r.Tag = i + GMInstruments.MidiStartIdx;
                PianoCanvas.Children.Add(ui.r);
                keys.Add(ui);
            }

            uikeys = keys.ToArray();
        }
    }
}
