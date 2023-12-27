using Haukcode.HighResolutionTimer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MeltySynth
{
    /// <summary>
    /// An instance of the MIDI file sequencer.
    /// </summary>
    /// <remarks>
    /// Note that this class does not provide thread safety.
    /// If you want to do playback control and render the waveform in separate threads,
    /// you must ensure that the methods will not be called simultaneously.
    /// </remarks>
    public sealed class MidiOutSequencer
    {
        private float speed;

        private MidiFile? midiFile;
        private TimeSpan currentTime;
        int currentTicks;
        private int msgIndex;
        private int loopIndex;
        private bool justSeeked;
        public bool IsPaused = false;
        public struct PlaybackTimeArgs
        {
            public TimeSpan timeSpan;
            public int ticks;
        }
        public event EventHandler<PlaybackTimeArgs> OnPlaybackTime;
        public event EventHandler<bool> OnPlaybackComplete;
        public event EventHandler<MidiFile> OnPlaybackStart;
        public delegate void OnProcessMidiMessageDel(int channel, int command, int data1, int data2);
        OnProcessMidiMessageDel onProcessMidiMessage;
        Stopwatch sw = new Stopwatch();
        Thread midioutThread;
        bool threadRunning = true;
        long startPlayMs;
        private object mutex = new object();

        public TimeSpan CurrentTime => currentTime;
        public MidiFile? CurrentMidiFile => midiFile;


        /// <summary>
        /// Initializes a new instance of the sequencer.
        /// </summary>
        /// <param name="synthesizer">The synthesizer to be handled by the sequencer.</param>
        public MidiOutSequencer(OnProcessMidiMessageDel del)
        {
            onProcessMidiMessage = del;

            midioutThread = new Thread(MidiOutThread);
            midioutThread.Start();
            speed = 1F;
        }


        List<long> msElapsed = new List<long>();
        long lastTimer = 0;
        int tickCnt = 0;
        void MidiOutThread()
        {
            sw.Start();
            HighResolutionTimer timer = new HighResolutionTimer();
            timer.SetPeriod(1);
            timer.Start();
            while(threadRunning)
            {
                timer.WaitForTrigger();
                lock (mutex)
                {
                    currentTime = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds - startPlayMs);
                    ProcessEvents();
                }
            }
        }

        public void Dispose()
        {
            threadRunning = false;
            midioutThread.Join();
        }
        /// <summary>
        /// Plays the MIDI file.
        /// </summary>
        /// <param name="midiFile">The MIDI file to be played.</param>
        /// <param name="loop">If <c>true</c>, the MIDI file loops after reaching the end.</param>
        public void Play(MidiFile midiFile, bool startPaused)
        {
            if (midiFile == null)
            {
                throw new ArgumentNullException(nameof(midiFile));
            }

            lock (mutex)
            {
                this.midiFile = midiFile;
                IsPaused = startPaused;

                currentTime = TimeSpan.Zero;
                currentTicks = 0;
                msgIndex = 0;
                loopIndex = 0;
                startPlayMs = sw.ElapsedMilliseconds;
            }
            OnPlaybackStart?.Invoke(this, this.midiFile);
        }

        public TimeSpan TicksToTime(int ticks)
        {
            int index = Array.BinarySearch(midiFile.Ticks, ticks);
            if (index < 0)
            {
                index = ~index;
            }
            TimeSpan time = midiFile.Times[index];
            return time;
        }

        public void SeekTo(TimeSpan time)
        {
            if (midiFile == null)
                return;
            int index = Array.BinarySearch(midiFile.Times, time);
            if (index < 0)
            {
                index = ~index;
            }

            currentTicks = midiFile.Messages[index].Ticks;
            currentTime = time;
            msgIndex = index;
            justSeeked = true;
            OnPlaybackTime?.Invoke(this, new PlaybackTimeArgs() { timeSpan = currentTime, ticks = currentTicks });
        }

        /// <summary>
        /// Stop playing.
        /// </summary>
        public void Stop()
        {
            midiFile = null;
        }

        public void Pause(bool pause)
        {
            IsPaused = pause;
        }

        /// <inheritdoc/>

        void ProcessToIndex(int endIdx)
        {
            for (int idx = 0; idx < endIdx; ++idx)
            {
                var msg = midiFile.Messages[idx];

                if (msg.Command == 0xB0 ||
                    msg.Command == 0xC0)
                {
                    onProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                }
            }

        }
        private void ProcessEvents()
        {
            if (midiFile == null)
            {
                return;
            }

            while (msgIndex < midiFile.Messages.Length)
            {
                if (justSeeked)
                {
                    ProcessToIndex(msgIndex);
                    justSeeked = false;
                }

                var time = midiFile.Messages[msgIndex].Time;
                var msg = midiFile.Messages[msgIndex];
                currentTicks = msg.Ticks;
                if (time <= currentTime)
                {
                    if (msg.Type == MidiFile.MessageType.Normal)
                    {
                        onProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                    }
                    msgIndex++;
                }
                else
                {
                    break;
                }
            }

            if (msgIndex == midiFile.Messages.Length)
            {
                OnPlaybackComplete?.Invoke(this, false);
            }

            OnPlaybackTime?.Invoke(this, new PlaybackTimeArgs() { timeSpan = currentTime, ticks = currentTicks });
        }

        /// <summary>
        /// Gets the current playback position.
        /// </summary>
        public TimeSpan Position => currentTime;

        /// <summary>
        /// Gets a value that indicates whether the current playback position is at the end of the sequence.
        /// </summary>
        /// <remarks>
        /// If the <see cref="Play(MidiFile, bool)">Play</see> method has not yet been called, this value is true.
        /// This value will never be <c>true</c> if loop playback is enabled.
        /// </remarks>
        public bool EndOfSequence
        {
            get
            {
                if (midiFile == null)
                {
                    return true;
                }
                else
                {
                    return msgIndex == midiFile.Messages.Length;
                }
            }
        }

        /// <summary>
        /// Gets or sets the playback speed.
        /// </summary>
        /// <remarks>
        /// The default value is 1.
        /// The tempo will be multiplied by this value.
        /// </remarks>
        public float Speed
        {
            get => speed;

            set
            {
                if (value > 0)
                {
                    speed = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("The playback speed must be a positive value.");
                }
            }
        }
    }
}
