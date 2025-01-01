using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using Avalonia.Threading;
using DynamicData;
using midilib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace midilonia.Views
{
    public partial class Sequencer : UserControl
    {
        double pixelsPerTick = 0;
        public Sequencer()
        {
            this.DataContext = App.SequencerMdl;
            InitializeComponent();
            App.SequencerMdl.PropertyChanged += SequencerMdl_PropertyChanged;
            App.SequencerMdl.ChordsRebuild += SequencerMdl_ChordsRebuild; ;
        }

        private void SequencerMdl_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.SequencerMdl.PlaybackCursorPos) &&
                App.SequencerMdl.AutoscrollActive) 
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                ScrollIfNeeded());
            }
        }

        void ScrollIfNeeded()
        {
            double windowWidth = BottomScrollViewer.Bounds.Width;
            double totalWidth = BottomScrollViewer.Extent.Width;
            double curOffset = BottomScrollViewer.Offset.X;
            double cursorPos = App.SequencerMdl.PlaybackCursorPos * pixelsPerTick;
            if (cursorPos > curOffset + windowWidth * 0.75)
            {
                Avalonia.Vector o = BottomScrollViewer.Offset;
                BottomScrollViewer.Offset = new Avalonia.Vector(cursorPos - windowWidth * 0.75,
                    o.Y);
            }
            if (cursorPos < curOffset + windowWidth * 0.25)
            {
                Avalonia.Vector o = BottomScrollViewer.Offset;
                BottomScrollViewer.Offset = new Avalonia.Vector(System.Math.Max(cursorPos - windowWidth * 0.25, 0),
                    o.Y);
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            Relayout();
            base.OnSizeChanged(e);
        }

        private void SequencerMdl_ChordsRebuild(object? sender, List<ChordAnalyzer.TimedChord> chords)
        {
            ChordNames.Children.Clear();
            int sixteenthRes = App.SequencerMdl.MidiSong.Resolution / 4;
            pixelsPerTick = (double)SequencerModel.PixelsPerSixteenth / (double)sixteenthRes;
            foreach (var tc in chords)
            {
                //kv.Value.
                var btn = new Button();
                Canvas.SetLeft(btn, tc.startTicks * pixelsPerTick);
                Canvas.SetTop(btn, 0);
                //btn.Width = SequencerModel.PixelsPerSixteenth;
                btn.Padding = new Avalonia.Thickness(0, 0);
                btn.Height = ChordNames.Height;
                btn.Width = tc.lengthTicks * pixelsPerTick;
                btn.Click += ChordBtn_Click;
                btn.Content = tc.chord.ToString();
                btn.Tag = tc;                
                ChordNames.Children.Add(btn);
            }
        }

        private void ChordBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            MidiPlayer player = App.Player;
            ChordAnalyzer.TimedChord tc = (ChordAnalyzer.TimedChord)((sender as Button).Tag);
            var notes = tc.chord.GetNotes(4);
            foreach (var note in notes)
            {
                player.SynthEngine.NoteOn(note, 100);
            }

            Timer t = new Timer((object state) =>
            {
                foreach (int note in notes)
                {
                    player.SynthEngine.NoteOff(note);
                }
            }, null, 1000, Timeout.Infinite);
        }

        void Relayout()
        {
            TimeStep.Children.Clear();

            var midiSong = App.SequencerMdl.MidiSong;
            if (midiSong == null)
                return;
            int sixteenthRes = midiSong.Resolution / 4;
            pixelsPerTick = (double)SequencerModel.PixelsPerSixteenth / (double)sixteenthRes;
            int height = (int)TimeStep.Height;

            for (int i = 0; i < midiSong.LengthSixteenths; i += 4)
            {
                var isDarkTheme = App.Current?.ActualThemeVariant == ThemeVariant.Dark;
                var strokeColor = isDarkTheme ? Avalonia.Media.Brushes.White : Avalonia.Media.Brushes.Black;

                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Avalonia.Point(i * SequencerModel.PixelsPerSixteenth - 1, 0),
                    EndPoint = new Avalonia.Point(i * SequencerModel.PixelsPerSixteenth,
                                                    (i % 16) == 0 ? height : height / 2),
                    Stroke = strokeColor,

                    StrokeThickness = 1
                };
                TimeStep.Children.Add(line);
            }

            for (int i = 0; i < midiSong.LengthSixteenths; i += 16)
            {
                var textBlock = new Avalonia.Controls.TextBlock
                {
                    Text = (i / 16 + 1).ToString(),
                };
                Canvas.SetLeft(textBlock, i * SequencerModel.PixelsPerSixteenth);
                TimeStep.Children.Add(textBlock);
            }

            TimeStep.Width = midiSong.LengthSixteenths * SequencerModel.PixelsPerSixteenth;
        }
        private void Canvas_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
        }

        private void NoteViewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ChannelCtrl cc = (sender as Button).DataContext as ChannelCtrl;
            App.SequencerMdl.SetNoteViewMode(cc.ChannelNum);
            sequencerPiano.DataContext = cc;
            noteViewCtrl.DataContext = cc;
            noteViewLeftCtrl.DataContext = cc;
            App.SequencerMdl.BuildChords(cc);
        }

      
        private void ChannelViewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            App.SequencerMdl.SetNoteViewMode(-1);
        }
        private void ChannelMuteSolo_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            bool isSoloMode = App.SequencerMdl.ChannelCtrls.Any(c => c.IsSolo);
            foreach (var channel in App.SequencerMdl.ChannelCtrls)
            {
                bool channelEnabled = (isSoloMode && channel.IsSolo) ||
                    (!isSoloMode && !channel.IsMute);
                App.Player.SynthEngine.SetChannelEnabled(channel.ChannelNum, channelEnabled);
            }
        }
        private void ChannelMute_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }

        private void ScrollViewer_ScrollChanged(object? sender, 
            Avalonia.Controls.ScrollChangedEventArgs e)
        {
            //App.SequencerMdl.AutoscrollActive = false;
        }
        private void PlayBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }
        private void StopBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }
        private void ToMelody_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }

        private void NoteZoomIn_Horiz_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SequencerModel.PixelsPerSixteenth *= 2;
            Relayout();
            noteViewCtrl.Relayout();
            if (App.SequencerMdl.NoteViewChannel != null)
                App.SequencerMdl.BuildChords(App.SequencerMdl.NoteViewChannel);
        }

        private void NoteZoomOut_Horiz_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SequencerModel.PixelsPerSixteenth = Math.Max(SequencerModel.PixelsPerSixteenth / 2, 1);
            Relayout();
            noteViewCtrl.Relayout();
            if (App.SequencerMdl.NoteViewChannel != null)
                App.SequencerMdl.BuildChords(App.SequencerMdl.NoteViewChannel);
        }
    }

    public class YOffsetBindingConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
           if (values.Count == 2 &&
              values[0] is Avalonia.Vector xOffset &&
              values[1] is double newY)
            {
                // Return a new vector with the existing X and updated Y
                return new Avalonia.Vector(xOffset.X, newY);
            }
            return Avalonia.Vector.Zero; // Default if binding is incorrect
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { value, value }; // Only one binding needed in this case.
        }
    }

}

