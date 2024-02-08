using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Input;

namespace ArtistTool
{
    public class MbArtist : IComparable<MbArtist>
    {
        public string Name { get; set; }
        public long Votes { get; set; }
        public string Id { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string namelwr;

        public string[] words;
        public string[] rawWords;
        int IComparable<MbArtist>.CompareTo(MbArtist? other)
        {
            return this.Name.CompareTo(other?.Name);
        }
    }

    public class MbTitle
    {
        public string Song { get; set; }
        public string ArtistKey { get; set; }
        public string Title { get; set; }

        public string[] words;
        public int idx;
    }
    public class MbDb
    {
        List<MbArtist> artists = new List<MbArtist>();
        Dictionary<string, List<MbArtist>> artistWords = new Dictionary<string, List<MbArtist>>();

        List<MbTitle> titles = new List<MbTitle>();
        Dictionary<string, HashSet<int>> titleWords = new Dictionary<string, HashSet<int>>();

        public Dictionary<string, HashSet<int>> TitleWords => titleWords;
        public Dictionary<string, List<MbArtist>> ArtistWords => artistWords;

        public List<MbArtist> filteredArtists;
        public IEnumerable<MbArtist> FilteredArtists
        {
            get
            {
                if (filteredArtists == null)
                {
                    filteredArtists = Artists.ToList();
                }
                return filteredArtists;
            }
        }


        public void SetArtistFilter(string filter)
        {
            if (filter.Length == 0)
                filteredArtists = Artists.ToList();
            string filterlwr = filter.ToLower();
            filteredArtists = Artists.Where(a => a.namelwr.Contains(filterlwr)).ToList();
        }
        public List<MbArtist> Artists => artists;
        public HashSet<string> Common100 = null;

        public void LoadArtists()
        {
            string[] allwords = File.ReadAllLines("20kwords.txt");
            Common100 = allwords.Take(100).ToHashSet();
            Dictionary<string, int> wordRanks = new Dictionary<string, int>();
            for (int idx = 0; idx < allwords.Length; idx++) {
                wordRanks.Add(allwords[idx], idx);
            }

            SQLiteConnection sqlite_conn = null;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source=database.db; Version = 3; Read Only=True;");
            sqlite_conn.Open();
            SQLiteCommand sqlite_cmd;
            string Createsql = "SELECT * FROM ARTISTS WHERE VOTES > 0";
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = Createsql;
            SQLiteDataReader r = sqlite_cmd.ExecuteReader();
            while (r.Read())
            {
                var artist = new MbArtist();
                artist.Id = Convert.ToString(r["artistKey"]);
                artist.Name = Convert.ToString(r["name"]);
                artist.Votes = Convert.ToInt64(r["votes"]);
                artist.namelwr = artist.Name.ToLower();
                artists.Add(artist);
            }

            foreach (var artist in artists)
            {
                artist.namelwr = artist.Name.ToLower();
            }
            artists.Sort();

            List<MbArtist> nonGenericNames = new List<MbArtist>();
            foreach (MbArtist artist in artists)
            {
                string[] words = artist.Name.Split(new char[] { ' ', '-', '_', '.', '\x2010' });
                List<string> artWds = new List<string>();
                List<string> artRawWds = new List<string>();
                int totalRank = 1;
                foreach (string wrd in words)
                {
                    string word = wrd.ToLower();
                    if (Common100.Contains(word))
                        continue;
                    if (word.EndsWith('s') && Common100.Contains(word.Substring(0, word.Length - 1)))
                        continue;
                    int rank;
                    if (!wordRanks.TryGetValue(word, out rank))
                        rank = 10000;
                    totalRank *= rank;
                }
                if (totalRank < 10000)
                    continue;

                foreach (string wrd in words)
                {
                    string word = wrd.ToLower();
                    artRawWds.Add(word);
                    if (Common100.Contains(word))
                        continue;
                    if (word.EndsWith('s') && Common100.Contains(word.Substring(0, word.Length-1)))
                        continue;
                    List<MbArtist> artistw;
                    if (!artistWords.TryGetValue(word, out artistw))
                    {
                        artistw = new List<MbArtist>();
                        artistWords.Add(word, artistw);
                    }

                    artistw.Add(artist);
                    artWds.Add(word);
                }
                artist.words = artWds.ToArray();
                artist.rawWords = artRawWds.ToArray();
                nonGenericNames.Add(artist);
            }
            artists = nonGenericNames;
            artists.Sort();
            filteredArtists = Artists.ToList();
        }

        public void LoadTitles()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string[] allwords = File.ReadAllLines("20kwords.txt");
            Common100 = allwords.Take(100).ToHashSet();

            SQLiteConnection sqlite_conn = null;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source=database.db; Version = 3; Read Only=True;");
            sqlite_conn.Open();
            SQLiteCommand sqlite_cmd;
            string Createsql = "SELECT song FROM TITLES";
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = Createsql;
            SQLiteDataReader r = sqlite_cmd.ExecuteReader();
            titles = new List<MbTitle>();
            int idx = 0;
            while (r.Read())
            {
                var title = new MbTitle();
                //title.ArtistKey = Convert.ToString(r["artistKey"]);
                title.Song = Convert.ToString(r["song"]);
                //title.Title = Convert.ToString(r["title"]);
                title.idx = idx++;
                titles.Add(title);
            }


            string titleJson = @"titlewords.json";
            if (File.Exists(titleJson))
            {
                using (StreamReader file = File.OpenText(titleJson))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    titleWords = (Dictionary<string, HashSet<int>>)serializer.Deserialize(file, typeof(Dictionary<string, HashSet<int>>));
                }
            }
            else
            {
                idx = 0;
                foreach (MbTitle title in titles)
                {
                    string[] words = title.Song.Split(new char[] { ' ', '-', '_', '.', '\x2010' });
                    List<string> artWds = new List<string>();
                    foreach (string wrd in words)
                    {
                        string word = wrd.ToLower();
                        if (Common100.Contains(word))
                            continue;
                        if (word.EndsWith('s') && Common100.Contains(word.Substring(0, word.Length - 1)))
                            continue;
                        HashSet<int> titlew;
                        if (!titleWords.TryGetValue(word, out titlew))
                        {
                            titlew = new HashSet<int>();
                            titleWords.Add(word, titlew);
                        }

                        titlew.Add(title.idx);
                        artWds.Add(word);
                    }
                    title.words = artWds.ToArray();
                    idx++;
                }

                using (StreamWriter file = File.CreateText(titleJson))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, titleWords);
                }
            }
            long seconds = sw.ElapsedMilliseconds / 1000;
            Trace.WriteLine($"Time {seconds}s");
        }
    }
}
