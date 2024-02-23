using Avalonia.Controls;
using Avalonia.Media;
using midilib;
using Avalonia.Controls.Shapes;
using System.Collections.Generic;
using Avalonia.Input;

namespace midilonia.Views
{
    public partial class InteractivePiano : UserControl
    {
        MidiPlayer player = App.Player;
        class UIKey
        {
            public Rectangle r;
            public bool isBlack;
            public int midiNote;

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
            if (!pointerPressed)
                return;

            var point = e.GetCurrentPoint(sender as Control);
            KeyboardTouchPos(point);
        }

        bool pointerPressed = false;

        private void PianoCanvas_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            pointerPressed = false;
            if (pressedKey != null)
            {
                player.SynthEngine.NoteOff(pressedKey.midiNote);
                pressedKey.SetHit(false);
                pressedKey = null;
            }
        }

        private void PianoCanvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            pointerPressed = true;
            var point = e.GetCurrentPoint(sender as Control);
            KeyboardTouchPos(point);
        }


        void KeyboardTouchPos(PointerPoint point)
        {
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
                player.SynthEngine.NoteOff(pressedKey.midiNote);
                pressedKey.SetHit(false);
            }
            pressedKey = hitkey;
            if (pressedKey != null)
            {
                player.SynthEngine.NoteOn(pressedKey.midiNote, 100);
                pressedKey.SetHit(true);
            }
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
            Piano piano = new Piano();
            List<UIKey> keys = new List<UIKey>();
            int startKey = 24;
            int numKeys = 48;
            float leftX = piano.PianoKeys[startKey].x;
            float rightX = piano.PianoKeys[startKey + numKeys + 1].x;
            float xScale = 1.0f / (rightX - leftX);

            for (int i = 0; i < numKeys; i++)
            {
                int keynum = i + startKey;
                bool isBlack = piano.PianoKeys[keynum].isBlack;
                UIKey ui = new UIKey();
                ui.r = new Rectangle();
                ui.r.Width = piano.PianoWhiteXs * xScale * len;
                ui.r.Height = isBlack ? h / 2 : h;
                ui.r.Stroke = Brushes.DarkBlue;
                ui.r.StrokeThickness = 2;
                ui.isBlack = isBlack;
                ui.r.Fill = isBlack ? Brushes.Black : Brushes.White;
                ui.midiNote = keynum;
                Canvas.SetLeft(ui.r, (piano.PianoKeys[keynum].x - leftX) * xScale * len);
                Canvas.SetTop(ui.r, 0);
                ui.r.Tag = i + GMInstruments.MidiStartIdx;
                PianoCanvas.Children.Add(ui.r);
                keys.Add(ui);
            }

            uikeys = keys.ToArray();
        }
    }
}
