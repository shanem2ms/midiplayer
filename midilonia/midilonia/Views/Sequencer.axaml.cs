using Avalonia.Controls;
using Avalonia.Styling;
using midilib;
using System.Collections.Generic;
using System.Threading.Channels;

namespace midilonia.Views
{
    public partial class Sequencer : UserControl
    {
        public Sequencer()
        {
            this.DataContext = App.SequencerMdl;
            InitializeComponent();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            Relayout();
            base.OnSizeChanged(e);
        }

        void Relayout()
        {
            TimeStep.Children.Clear();

            var midiSong = App.SequencerMdl.MidiSong;
            if (midiSong == null)
                return;
            int sixteenthRes = midiSong.Resolution / 4;
            double pixelsPerTick = (double)SequencerModel.PixelsPerSixteenth / (double)sixteenthRes;
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

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }

        private void ChannelMuteSolo_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
        }

        private void ScrollViewer_ScrollChanged(object? sender, Avalonia.Controls.ScrollChangedEventArgs e)
        {
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
    }
}
