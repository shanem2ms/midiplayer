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
        Vector4[] channelColors = new Vector4[16];
        NoteVis noteVis;
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

            Random r = new Random();
            for (int i = 0; i < 16; ++i)
            {
                channelColors[i] = new Vector4((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), 1);
            }

            isInit = true;
        }

        private void Player_OnPlaybackComplete(object sender, bool e)
        {
            currentMidiFile = null;
        }

        private void Player_OnPlaybackStart(object sender, MidiPlayer.PlaybackStartArgs e)
        {
            noteVis = new NoteVis(e.midiFile);
            currentMidiFile = e.midiFile;
        }
      
        public void OnRender()
        {

            if (!isInit)
                Init(); GL.ClearColor(Color4.DarkGray);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            float blockLength = (float)visTimeSpan.TotalMilliseconds;
            if (noteVis != null)
            {
                TimeSpan t = player.CurrentSongTime;
                Matrix4 viewProj = Matrix4.CreatePerspectiveFieldOfView(1.0f, 1.0f, 0.1f, 10.0f);
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
                    float xs = 0.005f;
                    float ys = nb.Length / blockLength;
                    float y0 = (nb.Start + nb.Length * 0.5f) / blockLength;
                    float yOffset = 1 + noteVis.PianoTopY;

                    y0 = y0 * 2 - 1;
                    Matrix4 mat = Matrix4.Scale(new Vector3(xs, ys, 0.003f)) *
                        Matrix4.CreateTranslation(new Vector3(x0, y0 + yOffset, -2));
                    glProgram.SetMVP(mat, viewProj);
                    glProgram.Set4("meshColor", channelColors[nb.Channel]);
                    glProgram.Set1("ambient", 1.0f);
                    glProgram.Set1("opacity", 1.0f);
                    glCube.Draw();
                }


                Vector4 pianoWhiteColor = Vector4.One;
                Vector4 pianoBlackColor = new Vector4(0, 0, 0, 1);
                Vector4 pianoPlaying = new Vector4(0, 0.5f, 1.0f, 1);
                foreach (var key in noteVis.PianoKeys)
                {
                    if (key.isBlack) continue;
                    Matrix4 mat = Matrix4.Scale(new Vector3(key.xs, key.ys, 0.003f)) *
                        Matrix4.CreateTranslation(new Vector3(key.x, key.y, -2));
                    glProgram.SetMVP(mat, viewProj);
                    glProgram.Set4("meshColor", key.channelsOn > 0 ? pianoPlaying : pianoWhiteColor);
                    glProgram.Set1("ambient", 1.0f);
                    glProgram.Set1("opacity", 1.0f);
                    glCube.Draw();

                }
                foreach (var key in noteVis.PianoKeys)
                {
                    if (!key.isBlack) continue;
                    Matrix4 mat = Matrix4.Scale(new Vector3(key.xs, key.ys, 0.003f)) *
                        Matrix4.CreateTranslation(new Vector3(key.x, key.y, -2));
                    glProgram.SetMVP(mat, viewProj);
                    glProgram.Set4("meshColor", key.channelsOn > 0 ? pianoPlaying : pianoBlackColor);
                    glProgram.Set1("ambient", 1.0f);
                    glProgram.Set1("opacity", 1.0f);
                    glCube.Draw();

                }
            }
        }
    }
}

