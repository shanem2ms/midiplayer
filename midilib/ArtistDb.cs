using System;
using MeltySynth;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using static midilib.MidiDb;
using System.Text.RegularExpressions;

namespace midilib
{
    public class Word
    {
        public Word(string _w)
        { word = _w; }
        public string word;
        public List<Song> songs = new List<Song>();

        public IEnumerable<string> Names => songs.Select(f => f.Name);
        public string Text => word;
        public int Count => songs.Count;

    }

    public class Artist
    {
        public string Name { get; set; }
        public int votes;
        public List<Song> songs = new List<Song>();
        public List<Song> Songs => songs;

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Artist)
            {
                return Name == (obj as Artist).Name;
            }
            else
                return false;
        }
    }

    public class Song
    {
        public Song(MidiDb.Fi fi)
        {
            Fi = fi;
        }
        public string Name => Fi.Name;
        public Artist Artist { get; set; }
        public MidiDb.Fi Fi { get; set; }
    }
    public class ArtistsFile
    {
        public Dictionary<string, List<string>> artistsSongs { get; set; }
    }
    public class ArtistDb
    {
        MidiDb db;
        public List<Word> Words { get; private set; }

        public List<Artist> Artists { get; } = new List<Artist>();

        IEnumerable<Artist> filteredArtists;
        public IEnumerable<Artist> FilteredArtists { get
            {
                if (filteredArtists == null)
                    filteredArtists = Artists.ToList();
                return filteredArtists;
            } 
            }

        public List<Song> songs;
        public List<Song> Songs => songs;

        public ArtistDb(MidiDb _db)
        {
            db = _db;
            songs = db.FilteredMidiFiles.Select(fi => new Song(fi)).ToList();
        }

        public void SetArtistFilter(string filter)
        {
            filteredArtists = Artists.Where(a => a.Name.Contains(filter)).ToList();
        }
        List<string> GetAllWords(string name)
        {
            name = Path.GetFileNameWithoutExtension(name);
            Regex r = new Regex(@"[^a-zA-Z]*([a-zA-Z]+)[^a-zA-Z]*");
            bool keepgoing = true;
            int index = 0;
            List<string> strs = new List<string>();
            while (keepgoing)
            {
                var match = r.Match(name, index);
                if (match.Success)
                {
                    index = match.Captures[0].Index + match.Captures[0].Length;
                    strs.Add(match.Groups[1].Value.ToLower());
                }
                keepgoing = match.Success;
            }
            return strs;
        }
        public bool BuildArtists()
        {
            Dictionary<string, Word> allWords = new Dictionary<string, Word>();
            foreach (var song in songs)
            {
                List<string> words = GetAllWords(song.Name);
                foreach (var word in words)
                {
                    int count = 0;
                    Word word1 = null;
                    if (!allWords.TryGetValue(word, out word1))
                    {
                        word1 = new Word(word);
                        allWords[word] = word1;
                    }
                    word1.songs.Add(song);
                }
            }

            Words =
                allWords.Where(kv => kv.Key.Length > 3).Select(kv => kv.Value).ToList();


            Words.Sort((a, b) => b.songs.Count - a.songs.Count);
            return true;
        }
    }


}
