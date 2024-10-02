using MeltySynth;
using NAudio.Midi;
using NAudio.Wave;
using System;
using System.Collections;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static MeltySynth.MidiSynthSequencer;

// See https://aka.ms/new-console-template for more information

var currentPlayerMidifile = new MeltySynth.MidiFile(args[0]);
midilib.MidiSong song = new midilib.MidiSong(currentPlayerMidifile);
midilib.MidiSong pianoSong = song.ConvertToPianoSong();
FileStream fs = new FileStream(args[1], FileMode.Create, FileAccess.Write);
pianoSong.SaveToStream(fs);
fs.Close();