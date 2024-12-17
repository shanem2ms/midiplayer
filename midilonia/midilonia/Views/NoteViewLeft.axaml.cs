using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using midilib;
using System;
using System.Collections.Generic;

namespace midilonia.Views;

public partial class NoteViewLeft : UserControl
{
    public static readonly StyledProperty<double> NotePixelsProp =
    AvaloniaProperty.Register<NoteView, double>(nameof(NotePixels));

    double notePixels = 20;
    public double NotePixels
    {
        get => notePixels;
        set
        {
            int noteCount = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;
            notePixels = value;
            this.Height = noteCount * NotePixels;
        }
    }

    public NoteViewLeft()
    {
        InitializeComponent();
    }

    ChannelCtrl channelCtrl;
    Line playbackCursor = null;

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
    }

    int gmNoteRange = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (channelCtrl != null)
        {
            RelayoutPiano();
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
        int channelHeight = (int)mainCanvas.Bounds.Height;
        if (channelHeight <= 0)
            return;
        double width = mainCanvas.Bounds.Width;

        double YnoteSizePixels = (1.0) / (double)gmNoteRange * channelHeight;
        double YborderLineSize = (0.1) / (double)gmNoteRange * channelHeight;

        int[] noteOnTick = new int[127];
        for (int j = 0; j < noteOnTick.Length; j++)
            noteOnTick[j] = -1;

        for (int y = 0; y < noteCount; ++y)
        {
            double Y = (y + 0.5) / (double)gmNoteRange * channelHeight;
            var line = new Line
            {
                StartPoint = new Avalonia.Point(0, Y),
                EndPoint = new Avalonia.Point(width, Y),
                StrokeThickness = YborderLineSize,
                Stroke = noteLinesBrush// Avalonia.Media.Brushes
            };
            mainCanvas.Children.Add(line);
        }        


        playbackCursor = null;
    }

    void RelayoutPiano()
    {
        mainCanvas.Children.Clear();
        double h = mainCanvas.Bounds.Width;
        Piano piano = new Piano();
        int startKey = 36;
        int numKeys = 48;
        startKey = System.Math.Min(System.Math.Max(0, startKey), 128 - numKeys);
        float leftX = piano.PianoKeys[startKey].x;
        float rightX = piano.PianoKeys[startKey + numKeys - 1].x + piano.PianoWhiteXs;
        float xScale = 1.0f / (rightX - leftX);

        for (int i = 0; i < numKeys; i++)
        {
            int keynum = i + startKey;
            bool isBlack = piano.PianoKeys[keynum].isBlack;
            Rectangle r = new Rectangle();
            r.Height = notePixels;
            r.Width = isBlack ? h / 2 : h;
            r.Stroke = Brushes.DarkBlue;
            r.StrokeThickness = 2;
            isBlack = isBlack;
            r.Fill = isBlack ? Brushes.Black : Brushes.White;
            Canvas.SetTop(r, (piano.PianoKeys[keynum].x - leftX) * notePixels);
            Canvas.SetLeft(r, 0);
            r.Tag = i + GMInstruments.MidiStartIdx;
            mainCanvas.Children.Add(r);
        }
    }
}