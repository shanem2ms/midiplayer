using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using midilib;
using System;

namespace midilonia.Views;

public partial class NoteView : UserControl
{
    public NoteView()
    {
        InitializeComponent();        
    }

    ChannelCtrl channelCtrl;
    Line playbackCursor = null;
    double pixelsPerTick = 0;

    public static readonly StyledProperty<double> NotePixelsProp =
    AvaloniaProperty.Register<NoteView, double>(nameof(NotePixels));

    double notePixels = 20;
    public double NotePixels { get => notePixels;
        set {
            int noteCount = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;
            notePixels = value;
            this.Height = noteCount * NotePixels;
        } }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (channelCtrl != null)
            channelCtrl.OnPlaybackCursorChanged -= ChannelCtrl_OnPlaybackCursorChanged;
        if (DataContext is ChannelCtrl)
        {
            channelCtrl = DataContext as ChannelCtrl;
            channelCtrl.OnPlaybackCursorChanged += ChannelCtrl_OnPlaybackCursorChanged;
            Relayout();
        }
        else
            channelCtrl = null;
        base.OnDataContextChanged(e);
    }

    private void ChannelCtrl_OnPlaybackCursorChanged(object? sender, int ticks)
    {
        Dispatcher.UIThread.InvokeAsync(() => UpdateCursor(ticks));
    }

    void UpdateCursor(int ticks)
    {
        if (playbackCursor == null)
        {
            playbackCursor = new Line
            {
                StrokeThickness = 5,
                Stroke = Brushes.Red // Avalonia.Media.Brushes
            };
            mainCanvas.Children.Add(playbackCursor);
        }

        playbackCursor.StartPoint = new Avalonia.Point(ticks * pixelsPerTick, 0);
        playbackCursor.EndPoint = new Avalonia.Point(ticks * pixelsPerTick, mainCanvas.Bounds.Height);
    }

    int gmNoteRange = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (channelCtrl != null)
        {
            Relayout();
        }
    }
    void Relayout()
    {
        int noteCount = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;

        var themeVariant = Application.Current.ActualThemeVariant;
        Brush noteLinesBrush;
        if (themeVariant == ThemeVariant.Dark)
        {
            noteLinesBrush = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));
        }
        else
        {
            noteLinesBrush = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0));
        }

        mainCanvas.Children.Clear();
        int midiFileRes = channelCtrl.Resolution;

        int sixteenthRes = midiFileRes / 4;
        pixelsPerTick = (double)SequencerModel.PixelsPerSixteenth / (double)sixteenthRes;
        long lengthSixteenths = channelCtrl.LengthSixteenths;
        int channelHeight = (int)mainCanvas.Bounds.Height;
        if (channelHeight <= 0)
            return;

        double YnoteSizePixels = (1.0) / (double)gmNoteRange * channelHeight;
        double YborderLineSize = (0.1) / (double)gmNoteRange * channelHeight;

        mainCanvas.Width = lengthSixteenths * SequencerModel.PixelsPerSixteenth;

        int[] noteOnTick = new int[127];
        for (int j = 0; j < noteOnTick.Length; j++)
            noteOnTick[j] = -1;

        

        for (int y = 0; y < noteCount; ++y)
        {
            double Y = y * notePixels;
            var line = new Line
            {
                StartPoint = new Avalonia.Point(0, Y),
                EndPoint = new Avalonia.Point(lengthSixteenths * SequencerModel.PixelsPerSixteenth, Y),
                StrokeThickness = YborderLineSize,
                Stroke = noteLinesBrush// Avalonia.Media.Brushes
            };
            mainCanvas.Children.Add(line);
        }
        foreach (var note in channelCtrl.Notes)
        {
            int startTicks = note.startTicks;
            int endTicks = note.startTicks + note.lengthTicks;

            if (note.note >= GMInstruments.MidiStartIdx &&
                note.note < GMInstruments.MidiEndIdx)
            {
                double Y = (GMInstruments.MidiEndIdx - note.note - 1) * notePixels +
                    notePixels * 0.5;

                var line = new Line
                {
                    StartPoint = new Avalonia.Point(startTicks * pixelsPerTick, Y),
                    EndPoint = new Avalonia.Point(endTicks * pixelsPerTick, Y),
                    StrokeThickness = YnoteSizePixels,
                    Stroke = Brushes.Blue // Avalonia.Media.Brushes
                };
                mainCanvas.Children.Add(line);
            }
            else if (note.note == MidiSong.PedalNote)
            {
                double Y = 0;

                var line = new Line
                {
                    StartPoint = new Avalonia.Point(startTicks * pixelsPerTick, Y),
                    EndPoint = new Avalonia.Point(endTicks * pixelsPerTick, Y + 5),
                    StrokeThickness = YnoteSizePixels,
                    Stroke = Brushes.Green
                };
                mainCanvas.Children.Add(line);
            }
        }

        playbackCursor = null;
    }
}