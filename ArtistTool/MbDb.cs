using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

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
        int IComparable<MbArtist>.CompareTo(MbArtist? other)
        {
            return this.Name.CompareTo(other?.Name);
        }
    }

    public class MbDb
    {
        List<MbArtist> artists = new List<MbArtist>();
        Dictionary<string, List<MbArtist>> artistWords = new Dictionary<string, List<MbArtist>>();

        public Dictionary<string, List<MbArtist>> ArtistWords => artistWords;

        public List<MbArtist> filteredArtists;
        public IEnumerable<MbArtist> FilteredArtists
        {
            get
            {
                if (filteredArtists == null)
                    filteredArtists = Artists.ToList();
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
        HashSet<string> wordHash = null;

        public void LoadArtists()
        {
            string[] allwords = File.ReadAllLines("20kwords.txt");
            wordHash = allwords.Take(100).ToHashSet();

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
                artist.namelwr = artist.Name.ToLower();
                artists.Add(artist);
            }

            foreach (var artist in artists)
            {
                artist.namelwr = artist.Name.ToLower();
            }
            artists.Sort();

            foreach (MbArtist artist in artists)
            {
                string[] words = artist.Name.Split(' ');
                List<string> artWds = new List<string>();
                foreach (string wrd in words)
                {
                    string word = wrd.ToLower();
                    if (wordHash.Contains(word))
                        continue;
                    if (word.EndsWith('s') && wordHash.Contains(word.Substring(0, word.Length-1)))
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
            }
            artists.Sort();
            filteredArtists = Artists.ToList();
        }

    }
}
