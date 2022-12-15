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

        SongVisData songData;
        private void Player_OnPlaybackStart(object? sender, MidiPlayer.PlaybackStartArgs e)
        {
            songData = new SongVisData(e.midiFile);
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

        class SongVisData
        {
            MeltySynth.MidiFile midiFile;
            public SongVisData(MeltySynth.MidiFile _midiFile)
            {
                midiFile = _midiFile;
            }

            public class ActiveNote
            {
                public List<Tuple<TimeSpan, bool>> times = new List<Tuple<TimeSpan, bool>>();

                public void AddTime(TimeSpan ts, bool onoff)
                {
                    times.Add(new Tuple<TimeSpan, bool>(ts, onoff));
                }
            }

            Dictionary<uint, ActiveNote> activeNotes = new Dictionary<uint, ActiveNote>();
            public Dictionary<uint, ActiveNote> ActiveNotes => activeNotes;
            uint ToIndex(byte channel, byte note)
            {
                return (uint)(channel << 16) | note;
            }


            void AddNoteData(byte channel, byte note, bool on, TimeSpan time)
            {
                uint idx = ToIndex(channel, note);
                ActiveNote an;
                if (!activeNotes.TryGetValue(idx, out an))
                {
                    an = new ActiveNote();
                    activeNotes.Add(idx, an);
                }
                an.AddTime(time, on);
            }

            void GetNotes(TimeSpan now, TimeSpan delta)
            {
                activeNotes = new Dictionary<uint, ActiveNote>();
                int startIndex = Array.BinarySearch(midiFile.Times, now);
                if (startIndex < 0)
                {
                    startIndex = ~startIndex;
                }
                int endIndex = Array.BinarySearch(midiFile.Times, now + delta);
                if (endIndex < 0)
                {
                    endIndex = ~endIndex;
                }

                for (int idx = startIndex; idx < endIndex; ++idx)
                {
                    MeltySynth.MidiFile.Message msg = midiFile.Messages[idx];
                    if (msg.Channel >= 16)
                        continue;
                    if (msg.Command == MidiSpec.NoteOff || msg.Command == MidiSpec.NoteOn)
                    {
                        if (msg.Command == MidiSpec.NoteOff ||
                            msg.Data2 == 0)
                        {
                            AddNoteData(msg.Channel, msg.Data1, false, msg.Time);
                        }
                        else
                        {
                            AddNoteData(msg.Channel, msg.Data1, true, msg.Time);
                        }

                    }
                }
            }

            public class NoteBlock
            {
                public byte Channel;
                public byte Note;
                public float Start;
                public float Length;
            }
            public List<NoteBlock> GetNoteBlocks(TimeSpan start, TimeSpan length)
            {
                GetNotes(start, length);
                List<NoteBlock> noteBlocks = new List<NoteBlock>();
                float endTime = (float)(start + length).TotalMinutes;
                float startTime = (float)start.TotalMilliseconds;
                foreach (var kv in ActiveNotes)
                {
                    byte channel = (byte)(kv.Key >> 16);
                    byte note = (byte)(kv.Key & 0xff);
                    var list = kv.Value;
                    bool ison = !list.times.First().Item2;
                    float onTime = startTime;
                    foreach (var notetime in list.times)
                    {
                        if (notetime.Item2 == ison)
                            continue;
                        if (!notetime.Item2)
                        {
                            float noteEndTime = (float)notetime.Item1.TotalMilliseconds;
                            NoteBlock nb = new NoteBlock()
                            {
                                Channel = channel,
                                Note = note,
                                Start = onTime - startTime,
                                Length = (noteEndTime - onTime)
                            };
                            noteBlocks.Add(nb);
                        }
                        else
                            onTime = (float)notetime.Item1.TotalMilliseconds;
                        ison = notetime.Item2;
                    }
                    if (ison)
                    {
                        NoteBlock nb = new NoteBlock()
                        {
                            Channel = channel,
                            Note = note,
                            Start = onTime - startTime,
                            Length = (endTime - onTime)
                        };
                        noteBlocks.Add(nb);
                    }
                }
                return noteBlocks;
            }
        }

        private void OpenTkControl_OnRender(TimeSpan delta)
        {
            GL.ClearColor(Color4.DarkGray);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            TimeSpan t = player.CurrentSongTime;
            float blockLength = (float)visTimeSpan.TotalMilliseconds;
            if (songData != null)
            {
                Matrix4 viewProj = Matrix4.CreatePerspectiveFieldOfView(1.0f, 1.0f, 0.1f, 10.0f);
                glProgram.Use(0);
                List<SongVisData.NoteBlock> noteBlocks = songData.GetNoteBlocks(t, visTimeSpan);
                foreach (var nb in noteBlocks)
                {
                    if (nb.Length < 0)
                        continue;
                    float notescale = 1.0f / 127.0f;
                    float x0 = nb.Note / 127.0f;
                    float ys = nb.Length / blockLength;
                    float y0 = (nb.Start + nb.Length * 0.5f) / blockLength;

                    x0 = x0 * 2 - 1;
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
