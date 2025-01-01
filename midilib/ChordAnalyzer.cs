using MeltySynth;
using NAudio.Midi;
using System.Drawing;
using System.Linq;
using System;
using System.Collections.Generic;
using Amazon.Runtime.Internal.Transform;
using System.Diagnostics;
using System.Net.WebSockets;

namespace midilib
{
    public class ChordAnalyzer
    {
        static int[] ChordWeights = new int[12]
         {
                    6, // C
                    -5, // C#
                    2,  // D
                    -5, // D#
                    2,  // E
                    3, // F
                    -1,  // F#
                    5,  // G
                    -5, // G#
                    3,  // A
                    0,  // A#
                    1 // B
         };

        static int[] ScaleNotePriorities = new int[]
        {
            0,
            7,
            5,
            9,
            4,
            2,
            11
        };

        int resolution;
        public ChordAnalyzer()
        {
        }

        int songKey = 0;
        public int SongKey => songKey;
        public static string[] KeyNames { get; } = new string[12]
        {
            "C",
            "C#",
            "D",
            "Eb",
            "E",
            "F",
            "F#",
            "G",
            "Ab",
            "A",
            "Bb",
            "B"
        };

        public enum ChordType
        {
            Major,
            Minor,
            Major7,
            Minor7,
            Dominant7,
            Minor7b5,
            Diminished7,
            MinorMajor7,
            Major6,
            Minor6,
            Dominant6,
            Minor6b5,
            Diminished6,
            MinorMajor6
        };

        class ChordMapping
        {
            List<int> notes;
            int[] noteOn = new int[12];

            public List<int> Notes => notes;
            public int NoteCount => notes.Count;
            public ChordMapping(List<int> _notes)
            {
                notes = _notes;
                for (int i = 0; i < noteOn.Length; ++i)
                {
                    noteOn[i] = 0;
                }
                noteOn[notes[0]] = 5;
                noteOn[notes[1]] = 2;
                noteOn[notes[2]] = 3;
                if (notes.Count > 3)
                    noteOn[notes[3]] = 1;
            }

            public List<int> Intersect(IEnumerable<int> _notes)
            {
                return _notes.Intersect(notes).ToList();
            }

            public int Matches(List<int> _notes)
            {
                if (_notes.Count == 0)
                    return 0;
                int value = -NoteCount;
                foreach (var n in _notes)
                {
                    value += noteOn[n];
                }
                return value;
            }
        }

        static Dictionary<ChordType, ChordMapping> ChordTypes = new Dictionary<ChordType, ChordMapping>
        {
            { ChordType.Major7,  new ChordMapping(new List<int> { 0, 4, 7, 11 })},  // major 7
            { ChordType.Minor7,  new ChordMapping(new List<int> { 0, 3, 7, 10 })},  // minor 7
            { ChordType.Dominant7,  new ChordMapping(new List<int> { 0, 4, 7, 10 })},  // dominant 7
            { ChordType.Minor7b5,  new ChordMapping(new List<int> { 0, 3, 6, 10 })},  // minor 7 b5
            { ChordType.Diminished7,  new ChordMapping(new List<int> { 0, 3, 6, 11 })},  // diminished 7
            { ChordType.MinorMajor7,  new ChordMapping(new List<int> { 0, 3, 7, 11 })},  // minor-major 7
            { ChordType.Major6,  new ChordMapping(new List<int> { 0, 4, 7, 9 })},  // major 6
            { ChordType.Minor6,  new ChordMapping(new List<int> { 0, 3, 7, 8 })},  // minor 6
            { ChordType.Dominant6, new ChordMapping( new List<int> { 0, 4, 7, 8 })},  // dominant 6
            { ChordType.Minor6b5,  new ChordMapping(new List<int> { 0, 3, 6, 8 })},  // minor 6 b5
            { ChordType.Diminished6,  new ChordMapping(new List<int> { 0, 3, 6, 9 })},  // diminished 6
            { ChordType.MinorMajor6,  new ChordMapping(new List<int> { 0, 3, 7, 9 })},  // minor-major 6
            { ChordType.Major, new ChordMapping(new List<int> { 0, 4, 7 })},
            { ChordType.Minor, new ChordMapping(new List<int> { 0, 3, 7 })},  // minor
        };

