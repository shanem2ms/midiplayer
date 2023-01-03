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
using OpenTK.Windowing.Common.Input;

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

        List<Vis> visList = new List<Vis>();
        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            visList.Add(new NoteVis(e.midiFile));
            visList.Add(new DrumVis(e.midiFile));
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

        Vector4 ConvOpenTk(System.Numerics.Vector4 vec4)
        {
            return new Vector4(vec4.X, vec4.Y, vec4.Z, vec4.W);
        }
        Matrix4 ConvOpenTk(System.Numerics.Matrix4x4 mat)
        {
            return new Matrix4(mat.M11, mat.M12, mat.M13, mat.M14,
                mat.M21, mat.M22, mat.M23, mat.M24,
                mat.M31, mat.M32, mat.M33, mat.M34,
                mat.M41, mat.M42, mat.M43, mat.M44);
        }
        private void OpenTkControl_OnRender(TimeSpan delta)
        {
            GL.ClearColor(new Color4(16, 16, 16, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (var vis in visList)
            {
                Matrix4 viewProj = Matrix4.CreateOrthographicOffCenter(0, 1, 1, 0, 0.1f, 10);
                List<NoteVis.Cube> cubes = vis.DoVis(visTimeSpan, player);
                glProgram.Use(0);
                foreach (var cube in cubes)
                {
                    Matrix4 m = new Matrix4();
                    glProgram.SetMVP(ConvOpenTk(cube.mat), viewProj);
                    glProgram.Set4("meshColor", ConvOpenTk(cube.color));
                    glProgram.Set1("ambient", 1.0f);
                    glProgram.Set1("opacity", 1.0f);
                    glCube.Draw();

                }
            }            
        }
    }
}
