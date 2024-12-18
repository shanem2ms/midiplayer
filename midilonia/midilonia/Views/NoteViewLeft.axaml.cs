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
            RelayoutPiano();
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
    void RelayoutPiano()
    {
        mainCanvas.Children.Clear();
        double h = mainCanvas.Bounds.Width;
        Piano piano = new Piano((float)notePixels);
        int startKey = GMInstruments.MidiStartIdx;
        int numKeys = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;
        float leftX = piano.PianoKeys[startKey].x;
        float rightX = piano.PianoKeys[startKey + numKeys - 1].x + piano.PianoWhiteXs;

        for (int i = 0; i < numKeys; i++)
        {
            int keynum = i + startKey;
            bool isBlack = piano.PianoKeys[keynum].isBlack;
            TextBlock tb = new TextBlock();
            tb.Width = h;
            //tb.Height = notePixels;
            int midiNoteIdx = GMInstruments.MidiStartIdx + i;
            tb.Text = $"{midiNoteIdx} {piano.PianoKeys[keynum].KeyLetter}";
            tb.Foreground = isBlack ? Brushes.LightBlue : Brushes.DarkBlue;
            tb.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            double top = (numKeys - 1) * notePixels - (piano.PianoKeys[keynum].x - leftX);
            Canvas.SetLeft(tb, 0);            
            Canvas.SetTop(tb, top);

            Rectangle r = new Rectangle();
            r.Height = notePixels;
            r.Width = isBlack ? h / 2 : h;
            r.Stroke = Brushes.DarkBlue;
            r.StrokeThickness = 2;
            isBlack = isBlack;
            r.Fill = isBlack ? Brushes.Black : Brushes.White;            
            Canvas.SetTop(r, top);
            Canvas.SetLeft(r, 0);
            r.Tag = midiNoteIdx;
            mainCanvas.Children.Add(r);
            mainCanvas.Children.Add(tb);
        }
    }
}