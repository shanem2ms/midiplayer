using System;
using MeltySynth;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static midilib.MidiDb;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

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

        List<Song> songs = new List<Song>();
        public List<Song> Songs => songs;
        public int Votes { get; set; } = 0;

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

        [JsonIgnore]
        public Artist Artist { get; set; }
        [JsonIgnore]
        public MidiDb.Fi Fi { get; set; }

        [JsonIgnore]
        public List<string> Words { get; set; }
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

        List<Artist> filteredArtists;
        public IEnumerable<Artist> FilteredArtists
        {
            get
            {
                if (filteredArtists == null)
                {
                    filteredArtists = Artists.ToList();
                    filteredArtists.Sort((a, b) => b.Votes.CompareTo(a.Votes));
                }
                return filteredArtists;
            }
        }

        public List<Song> songs;
        public List<Song> Songs => songs;
        public List<Song> UnassignedSongs => songs.Where(s => s.Artist == null).ToList();

        public ArtistDb(MidiDb _db)
        {
            db = _db;
            songs = db.FilteredMidiFiles.Select(fi => new Song(fi)).ToList();
        }


        class ArtistDef
        {
            public ArtistDef()
            { }
            public ArtistDef(Artist a)
            {
                Name = a.Name;
                Votes = a.Votes;
                Songs = a.Songs.Select(s => s.Name).ToList();
            }
            public string Name { get; set; }
            public int Votes { get; set; }
            public List<string> Songs { get; set; }
        }

        public void Load(string filename)
        {
            Dictionary<string, Song> songDic = Songs.ToDictionary(s => s.Name);
            string jsontext = File.ReadAllText(filename);
            List<ArtistDef> artists = JsonConvert.DeserializeObject<List<ArtistDef>>(jsontext);
            foreach (ArtistDef artistDef in artists)
            {
                Artist artist = new Artist();
                artist.Name = artistDef.Name;
                artist.Votes = artistDef.Votes;
                artist.Songs.AddRange(artistDef.Songs.Select((s) => { var sng = songDic[s]; sng.Artist = artist; return sng; })); ;
                Artists.Add(artist);
            }
        }

        public void Save(string filename)
        {
            List<ArtistDef> adefs = Artists.Select(a => new ArtistDef(a)).ToList();
            string jsonstr = JsonConvert.SerializeObject(adefs);
            File.WriteAllText(filename, jsonstr);
        }

        public void SetArtistFilter(string filter)
        {
            string flower = filter.ToLower();
            filteredArtists = Artists.Where(a => a.Name.ToLower().Contains(flower)).ToList();
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

        HashSet<string> GetNumbers(string name)
        {
            name = Path.GetFileNameWithoutExtension(name);
            Regex r = new Regex(@"[^0-9]*([0-9]+)[^0-9]*");
            bool keepgoing = true;
            int index = 0;
            HashSet<string> strs = new HashSet<string>();
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


        public bool BuildSongWords()
        {
            Dictionary<string, Word> allWords = new Dictionary<string, Word>();
            var unassignedSongs = UnassignedSongs;
            foreach (var song in unassignedSongs)
            {
                if (song.Artist != null)
                    continue;
                song.Words = GetAllWords(song.Name);
                foreach (var word in song.Words)
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

                HashSet<string> numbers = GetNumbers(song.Name);
                foreach (var number in numbers)
                {
                    song.Words.Add(number);
                }
            }

            Words =
                allWords.Where(kv => kv.Key.Length > 3).Select(kv => kv.Value).ToList();


            Words.Sort((a, b) => b.songs.Count - a.songs.Count);
            return true;
        }
    }


}
