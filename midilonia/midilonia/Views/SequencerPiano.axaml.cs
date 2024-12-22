using Avalonia.Controls;
using Avalonia.Media;
using midilib;
using Avalonia.Controls.Shapes;
using System.Collections.Generic;
using Avalonia.Input;
using Amazon.Runtime.Internal.Transform;
using System.ComponentModel;
using System;
using Avalonia.Threading;
using System.Threading;

namespace midilonia.Views
{
    public partial class SequencerPiano : UserControl, INotifyPropertyChanged
    {
        MidiPlayer player = App.Player;
        public new event PropertyChangedEventHandler? PropertyChanged;
        int numKeys = 72;

        public string[] Instruments => GMInstruments.Names;

        public int CurrentOctave { get; set; } = 0;
        class UIKey
        {
            public Rectangle r;
            public bool isBlack;
            public int midiNote;
            bool isHit = false;
            bool isActive = false;

            public bool IsActive => isActive;
            public void SetHit(bool hit)
            {
                isHit = hit;
                UpdateColor();
            }

            public void SetActive(bool active)
            {
                isActive = active;
                UpdateColor();
            }

            void UpdateColor()
            {
                if (isHit)
                    r.Fill = Brushes.Red;
                else if (isActive)
                    r.Fill = Brushes.Salmon;
                else
                    r.Fill = isBlack ? Brushes.Black : Brushes.White;
            }
        }

        int currentPatch = 0;
        public int CurrentPatch { get => currentPatch; set { currentPatch = value; player.SynthEngine.SetPatch(currentPatch); } }
        class PointerState
        {
            public UIKey pressedKey = null;

        }

        UIKey[] uikeys;
        public SequencerPiano()
        {
            InitializeComponent();
            PianoCanvas.SizeChanged += PianoCanvas_SizeChanged;
            PianoCanvas.PointerPressed += PianoCanvas_PointerPressed;
            PianoCanvas.PointerReleased += PianoCanvas_PointerReleased;
            PianoCanvas.PointerMoved += PianoCanvas_PointerMoved;
            //GMInstruments.Names
        }

        ChannelCtrl channelCtrl;
        bool[] activeNotes = new bool[128]; 
        protected override void OnDataContextChanged(EventArgs e)
        {
            if (channelCtrl != null)
                channelCtrl.OnPlaybackCursorChanged -= ChannelCtrl_OnPlaybackCursorChanged;
            if (DataContext is ChannelCtrl)
            {
                channelCtrl = DataContext as ChannelCtrl;
                channelCtrl.OnPlaybackCursorChanged += ChannelCtrl_OnPlaybackCursorChanged;
            }
            else
                channelCtrl = null;
            base.OnDataContextChanged(e);
        }

        private void ChannelCtrl_OnPlaybackCursorChanged(object? sender, int e)
        {
            if (channelCtrl == null)
                return;
            channelCtrl.GetActiveNotes(e, activeNotes);
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var uikey in uikeys)
                {
                    uikey.SetActive(activeNotes[uikey.midiNote]);
                }
            });
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
            int startKey = 36 + CurrentOctave * 12;
            startKey = System.Math.Min(System.Math.Max(0, startKey), 128 - numKeys);
            float leftX = piano.PianoKeys[startKey].x;
            float rightX = piano.PianoKeys[startKey + numKeys - 1].x + piano.PianoWhiteXs;
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

        private void OctaveDn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CurrentOctave--;
            BuildPiano(PianoCanvas.Bounds.Size);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentOctave)));
        }

        private void OctaveUp_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CurrentOctave++;
            BuildPiano(PianoCanvas.Bounds.Size);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentOctave)));
        }

        private void PlayActive_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            List<int> midiNotes = new List<int>();
            foreach (var uikey in uikeys)
            {
                if (uikey.IsActive)
                {
                    player.SynthEngine.NoteOn(uikey.midiNote, 100);
                    midiNotes.Add(uikey.midiNote);
                }
            }

            Timer t = new Timer((object state) =>
            {
                foreach (int note in midiNotes)
                {
                    player.SynthEngine.NoteOff(note);
                }
            }, null, 1000, Timeout.Infinite);
        }

        private void CurrentCord_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            channelCtrl.GetActiveNotes(channelCtrl.PlaybackCursorPos, activeNotes);
        }
    }
}