        public class Chord
        {
            public int BaseNote;
            public ChordType ChordType;

            public Chord() { BaseNote = -1; }
            public override string ToString()
            {
                if (BaseNote < 0) return "";
                return KeyNames[BaseNote] + " " + ChordType.ToString();
            }

            public static bool Compare(Chord a, Chord b)
            {
                if (a == null) return b == null ? true : false;
                if (b == null) return a == null ? true : false;

                if (a.ChordType != b.ChordType) return false;

                return a.BaseNote == b.BaseNote;
            }

            public List<int> GetNotes(int octave)
            {
                ChordMapping mapping = ChordTypes[ChordType];
                return 
                    mapping.Notes.Select(n => n + BaseNote + 12 * octave).ToList();
            }
        }

        public class TimedChord
        {
            public int startTicks;
            public int lengthTicks;
            public Chord chord;
        }
        List<int> Transpose(int chord, List<int> list)
        {
            return list.Select(n => (n + 12 + chord) % 12).ToList();
        }
        public Chord GetChord(int SongChord, List<int> notes)
        {
            List<int> scaleNotes = notes.Select(n => n % 12).Distinct().ToList();

            Chord bestChord = null;
            int bestVal = 0;
            foreach (var scaleNote in ScaleNotePriorities)
            {
                int baseNote = (SongChord + scaleNote) % 12;
                List<int> transposedNotes = Transpose(-baseNote, scaleNotes);
                transposedNotes.Sort();
                foreach (var chordType in ChordTypes)
                {
                    int matchVal = chordType.Value.Matches(transposedNotes);
                    if (matchVal > bestVal)
                    {
                        bestChord = new Chord() { BaseNote = baseNote, ChordType = chordType.Key };
                        bestVal = matchVal;
                    }
                }
            }
            return bestChord;
        }

        public void Analyze(MeltySynth.MidiFile midiFile)
        {
            int pixelsPerSixteenth = 10;
            int sixteenthRes = midiFile.Resolution / 4;
            double pixelsPerTick = (double)pixelsPerSixteenth / (double)sixteenthRes;
            int lastTick = midiFile.Messages.Last().Ticks;
            long lengthSixteenths = lastTick / sixteenthRes;

            var channelGroups = midiFile.Messages.Where(m => m.Channel < 16).GroupBy(m => m.Channel).
                OrderBy(g => g.Key);
            int numChannels = channelGroups.Count();

            int gmNoteRange = GMInstruments.MidiEndIdx - GMInstruments.MidiStartIdx;

            Random r = new Random();
            int numSixteenths = (lastTick + 1) / sixteenthRes;
            int[] noteCounts = new int[12];
            for (int i = 0; i < numChannels; i++)
            {
                int[] noteOnTick = new int[127];
                for (int j = 0; j < noteOnTick.Length; j++)
                    noteOnTick[j] = -1;

                var grp = channelGroups.ElementAt(i);
                if (grp.Key == 9)
                    continue;
                int msgIdx = 0;
                int msgLength = grp.Count();
                for (int sixteenthIdx = 0; sixteenthIdx < numSixteenths; sixteenthIdx++)
                {
                    int tickIdx = sixteenthIdx * sixteenthRes;
                    while (msgIdx < msgLength)
                    {
                        var msg = grp.ElementAt(msgIdx);
                        if (msg.Ticks > tickIdx)
                            break;

                        if ((msg.Command & 0xF0) == 0x90 &&
                        msg.Data2 > 0)
                        {
                            if (noteOnTick[msg.Data1] == -1)
                                noteOnTick[msg.Data1] = msg.Ticks;
                        }
                        else if ((msg.Command & 0xF0) == 0x80 ||
                            ((msg.Command & 0xF0) == 0x90 &&
                            msg.Data2 == 0))
                        {
                            int startTicks = noteOnTick[msg.Data1];
                            int endTicks = msg.Ticks;
                            noteOnTick[msg.Data1] = -1;
                        }

                        msgIdx++;
                    }

                    int[] chordNotes = new int[12];
                    int chordCount = 0;
                    for (int noteIdx = 0; noteIdx < noteOnTick.Length; noteIdx++)
                    {
                        if (noteOnTick[noteIdx] >= 0)
                        {
                            noteCounts[noteIdx % 12]++;
                            if (chordNotes[noteIdx % 12]++ == 0)
                                chordCount++;
                        }
                    }

                    if (chordCount >= 3)
                    {
                    }
                }
            }
            songKey = CalculateKey(noteCounts);
        }

