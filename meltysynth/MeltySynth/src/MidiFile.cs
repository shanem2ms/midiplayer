﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MeltySynth
{
    /// <summary>
    /// Represents a standard MIDI file.
    /// </summary>
    public sealed class MidiFile
    {
        private Message[] messages;
        //private TimeSpan[] times;
        private Meta[] metas;


        /// <summary>
        /// Loads a MIDI file from the stream.
        /// </summary>
        /// <param name="stream">The data stream used to load the MIDI file.</param>
        public MidiFile(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Load(stream, 0, MidiFileLoopType.None);

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
        }


        public MidiFile(Message[] _msgs, int resolution)
        {
            Resolution = resolution;
            messages = _msgs;
            AddTimings(messages, resolution);
        }
        /// <summary>
        /// Loads a MIDI file from the stream.
        /// </summary>
        /// <param name="stream">The data stream used to load the MIDI file.</param>
        /// <param name="loopPoint">The loop start point in ticks.</param>
        public MidiFile(Stream stream, int loopPoint)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (loopPoint < 0)
            {
                throw new ArgumentException("The loop point must be a non-negative value.", nameof(loopPoint));
            }

            Load(stream, loopPoint, MidiFileLoopType.None);

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
        }

        /// <summary>
        /// Loads a MIDI file from the stream.
        /// </summary>
        /// <param name="stream">The data stream used to load the MIDI file.</param>
        /// <param name="loopType">The type of the loop extension to be used.</param>
        public MidiFile(Stream stream, MidiFileLoopType loopType)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Load(stream, 0, loopType);

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
        }

        /// <summary>
        /// Loads a MIDI file from the file.
        /// </summary>
        /// <param name="path">The MIDI file name and path.</param>
        public MidiFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, 0, MidiFileLoopType.None);
            }

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
        }

        /// <summary>
        /// Loads a MIDI file from the file.
        /// </summary>
        /// <param name="path">The MIDI file name and path.</param>
        /// <param name="loopPoint">The loop start point in ticks.</param>
        public MidiFile(string path, int loopPoint)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (loopPoint < 0)
            {
                throw new ArgumentException("The loop point must be a non-negative value.", nameof(loopPoint));
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, loopPoint, MidiFileLoopType.None);
            }

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
        }

        /// <summary>
        /// Loads a MIDI file from the file.
        /// </summary>
        /// <param name="path">The MIDI file name and path.</param>
        /// <param name="loopType">The type of the loop extension to be used.</param>
        public MidiFile(string path, MidiFileLoopType loopType)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(stream, 0, loopType);
            }

            // Workaround for nullable warnings in .NET Standard 2.1.
            Debug.Assert(messages != null);
        }

        // Some .NET implementations round TimeSpan to the nearest millisecond,
        // and the timing of MIDI messages will be wrong.
        // This method makes TimeSpan without rounding.
        internal static TimeSpan GetTimeSpanFromSeconds(double value)
        {
            return new TimeSpan((long)(TimeSpan.TicksPerSecond * value));
        }

        private void Load(Stream stream, int loopPoint, MidiFileLoopType loopType)
        {
            using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var chunkType = reader.ReadFourCC();
                if (chunkType != "MThd")
                {
                    throw new InvalidDataException($"The chunk type must be 'MThd', but was '{chunkType}'.");
                }

                var size = reader.ReadInt32BigEndian();
                if (size != 6)
                {
                    throw new InvalidDataException($"The MThd chunk has invalid data.");
                }

                var format = reader.ReadInt16BigEndian();
                if (!(format == 0 || format == 1))
                {
                    throw new NotSupportedException($"The format {format} is not supported.");
                }

                var trackCount = reader.ReadInt16BigEndian();
                var resolution = reader.ReadInt16BigEndian();
                Resolution = resolution;

                var messageLists = new List<Message>[trackCount];
                var tickLists = new List<int>[trackCount];
                var metasList = new List<Meta>[trackCount];
                for (var i = 0; i < trackCount; i++)
                {
                    (messageLists[i], tickLists[i], metasList[i]) = ReadTrack(reader, loopType, i);
                }

                if (loopPoint != 0)
                {
                    var tickList = tickLists[0];
                    var messageList = messageLists[0];
                    if (loopPoint <= tickList.Last())
                    {
                        for (var i = 0; i < tickList.Count; i++)
                        {
                            if (tickList[i] >= loopPoint)
                            {
                                tickList.Insert(i, loopPoint);
                                messageList.Insert(i, Message.LoopStart());
                                break;
                            }
                        }
                    }
                    else
                    {
                        tickList.Add(loopPoint);
                        messageList.Add(Message.LoopStart());
                    }
                }

                double tempo;
                messages =
                    MergeTracks(messageLists, tickLists, resolution, out tempo);
                metas = metasList.SelectMany(x => x).ToArray();
            }
        }

        private void Save(Stream stream)
        {
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                // Write the MIDI header chunk
                writer.WriteFourCC("MThd"); // Chunk type: "MThd"
                writer.WriteInt32BigEndian(6); // Chunk size: 6 bytes
                writer.WriteInt16BigEndian(0); // Format type: 0 (single track)
                writer.WriteInt16BigEndian(1); // Number of tracks: 1
                writer.WriteInt16BigEndian((short)Resolution); // Resolution (ticks per quarter note)

                // Prepare to write the track chunk
                writer.WriteFourCC("MTrk"); // Chunk type: "MTrk"

                // Use a MemoryStream to calculate the track chunk size
                using (var trackStream = new MemoryStream())
                {
                    using (var trackWriter = new BinaryWriter(trackStream, Encoding.ASCII, true))
                    {
                        int previousTick = 0;

                        // Ensure messages are sorted by their tick values
                        var sortedMessages = messages.OrderBy(m => m.Ticks);

                        foreach (var message in sortedMessages)
                        {
                            int deltaTime = message.Ticks - previousTick;
                            previousTick = message.Ticks;

                            // Write the delta time as a variable-length quantity
                            WriteVariableLengthQuantity(trackWriter, deltaTime);

                            // Write the MIDI event
                            trackWriter.Write(message.Data1);

                            // Write the event data (assuming 'Data' is a byte array)
                            trackWriter.Write(message.Data2);
                        }

                        // Write the End of Track meta event
                        trackWriter.Write((byte)0x00); // Delta time
                        trackWriter.Write((byte)0xFF); // Meta event type
                        trackWriter.Write((byte)0x2F); // Meta event subtype: End of Track
                        trackWriter.Write((byte)0x00); // Length: 0

                        // Get the track data and its length
                        var trackData = trackStream.ToArray();
                        writer.WriteInt32BigEndian(trackData.Length); // Track chunk size

                        // Write the track data to the main stream
                        writer.Write(trackData);
                    }
                }
            }
        }

        // Helper method to write variable-length quantities
        private void WriteVariableLengthQuantity(BinaryWriter writer, int value)
        {
            uint buffer = (uint)value;
            byte[] bytes = new byte[4];
            int index = 0;

            bytes[index++] = (byte)(buffer & 0x7F);
            while ((buffer >>= 7) > 0)
            {
                bytes[index++] = (byte)((buffer & 0x7F) | 0x80);
            }

            // Write bytes in reverse order
            for (int i = index - 1; i >= 0; i--)
            {
                writer.Write(bytes[i]);
            }
        }


        class Note
        {
            public int noteOn = -1;
            public byte volume;
        }

        static Message[] NormalizeVolume(Message[] messages)
        {
            byte maxvol = 0;
            foreach (var msg in messages)
            {
                if ((msg.Command & 0xF0) == 0x90)
                {
                    maxvol = Math.Max(msg.Data2, maxvol);
                }
            }
            List<Message> outMessage = new List<Message>();
            int mul = 127 * 0xFFFF / maxvol;
            foreach (var msg in messages)
            {
                Message newmsg = new Message();
                newmsg = msg;
                if ((msg.Command & 0xF0) == 0x90)
                {
                    int nv = (newmsg.Data2 * mul) / 0xFFFF;
                    newmsg.Data2 = (byte)nv;
                }
                outMessage.Add(newmsg);
            }
            return outMessage.ToArray();
        }

        private static (List<Message>, List<int>, List<Meta>) ReadTrack(BinaryReader reader, MidiFileLoopType loopType, int trackIdx)
        {
            var chunkType = reader.ReadFourCC();
            if (chunkType != "MTrk")
            {
                throw new InvalidDataException($"The chunk type must be 'MTrk', but was '{chunkType}'.");
            }

            reader.ReadInt32BigEndian();

            var messages = new List<Message>();
            var ticks = new List<int>();
            var metas = new List<Meta>();

            int tick = 0;
            byte lastStatus = 0;

            while (true)
            {
                var delta = reader.ReadIntVariableLength();
                var first = reader.ReadByte();

                try
                {
                    tick = checked(tick + delta);
                }
                catch (OverflowException)
                {
                    throw new NotSupportedException("Long MIDI file is not supported.");
                }

                if ((first & 128) == 0)
                {
                    var command = lastStatus & 0xF0;
                    if (command == 0xC0 || command == 0xD0)
                    {
                        messages.Add(Message.Common(lastStatus, first));
                        ticks.Add(tick);
                    }
                    else
                    {
                        var data2 = reader.ReadByte();
                        messages.Add(Message.Common(lastStatus, first, data2, loopType));
                        ticks.Add(tick);
                    }

                    continue;
                }

                switch (first)
                {
                    case 0xF0: // System Exclusive
                        AddMeta(metas, reader, 0, trackIdx);
                        break;

                    case 0xF7: // System Exclusive
                        AddMeta(metas, reader, 0, trackIdx);
                        break;

                    case 0xFF: // Meta Event
                        {
                            byte metaEvent = reader.ReadByte();
                            switch (metaEvent)
                            {
                                case 0x2F: // End of Track
                                    reader.ReadByte();
                                    messages.Add(Message.EndOfTrack());
                                    ticks.Add(tick);
                                    return (messages, ticks, metas);

                                case 0x51: // Tempo
                                    messages.Add(Message.TempoChange(ReadTempo(reader)));
                                    ticks.Add(tick);
                                    break;

                                default:
                                    AddMeta(metas, reader, metaEvent, trackIdx);
                                    break;
                            }
                        }
                        break;

                    default:
                        var command = first & 0xF0;
                        if (command == 0xC0 || command == 0xD0)
                        {
                            var data1 = reader.ReadByte();
                            messages.Add(Message.Common(first, data1));
                            ticks.Add(tick);
                        }
                        else
                        {
                            var data1 = reader.ReadByte();
                            var data2 = reader.ReadByte();
                            messages.Add(Message.Common(first, data1, data2, loopType));
                            ticks.Add(tick);
                        }
                        break;
                }

                lastStatus = first;
            }
        }

        private void AddTimings(Message[]messages, int resolution)
        {
            var prevTick = 0;
            var currentTime = TimeSpan.Zero;
            var tempo = 120.0;
            for (int idx = 0; idx < messages.Length; idx++)
            {
                var message = messages[idx];
                var currentTick = message.Ticks;
                var deltaTick = currentTick - prevTick;
                var deltaTime = GetTimeSpanFromSeconds(60.0 / (resolution * tempo) * deltaTick);

                currentTime += deltaTime;
                prevTick = currentTick;

                if (message.Type == MessageType.TempoChange)
                {
                    tempo = message.Tempo;
                    message.Time = currentTime;
                }
                else
                {
                    message.Time = currentTime;
                }
            }
        }

        private static Message[] MergeTracks(List<Message>[] messageLists, List<int>[] tickLists, int resolution, out double outTempo)
        {
            var mergedMessages = new List<Message>();

            var indices = new int[messageLists.Length];

            var currentTick = 0;
            var currentTime = TimeSpan.Zero;

            var tempo = 120.0;

            while (true)
            {
                var minTick = int.MaxValue;
                var minIndex = -1;
                for (var ch = 0; ch < tickLists.Length; ch++)
                {
                    if (indices[ch] < tickLists[ch].Count)
                    {
                        var tick = tickLists[ch][indices[ch]];
                        if (tick < minTick)
                        {
                            minTick = tick;
                            minIndex = ch;
                        }
                    }
                }

                if (minIndex == -1)
                {
                    break;
                }

                var nextTick = tickLists[minIndex][indices[minIndex]];
                var deltaTick = nextTick - currentTick;
                var deltaTime = GetTimeSpanFromSeconds(60.0 / (resolution * tempo) * deltaTick);

                currentTick += deltaTick;
                currentTime += deltaTime;

                var message = messageLists[minIndex][indices[minIndex]];
                if (message.Type == MessageType.TempoChange)
                {
                    tempo = message.Tempo;
                    message.Time = currentTime;
                    message.Ticks = currentTick;
                    mergedMessages.Add(message);
                }
                else
                {
                    message.Time = currentTime;
                    message.Ticks = currentTick;
                    mergedMessages.Add(message);
                }

                indices[minIndex]++;
            }

            outTempo = tempo;
            return mergedMessages.ToArray();
        }

        private static int ReadTempo(BinaryReader reader)
        {
            var size = reader.ReadIntVariableLength();
            if (size != 3)
            {
                throw new InvalidDataException("Failed to read the tempo value.");
            }

            var b1 = reader.ReadByte();
            var b2 = reader.ReadByte();
            var b3 = reader.ReadByte();
            return (b1 << 16) | (b2 << 8) | b3;
        }

        private static void AddMeta(List<Meta> metas, BinaryReader reader, byte metaEvent, int trackIdx)
        {
            var size = reader.ReadIntVariableLength();
            Meta meta = new Meta() { metaType = metaEvent, data = reader.ReadBytes(size), trackIdx = trackIdx };
            metas.Add(meta);
        }

        /// <summary>
        /// The length of the MIDI file.
        /// </summary>
        public TimeSpan Length => messages.Length > 0 ? messages.Last().Time : TimeSpan.Zero;

        public Message[] Messages => messages;
        public TimeSpan[] Times => messages.Select(m => m.Time).ToArray();
        public int[] Ticks => messages.Select(m => m.Ticks).ToArray();
        public Meta[] Metas => metas;

        public int Resolution;


        public struct Meta
        {
            public byte metaType;
            public byte[] data;
            public int trackIdx;

            public string GetStringData()
            {
                return ReadFixedLengthString();
            }

            public string ReadFixedLengthString()
            {
                int actualLength;
                for (actualLength = 0; actualLength < data.Length; actualLength++)
                {
                    if (data[actualLength] == 0)
                    {
                        break;
                    }
                }

                return Encoding.ASCII.GetString(data, 0, actualLength);
            }
        }

        public struct Message
        {
            private byte channel;
            private byte command;
            private byte data1;
            private byte data2;
            private TimeSpan time;
            private int ticks;
            private Message(byte channel, byte command, byte data1, byte data2)
            {
                this.channel = channel;
                this.command = command;
                this.data1 = data1;
                this.data2 = data2;
                this.time = TimeSpan.Zero;
                this.ticks = 0;
            }

            public static Message Common(byte status, byte data1)
            {
                byte channel = (byte)(status & 0x0F);
                byte command = (byte)(status & 0xF0);
                byte data2 = 0;
                return new Message(channel, command, data1, data2);
            }

            public static Message Common(byte status, byte data1, byte data2, MidiFileLoopType loopType)
            {
                byte channel = (byte)(status & 0x0F);
                byte command = (byte)(status & 0xF0);

                if (command == 0xB0)
                {
                    switch (loopType)
                    {
                        case MidiFileLoopType.RpgMaker:
                            if (data1 == 111)
                            {
                                return LoopStart();
                            }
                            break;

                        case MidiFileLoopType.IncredibleMachine:
                            if (data1 == 110)
                            {
                                return LoopStart();
                            }
                            if (data1 == 111)
                            {
                                return LoopEnd();
                            }
                            break;

                        case MidiFileLoopType.FinalFantasy:
                            if (data1 == 116)
                            {
                                return LoopStart();
                            }
                            if (data1 == 117)
                            {
                                return LoopEnd();
                            }
                            break;
                    }
                }

                return new Message(channel, command, data1, data2);
            }

            public static Message TempoChange(double tempo)
            {
                return TempoChange((int)(60000000.0 / tempo));
            }
            public static Message TempoChange(int tempo)
            {
                byte command = (byte)(tempo >> 16);
                byte data1 = (byte)(tempo >> 8);
                byte data2 = (byte)(tempo);
                return new Message((int)MessageType.TempoChange, command, data1, data2);
            }

            public static Message LoopStart()
            {
                return new Message((int)MessageType.LoopStart, 0, 0, 0);
            }

            public static Message LoopEnd()
            {
                return new Message((int)MessageType.LoopEnd, 0, 0, 0);
            }

            public static Message EndOfTrack()
            {
                return new Message((int)MessageType.EndOfTrack, 0, 0, 0);
            }

            public override string ToString()
            {
                switch (channel)
                {
                    case (int)MessageType.TempoChange:
                        return "Tempo: " + Tempo;

                    case (int)MessageType.LoopStart:
                        return "LoopStart";

                    case (int)MessageType.LoopEnd:
                        return "LoopEnd";

                    case (int)MessageType.EndOfTrack:
                        return "EndOfTrack";

                    default:
                        return "CH" + channel + ": " + command.ToString("X2") + ", " + data1.ToString("X2") + ", " + data2.ToString("X2");
                }
            }

            public MessageType Type
            {
                get
                {
                    switch (channel)
                    {
                        case (int)MessageType.TempoChange:
                            return MessageType.TempoChange;

                        case (int)MessageType.LoopStart:
                            return MessageType.LoopStart;

                        case (int)MessageType.LoopEnd:
                            return MessageType.LoopEnd;

                        case (int)MessageType.EndOfTrack:
                            return MessageType.EndOfTrack;

                        default:
                            return MessageType.Normal;
                    }
                }
            }

            public byte Channel { get => channel; set => channel = value; }
            public byte Command { get => command; set => command = value; }
            public byte Data1 { get => data1; set => data1 = value; }
            public byte Data2 { get => data2; set => data2 = value; }
            public TimeSpan Time { get => time; set => time = value; }
            public int Ticks { get => ticks; set => ticks = value; }

            public double Tempo
            {
                get => 60000000.0 / ((command << 16) | (data1 << 8) | data2);
                set
                {
                    uint largenum = (uint)(60000000.0 / value);
                    command = (byte)((largenum >> 16) & 0xFF);
                    data1 = (byte)((largenum >> 8) & 0xFF);
                    data2 = (byte)((largenum & 0xFF));
                }
            }

        }
        public enum MessageType
        {
            Normal = 0,
            TempoChange = 252,
            LoopStart = 253,
            LoopEnd = 254,
            EndOfTrack = 255
        }
    }
}
