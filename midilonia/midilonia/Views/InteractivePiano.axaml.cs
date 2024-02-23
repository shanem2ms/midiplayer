using Avalonia.Controls;
using Avalonia.Media;
using midilib;
using Avalonia.Controls.Shapes;
using System.Collections.Generic;
using Avalonia.Input;
using Amazon.Runtime.Internal.Transform;

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

        class PointerState
        {
            public UIKey pressedKey = null;

        }

        UIKey[] uikeys;
        public InteractivePiano()
        {
            InitializeComponent();
            PianoCanvas.SizeChanged += PianoCanvas_SizeChanged;
            PianoCanvas.PointerPressed += PianoCanvas_PointerPressed;
            PianoCanvas.PointerReleased += PianoCanvas_PointerReleased;
            PianoCanvas.PointerMoved += PianoCanvas_PointerMoved;
        }

        Dictionary<IPointer, PointerState> pointerMap = new Dictionary<IPointer, PointerState>();

        private void PianoCanvas_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            PointerState ps;
            if (!pointerMap.TryGetValue(e.Pointer, out ps))
                return;

            var point = e.GetCurrentPoint(sender as Control);
            KeyboardTouchPos(ps, point);
        }



        private void PianoCanvas_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            PointerState ps;
            if (!pointerMap.TryGetValue(e.Pointer, out ps))
                return;

            if (ps.pressedKey != null)
            {
                player.SynthEngine.NoteOff(ps.pressedKey.midiNote);
                ps.pressedKey.SetHit(false);
                ps.pressedKey = null;
            }
            pointerMap.Remove(e.Pointer);
        }

        private void PianoCanvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(sender as Control);
            PointerState ps = new PointerState();
            pointerMap.Add(e.Pointer, ps);
            KeyboardTouchPos(ps, point);
        }


        void KeyboardTouchPos(PointerState ps, PointerPoint point)
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

            if (hitkey == ps.pressedKey)
                return;
            if (ps.pressedKey != null)
            {
                player.SynthEngine.NoteOff(ps.pressedKey.midiNote);
                ps.pressedKey.SetHit(false);
            }
            ps.pressedKey = hitkey;
            if (ps.pressedKey != null)
            {
                player.SynthEngine.NoteOn(ps.pressedKey.midiNote, 100);
                ps.pressedKey.SetHit(true);
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