        public Dictionary<int, Chord> BuildChordsForTrack2(int songKey, MidiSong.TrackInfo track,
            int reesolution)
        {
            Dictionary<int, Chord> songChords = new Dictionary<int, Chord>();
            int expireTicks = reesolution / 4;
            List<MidiSong.Note> activeNotes = new List<MidiSong.Note>();
            List<MidiSong.Note> nextActiveNotes = new List<MidiSong.Note>();
            var tickGroups = track.Notes.GroupBy(t => t.startTicks).OrderBy(g => g.Key);
            foreach (var noteGrp in tickGroups)
            {
                int curTickTime = noteGrp.First().startTicks;
                int expireTickTime = curTickTime - expireTicks;
                nextActiveNotes.Clear();
                foreach (var an in activeNotes)
                {
                    if (an.startTicks > expireTickTime &&
                        (an.startTicks + an.lengthTicks > curTickTime))
                    {
                        nextActiveNotes.Add(an);
                    }
                }
                foreach (var note in noteGrp)
                {
                    nextActiveNotes.Add(note);
                }

                if (nextActiveNotes.Count >= 3)
                {
                    Chord c = GetChord(songKey,
                        nextActiveNotes.Select(n => (int)n.note).ToList());
                    if (c != null)
                    {
                        songChords.Add(curTickTime, c);
                    }
                }
                var tmp = nextActiveNotes;
                nextActiveNotes = activeNotes;
                activeNotes = tmp;
            }

            return songChords;
        }

        class NoteStartEnd
        {
            public int ticks;
            public MidiSong.Note note;
            public bool isstart;
        }

        public List<TimedChord> BuildChordsForTrack(int songKey, MidiSong.TrackInfo track,
            int reesolution)
        {            
            List<NoteStartEnd> notes = new List<NoteStartEnd>();
            foreach (var note in track.Notes)
            {
                notes.Add(new NoteStartEnd() { ticks = note.startTicks, note = note, isstart = true });
                notes.Add(new NoteStartEnd() { ticks = note.startTicks + note.lengthTicks, note = note, isstart = false });
            }

            notes.Sort((a, b) => a.ticks.CompareTo(b.ticks));

            List<TimedChord> songChords = new List<TimedChord>();
            int expireTicks = reesolution / 4;
            List<MidiSong.Note> activeNotes = new List<MidiSong.Note>();
            var tickGroups = notes.GroupBy(t => t.ticks).OrderBy(g => g.Key);
            
            Chord chord = null;
            int startTicks = -1;
            foreach (var noteGrp in tickGroups)
            {
                int curTicks = noteGrp.Key;
                foreach (var note in noteGrp)
                {
                    if (note.isstart)
                        activeNotes.Add(note.note);
                    else
                        activeNotes.Remove(note.note);
                }

                if (activeNotes.Count >= 3)
                {
                    Chord c = GetChord(songKey,
                        activeNotes.Select(n => (int)n.note).ToList());
                    if (!Chord.Compare(chord, c))
                    {
                        if (chord != null)
                        {
                            songChords.Add(new TimedChord()
                                { chord = chord, startTicks = startTicks, lengthTicks = curTicks - startTicks });
                        }
                        chord = c;
                        startTicks = curTicks;
                    }
                }
                else
                {
                    if (chord != null)
                    {
                        songChords.Add(new TimedChord()
                            { chord = chord, startTicks = startTicks, lengthTicks = curTicks - startTicks });
                    }
                    chord = null;
                    startTicks = -1;
                }
            }

            return songChords;
        }
        
        int CalculateKey(int[] noteOccurences)
        {
            int[] keyWeights = new int[12];
            for (int i = 0; i < 12; ++i)
            {
                for (int j = 0; j < 12; ++j)
                {
                    keyWeights[i] += noteOccurences[(j + i) % 12] * ChordWeights[j];
                }
            }
            return keyWeights.ToList().IndexOf(keyWeights.Max());
        }
    }
}
