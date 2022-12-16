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
        Vector4[] channelColors = new Vector4[16];

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

            Random r = new Random();
            for (int i = 0; i < 16; ++i)
            {
                channelColors[i] = new Vector4(r.NextSingle(), r.NextSingle(), r.NextSingle(), 1);
            }
        }

        private void Player_OnPlaybackComplete(object? sender, bool e)
        {
            currentMidiFile = null;
        }

        NoteVis noteVis;
        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            noteVis = new NoteVis(e.midiFile);
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
            GL.ClearColor(Color4.DarkGray);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            TimeSpan t = player.CurrentSongTime;
            float blockLength = (float)visTimeSpan.TotalMilliseconds;
            if (noteVis != null)
            {
                Matrix4 viewProj = Matrix4.CreatePerspectiveFieldOfView(1.0f, 1.0f, 0.1f, 10.0f);
                glProgram.Use(0);
                List<NoteVis.NoteBlock> noteBlocks = noteVis.GetNoteBlocks(t, visTimeSpan);
                foreach (var nb in noteBlocks)
                {
                    if (nb.Length < 0)
                        continue;
                    float notescale = 1.0f / 127.0f;
                    float x0 = nb.Note / 127.0f;
                    float ys = nb.Length / blockLength;
                    float y0 = (nb.Start + nb.Length * 0.5f) / blockLength;

                    x0 = x0 * 2 - 1;
                    y0 = y0 * 2 - 1;
                    Matrix4 mat = Matrix4.CreateScale(new Vector3(notescale, ys, notescale)) * 
                        Matrix4.CreateTranslation(new Vector3(x0, y0, -2));
                    glProgram.SetMVP(mat, viewProj);
                    glProgram.Set4("meshColor", channelColors[nb.Channel]);
                    glProgram.Set1("ambient", 1.0f);
                    glProgram.Set1("opacity", 1.0f);
                    glCube.Draw();
                }
            }
        }
    }
}
