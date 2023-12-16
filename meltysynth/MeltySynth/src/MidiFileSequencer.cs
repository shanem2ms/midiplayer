using System;

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
    public sealed class MidiFileSequencer : IAudioRenderer
    {
        private readonly Synthesizer synthesizer;

        private float speed;

        private MidiFile? midiFile;
        private bool loop;

        private int blockWrote;

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
        public OnProcessMidiMessageDel OnProcessMidiMessage;

        public TimeSpan CurrentTime => currentTime;
        public MidiFile? CurrentMidiFile => midiFile;


        /// <summary>
        /// Initializes a new instance of the sequencer.
        /// </summary>
        /// <param name="synthesizer">The synthesizer to be handled by the sequencer.</param>
        public MidiFileSequencer(Synthesizer synthesizer)
        {
            if (synthesizer == null)
            {
                throw new ArgumentNullException(nameof(synthesizer));
            }

            this.synthesizer = synthesizer;

            speed = 1F;
        }

       
        /// <summary>
        /// Plays the MIDI file.
        /// </summary>
        /// <param name="midiFile">The MIDI file to be played.</param>
        /// <param name="loop">If <c>true</c>, the MIDI file loops after reaching the end.</param>
        public void Play(MidiFile midiFile, bool loop)
        {
            if (midiFile == null)
            {
                throw new ArgumentNullException(nameof(midiFile));
            }

            this.midiFile = midiFile;
            this.loop = loop;

            blockWrote = synthesizer.BlockSize;

            currentTime = TimeSpan.Zero;
            currentTicks = 0;
            msgIndex = 0;
            loopIndex = 0;

            OnPlaybackStart?.Invoke(this, this.midiFile);
            synthesizer.Reset();
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

            currentTime = time;
            msgIndex = index;
            justSeeked = true;
        }
     
        /// <summary>
        /// Stop playing.
        /// </summary>
        public void Stop()
        {
            midiFile = null;
            synthesizer.Reset();
        }

        public void Pause(bool pause)
        {
            IsPaused = pause;
        }

        /// <inheritdoc/>
        public void Render(Span<float> left, Span<float> right)
        {
            if (left.Length != right.Length)
            {
                throw new ArgumentException("The output buffers for the left and right must be the same length.");
            }

            if (IsPaused)
            {
                left.Fill(0);
                right.Fill(0);
                return;
            }

            var wrote = 0;
            while (wrote < left.Length)
            {
                if (blockWrote == synthesizer.BlockSize)
                {
                    ProcessEvents();
                    blockWrote = 0;
                    currentTime += MidiFile.GetTimeSpanFromSeconds((double)speed * synthesizer.BlockSize / synthesizer.SampleRate);
                }

                var srcRem = synthesizer.BlockSize - blockWrote;
                var dstRem = left.Length - wrote;
                var rem = Math.Min(srcRem, dstRem);

                synthesizer.Render(left.Slice(wrote, rem), right.Slice(wrote, rem));

                blockWrote += rem;
                wrote += rem;
            }
        }

        void ProcessToIndex(int endIdx)
        {
            for (int idx = 0; idx < endIdx; ++idx)
            {
                var msg = midiFile.Messages[idx];

                if (msg.Command == 0xB0 ||
                    msg.Command == 0xC0)
                {
                    synthesizer.ProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
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
                    synthesizer.NoteOffAll(false);
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
                        if (OnProcessMidiMessage != null)
                            OnProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                        synthesizer.ProcessMidiMessage(msg.Channel, msg.Command, msg.Data1, msg.Data2);
                    }
                    else if (loop)
                    {
                        if (msg.Type == MidiFile.MessageType.LoopStart)
                        {
                            loopIndex = msgIndex;
                        }
                        else if (msg.Type == MidiFile.MessageType.LoopEnd)
                        {
                            currentTime = midiFile.Messages[loopIndex].Time;
                            currentTicks = midiFile.Messages[loopIndex].Ticks;
                            msgIndex = loopIndex;
                            synthesizer.NoteOffAll(false);
                        }                        
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
                synthesizer.NoteOffAll(false);
                if (loop)
                {
                    currentTime = midiFile.Messages[loopIndex].Time;
                    currentTicks = midiFile.Messages[loopIndex].Ticks;
                    msgIndex = loopIndex;
                }
                else
                {
                    OnPlaybackComplete?.Invoke(this, loop);
                }
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
