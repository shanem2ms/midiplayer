using System;
using MeltySynth;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;

namespace midilib
{
    public class SongDb
    {
        MidiDb db;
        SQLiteConnection con;
        public SongDb(MidiDb _db)
        {
            db = _db;
            string file = Path.Combine(_db.HomeDir, "song.db");
            if (File.Exists(file))
                File.Delete(file);
            con = new SQLiteConnection($"Data Source={file}; Version=3;New=True;Compress=True;");
        }

        public async Task<bool> Build()
        {
            con.Open();

            Stopwatch sw = new Stopwatch();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "CREATE TABLE songs (Id INTEGER PRIMARY KEY, Name TEXT, Length INT, Channels INT)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE channelpatches (Id INTEGER, Channel INTEGER, Patch INTEGER, FOREIGN KEY(ID) REFERENCES songs(Id))";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE songtext (Id INTEGER, TEXT Text, TrackId INTEGER, FOREIGN KEY(ID) REFERENCES songs(Id))";
            cmd.ExecuteNonQuery();

            int rowcnt = 0;
            List<MidiDb.Fi> allfiles = db.FilteredMidiFiles.ToList();
            for (int idx = 0; idx < allfiles.Count;)
            {
                int batchSize = Math.Min(allfiles.Count() - idx, 1024);
                var batch = allfiles.GetRange(idx, idx + batchSize);

                sw.Reset();
                sw.Start();
                cmd.CommandText = $"BEGIN";
                await cmd.ExecuteNonQueryAsync();

                var tasks = batch.Select(async fi =>
                {
                    int rowId = Interlocked.Increment(ref rowcnt);
                    string path = await db.GetLocalFile(fi, false);
                    if (path == null) return false;
                    try
                    {
                        MidiFile midiFile = new MidiFile(path);
                        MidiFile.Message[] messages = midiFile.Messages;
                        var channelMessages = messages.GroupBy(m => m.Channel).ToDictionary(g => g.Key, g => g.ToList());
                        int ms = midiFile.Length.Milliseconds;
                        int channels = channelMessages.Keys.Count - 1;
                        string escname = fi.Name.Replace("'", "''");
                        cmd.CommandText = $"INSERT INTO songs (Id, Name, Length, Channels) VALUES ({rowId}, '{escname}', {ms}, {channels})";
                        await cmd.ExecuteNonQueryAsync();

                        foreach (var kv in channelMessages)
                        {
                            var msgs = kv.Value;
                            var patchmsg = msgs.FirstOrDefault(m => (m.Command & 0xF0) == 0xC0);
                            if (patchmsg.Command != 0)
                            {
                                cmd.CommandText = $"INSERT INTO channelpatches (Id, Channel, Patch) VALUES ({rowId}, {kv.Key}, {patchmsg.Data1})";
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        MidiFile.Meta[] metas = midiFile.Metas;
                        foreach (var meta in metas)
                        {
                            if (meta.metaType == 3)
                            {
                                string metatext = meta.GetStringData().Replace("'", "''");
                                cmd.CommandText = $"INSERT INTO songtext (Id, Text, TrackId) VALUES ({rowId}, '{metatext}', {meta.trackIdx})";
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"FAILED {path}");
                    }
                    return true;

                });                
                await Task.WhenAll(tasks);
                cmd.CommandText = $"COMMIT";
                await cmd.ExecuteNonQueryAsync();

                sw.Stop();                
                idx += batchSize;
                Console.WriteLine($"Complete {idx} of {allfiles.Count}. {sw.ElapsedMilliseconds / 1000} seconds");
            }
            con.Close();
            return true;
        }
        public void AddSong()
        {
        }
    }

    static class GMInstruments
    {
        public static string[] Names =
        {
            "Acoustic Grand Piano",
            "Bright Acoustic Piano",
            "Electric Grand Piano",
            "Honky-tonk Piano",
            "Electric Piano 1",
            "Electric Piano 2",
            "Harpsichord",
            "Clavi",
            "Celesta",
            "Glockenspiel",
            "Music Box",
            "Vibraphone",
            "Marimba",
            "Xylophone",
            "Tubular Bells",
            "Dulcimer",
            "Drawbar Organ",
            "Percussive Organ",
            "Rock Organ",
            "Church Organ",
            "Reed Organ",
            "Accordion",
            "Harmonica",
            "Tango Accordion",
            "Acoustic Guitar (nylon)",
            "Acoustic Guitar (steel)",
            "Electric Guitar (jazz)",
            "Electric Guitar (clean)",
            "Electric Guitar (muted)",
            "Overdriven Guitar",
            "Distortion Guitar",
            "Guitar harmonics",
            "Acoustic Bass",
            "Electric Bass (finger)",
            "Electric Bass (pick)",
            "Fretless Bass",
            "Slap Bass 1",
            "Slap Bass 2",
            "Synth Bass 1",
            "Synth Bass 2",
            "Violin",
            "Viola",
            "Cello",
            "Contrabass",
            "Tremolo Strings",
            "Pizzicato Strings",
            "Orchestral Harp",
            "Timpani",
            "String Ensemble 1",
            "String Ensemble 2",
            "SynthStrings 1",
            "SynthStrings 2",
            "Choir Aahs",
            "Voice Oohs",
            "Synth Voice",
            "Orchestra Hit",
            "Trumpet",
            "Trombone",
            "Tuba",
            "Muted Trumpet",
            "French Horn",
            "Brass Section",
            "SynthBrass 1",
            "SynthBrass 2",
            "Soprano Sax",
            "Alto Sax",
            "Tenor Sax",
            "Baritone Sax",
            "Oboe",
            "English Horn",
            "Bassoon",
            "Clarinet",
            "Piccolo",
            "Flute",
            "Recorder",
            "Pan Flute",
            "Blown Bottle",
            "Shakuhachi",
            "Whistle",
            "Ocarina",
            "Lead 1 (square)",
            "Lead 2 (sawtooth)",
            "Lead 3 (calliope)",
            "Lead 4 (chiff)",
            "Lead 5 (charang)",
            "Lead 6 (voice)",
            "Lead 7 (fifths)",
            "Lead 8 (bass + lead)",
            "Pad 1 (new age)",
            "Pad 2 (warm)",
            "Pad 3 (polysynth)",
            "Pad 4 (choir)",
            "Pad 5 (bowed)",
            "Pad 6 (metallic)",
            "Pad 7 (halo)",
            "Pad 8 (sweep)",
            "FX 1 (rain)",
            "FX 2 (soundtrack)",
            "FX 3 (crystal)",
            "FX 4 (atmosphere)",
            "FX 5 (brightness)",
            "FX 6 (goblins)",
            "FX 7 (echoes)",
            "FX 8 (sci-fi)",
            "Sitar",
            "Banjo",
            "Shamisen",
            "Koto",
            "Kalimba",
            "Bag pipe",
            "Fiddle",
            "Shanai",
            "Tinkle Bell",
            "Agogo",
            "Steel Drums",
            "Woodblock",
            "Taiko Drum",
            "Melodic Tom",
            "Synth Drum",
            "Reverse Cymbal",
            "Guitar Fret Noise",
            "Breath Noise",
            "Seashore",
            "Bird Tweet",
            "Telephone Ring",
            "Helicopter",
            "Applause",
            "Gunshot"
        };
        public static string[] Drums =
        {
            "Acoustic Bass Drum",
            "Bass Drum 1",
            "Side Stick",
            "Acoustic Snare",
            "Hand Clap",
            "Electric Snare",
            "Low Floor Tom",
            "Closed Hi Hat",
            "High Floor Tom",
            "Pedal Hi-Hat",
            "Low Tom",
            "Open Hi-Hat",
            "Low-Mid Tom",
            "Hi-Mid Tom",
            "Crash Cymbal 1",
            "High Tom",
            "Ride Cymbal 1",
            "Chinese Cymbal",
            "Ride Bell",
            "Tambourine",
            "Splash Cymbal",
            "Cowbell",
            "Crash Cymbal 2",
            "Vibraslap",
            "Ride Cymbal 2",
            "Hi Bongo",
            "Low Bongo",
            "Mute Hi Conga",
            "Open Hi Conga",
            "Low Conga",
            "High Timbale",
            "Low Timbale",
            "High Agogo",
            "Low Agogo",
            "Cabasa",
            "Maracas",
            "Short Whistle",
            "Long Whistle",
            "Short Guiro",
            "Long Guiro",
            "Claves",
            "Hi Wood Block",
            "Low Wood Block",
            "Mute Cuica",
            "Open Cuica",
            "Mute Triangle",
            "Open Triangle"
                    };
    }
}
