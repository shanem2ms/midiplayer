using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using OpenTK.Graphics;
using System.Collections.Generic;
using midilib;

namespace midimo.iOS
{
	public class GlView
	{
        GLObjects.Program glProgram;
        GLObjects.VertexArray glCube;
        TimeSpan visTimeSpan = new TimeSpan(0, 0, 5);
        List<Vis> visualizations = new List<Vis>();
        MidiPlayer player;
        bool isInit = false;
        MeltySynth.MidiFile currentMidiFile;

        public GlView(MidiPlayer _player)
		{
            player = _player;
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackComplete += Player_OnPlaybackComplete;
        }

        public void Init()
        {
            glProgram = new GLObjects.Program();
            glCube = GLObjects.Cube.MakeCube(glProgram);

            isInit = true;
        }

        private void Player_OnPlaybackComplete(object sender, bool e)
        {
            currentMidiFile = null;
        }

        private void Player_OnPlaybackStart(object sender, MidiPlayer.PlaybackStartArgs e)
        {
            visualizations.Clear();
            visualizations.Add(new NoteVis(e.midiFile));
            visualizations.Add(new DrumVis(e.midiFile));
            currentMidiFile = e.midiFile;
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
        public void OnRender()
        {
            if (!isInit)
                Init();

            GL.ClearColor(new Color4(16, 16, 16, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            foreach (Vis vis in visualizations)
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

