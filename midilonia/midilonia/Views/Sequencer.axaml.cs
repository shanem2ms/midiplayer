using Avalonia.Controls;

namespace midilonia.Views
{
    public partial class Sequencer : UserControl
    {
        public Sequencer()
        {
            this.DataContext = App.SequencerMdl;
            InitializeComponent();
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
