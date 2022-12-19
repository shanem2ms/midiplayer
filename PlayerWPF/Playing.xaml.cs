using midilib;
using OpenTK.Mathematics;
using System;
using System.Windows.Controls;
using OpenTK.Graphics.ES30;
using OpenTK.Wpf;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static GLObjects.Program;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using static midilib.NoteVis;

namespace PlayerWPF
{
    /// <summary>
    /// Interaction logic for Playing.xaml
    /// </summary>
    public partial class Playing : UserControl
    {
        ChannelOutput[] channelOutputs;
        MidiPlayer player = App.Player;
        GLObjects.Program glProgram;
        GLObjects.VertexArray glCube;
        MeltySynth.MidiFile? currentMidiFile;
        TimeSpan visTimeSpan = new TimeSpan(0, 0, 5);

        public Playing()
        {
            InitializeComponent();
            var settings = new GLWpfControlSettings
            {
            };
            OpenTkControl.Start(settings);
            channelOutputs = new ChannelOutput[] {
                Ch0, Ch1, Ch2, Ch3, Ch4, Ch5, Ch6, Ch7,
            Ch8, Ch9, Ch10, Ch11, Ch12, Ch13, Ch14, Ch15 };
            player.OnChannelEvent += Player_OnChannelEvent;
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackComplete += Player_OnPlaybackComplete;

            glProgram = GLObjects.Program.FromFiles("Main.vert", "Main.frag");
            glCube = GLObjects.Cube.MakeCube(glProgram);

        }

        private void Player_OnPlaybackComplete(object? sender, bool e)
        {
            currentMidiFile = null;
        }

        NoteVis noteVis;
        Vector4[] channelColors = new Vector4[16];
        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            noteVis = new NoteVis(e.midiFile);
            for (int i = 0; i < 16; ++i)
            {
                channelColors[i] = new Vector4(noteVis.ChannelColors[i].R / 255.0f,
                    noteVis.ChannelColors[i].G / 255.0f,
                    noteVis.ChannelColors[i].B / 255.0f,
                    1);
            }
            currentMidiFile = e.midiFile;
            foreach (ChannelOutput c in channelOutputs)
            {
                c.ResetForSong();
            }
        }

        private void Player_OnChannelEvent(object? sender, MidiPlayer.ChannelEvent e)
        {
            if (e.channel < channelOutputs.Length)
            {
                Dispatcher.BeginInvoke(() =>
                    channelOutputs[e.channel].SetMidiData(e));
            }
        }
        private void OpenTkControl_OnRender(TimeSpan delta)
        {
            GL.ClearColor(new Color4(16, 16, 16, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            TimeSpan t = player.CurrentSongTime;
            float blockLength = (float)visTimeSpan.TotalMilliseconds;
            if (noteVis != null)
            {
                Matrix4 viewProj = Matrix4.CreateOrthographicOffCenter(0, 1, 1, 0, 0.1f, 10);
                glProgram.Use(0);
                noteVis.Update(t, visTimeSpan);
                List<NoteVis.NoteBlock> noteBlocks = noteVis.NoteBlocks;

                foreach (var nb in noteBlocks)
                {
                    if (nb.Length < 0)
                        continue;
                    if (nb.Note < 21 || nb.Note > 108)
                        continue;

                    int pianoKeyIdx = nb.Note - 21;
                    NoteVis.PianoKey pianoKey = noteVis.PianoKeys[pianoKeyIdx];
                    float x0 = pianoKey.x;
                    float xs = pianoKey.isBlack ? noteVis.PianoBlackXs : noteVis.PianoWhiteXs * 0.75f;
                    float ys = nb.Length / blockLength;
                    float y0 = 1 - (nb.Start + nb.Length) / blockLength;

                    Matrix4 mat = Matrix4.CreateScale(new Vector3(xs, ys, 0.003f)) *
                        Matrix4.CreateTranslation(new Vector3(x0, y0, -2));
                    glProgram.SetMVP(mat, viewProj);
                    glProgram.Set4("meshColor", channelColors[nb.Channel]);
                    glProgram.Set1("ambient", 1.0f);
                    glProgram.Set1("opacity", 1.0f);
                    glCube.Draw();
                }


                Vector4 pianoWhiteColor = Vector4.One;
                Vector4 pianoBlackColor = new Vector4(0, 0, 0, 1);
                Vector4 pianoPlayingColor = new Vector4(0, 0.5f, 1, 1);
                Vector4 pianoBlackPlayingColor = new Vector4(0.35f, 0.75f, 1, 1);
                foreach (var key in noteVis.PianoKeys)
                {
                    if (key.isBlack) continue;
                    Matrix4 mat = Matrix4.CreateScale(new Vector3(noteVis.PianoWhiteXs, key.ys, 0.003f)) *
                        Matrix4.CreateTranslation(new Vector3(key.x, key.y, -2));
                    glProgram.SetMVP(mat, viewProj);
                    glProgram.Set4("meshColor", key.channelsOn > 0 ? pianoPlayingColor : pianoWhiteColor);
                    glProgram.Set1("ambient", 1.0f);
                    glProgram.Set1("opacity", 1.0f);
                    glCube.Draw();

                }
                foreach (var key in noteVis.PianoKeys)
                {
                    if (!key.isBlack) continue;
                    Matrix4 mat = Matrix4.CreateScale(new Vector3(noteVis.PianoBlackXs, key.ys, 0.003f)) *
                        Matrix4.CreateTranslation(new Vector3(key.x, key.y, -2));
                    glProgram.SetMVP(mat, viewProj);
                    glProgram.Set4("meshColor", key.channelsOn > 0 ? pianoBlackPlayingColor : pianoBlackColor);
                    glProgram.Set1("ambient", 1.0f);
                    glProgram.Set1("opacity", 1.0f);
                    glCube.Draw();

                }                
            }
        }
    }
}
