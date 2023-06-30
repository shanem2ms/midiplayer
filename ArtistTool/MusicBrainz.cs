using midilib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace ArtistTool
{
    public class MbArtist : IComparable<MbArtist>
    {
        public string Name { get; set; }
        public long Votes { get; set; }
        public string Id { get; set; }
        
        [Newtonsoft.Json.JsonIgnore]
        public string namelwr;
        int IComparable<MbArtist>.CompareTo(MbArtist? other)
        {
            return this.Name.CompareTo(other?.Name);
        }
    }

    public class MbArtistCredit
    {
        public string name;
    }
    public class MBTitle
    {
        [JsonProperty("artist-credit")]
        public MbArtistCredit[] artistcredit;
    }

    public class MusicBrainz
    {
        public class Rating
        {
            public object value { get; set; }
            
            [JsonProperty("votes-count")]
            public int votes_count {get;set;}
        }

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

        public List<MbArtist> Artists => artists;
        HashSet<string> wordHash = null;

        public void SetArtistFilter(string filter)
        {
            if (filter.Length == 0)
                filteredArtists = Artists.ToList();
            string filterlwr = filter.ToLower();
            filteredArtists = Artists.Where(a => a.namelwr.Contains(filterlwr)).ToList();
        }

        enum PType
        {
            Property,
            ArrayIdx
        }
        class ParseType
        {
            public PType ptype;
            public int idx;
            public string name;
        }

        public string ReadArtistId(Stream str)
        {
            ParseType[] parsestr = new ParseType[]
            {
                new ParseType() { ptype = PType.Property, idx = 0, name = "artist-credit" },
                new ParseType() { ptype = PType.ArrayIdx, idx = 0, name = "" },
                new ParseType() { ptype = PType.Property, idx = 0, name = "artist" },
                new ParseType() { ptype = PType.Property, idx = 0, name = "id" }
            };

            object? val = ReadJSON(str, parsestr);
            if (val is string)
                return (string)val;
            return "";
        }
        public string ReadTitle(Stream str)
        {
            ParseType[] parsestr = new ParseType[]
            {
                new ParseType() { ptype = PType.Property, idx = 0, name = "title" },
            };

            object? val = ReadJSON(str, parsestr);
            if (val is string)
                return (string)val;
            return "";
        }

        public void LoadArtists()
        {
            string []allwords = File.ReadAllLines("20kwords.txt");
            wordHash = allwords.ToHashSet();
            StreamReader fileStream = File.OpenText("artists.json");
            JsonSerializer serializer = new JsonSerializer();
            artists = (List<MbArtist>)serializer.Deserialize(fileStream, typeof(List<MbArtist>));
            foreach (var artist in artists)
            {
                artist.namelwr = artist.Name.ToLower();
            }
            artists.Sort();

            foreach (MbArtist artist in artists) 
            {
                string []words = artist.Name.Split(' ');
                foreach (string wrd in words) 
                {
                    string word = wrd.ToLower();
                    if (wordHash.Contains(word))
                        continue;
                    List<MbArtist> artistw;
                    if (!artistWords.TryGetValue(word, out artistw))
                    {
                        artistw = new List<MbArtist>();
                        artistWords.Add(word, artistw);
                    }

                    artistw.Add(artist);
                }
            }
            artists.Sort();
            filteredArtists = Artists.ToList();
        }

        public void LoadReleaseFromDatabase(string jsonfile)
        {
            string artistid = @"f27ec8db-af05-4f36-916e-3d57f91ecf5e";
            int artistsVisited = 0;
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            JsonSerializer serializer = new JsonSerializer();
            char[] blockdata = new char[1 << 16];
            using (FileStream s = File.Open(jsonfile, FileMode.Open))
            using (StreamReader sr = new StreamReader(s))
            {
                long fileSize = s.Length;
                int nBrackets = 0;
                bool inDblQuotes = false;
                long linesRead = 0;
                bool escapeNext = false;
                int lastQuote = 0;
                while (true)
                {
                    int charsRead = sr.ReadBlock(blockdata, 0, blockdata.Length);
                    if (charsRead != blockdata.Length)
                        break;
                    int startWriteIdx = 0;
                    for (int idx = 0; idx < charsRead; idx++)
                    {
                        bool escapeThis = escapeNext;
                        switch (blockdata[idx])
                        {
                            case '"':
                                if (!escapeThis)
                                    inDblQuotes = !inDblQuotes;
                                break;
                            //case '\'':
                            //    if (inDblQuotes  == 0 && !escapeThis)
                            //        nSingleQuotes = 1 - nSingleQuotes;
                            //    break;
                            case '{':
                                if (!inDblQuotes)
                                {
                                    nBrackets++;
                                }
                                break;
                            case '}':
                                {
                                    if (!inDblQuotes)
                                    {
                                        nBrackets--;
                                        if (nBrackets == 0)
                                        {
                                            writer.Write(blockdata, startWriteIdx, (idx + 1 - startWriteIdx));
                                            startWriteIdx = idx + 1;
                                            writer.Flush();
                                            stream.Position = 0;
                                            string aid = ReadArtistId(stream);
                                            if (aid == artistid)
                                            {
                                                stream.Position = 0;
                                                string title = ReadTitle(stream);
                                                Trace.WriteLine(title);
                                            }
                                            stream = new MemoryStream();
                                            writer = new StreamWriter(stream);
                                            if (artistsVisited % 1000 == 0)
                                            {
                                                Trace.WriteLine($"{artistsVisited} {(s.Position * 100) / s.Length}");
                                            }
                                            artistsVisited++;
                                        }
                                        else if (nBrackets < 0)
                                            Debugger.Break();
                                    }
                                }
                                break;
                            case '\\':
                                if (!inDblQuotes)
                                    Debugger.Break();
                                escapeNext = true;
                                break;
                        }
                        if (escapeThis) escapeNext = false;
                    }
                    writer.Write(blockdata, startWriteIdx, charsRead - startWriteIdx);
                    linesRead += charsRead;
                }
            }

            FileStream outfile = File.Open(@"artists.json", FileMode.Create, FileAccess.Write);
            StreamWriter artistwriter = new StreamWriter(outfile);
            serializer.Serialize(artistwriter, artists);
            artistwriter.Flush();
        }
        public void LoadFromDatabase(string jsonfile)
        {
            int artistsVisited = 0;
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            char[] blockdata = new char[1 << 16];
            using (FileStream s = File.Open(jsonfile, FileMode.Open))
            using (StreamReader sr = new StreamReader(s))
            {
                int nBrackets = 0;
                bool inDblQuotes  = false;
                long linesRead = 0;
                bool escapeNext = false;
                int lastQuote = 0;
                while (true)
                {
                    int charsRead = sr.ReadBlock(blockdata, 0, blockdata.Length);
                    if (charsRead != blockdata.Length)
                        break;
                    int startWriteIdx = 0;
                    for (int idx = 0; idx < charsRead; idx++)
                    {
                        bool escapeThis = escapeNext;
                        switch (blockdata[idx])
                        {
                            case '"':
                                if (!escapeThis)
                                    inDblQuotes  = !inDblQuotes;
                                break;
                            //case '\'':
                            //    if (inDblQuotes  == 0 && !escapeThis)
                            //        nSingleQuotes = 1 - nSingleQuotes;
                            //    break;
                            case '{':
                                if (!inDblQuotes )
                                {
                                    nBrackets++;
                                }
                                break;
                            case '}':
                                {
                                    if (!inDblQuotes )
                                    {
                                        nBrackets--;
                                        if (nBrackets == 0)
                                        {
                                            writer.Write(blockdata, startWriteIdx, (idx + 1 - startWriteIdx));
                                            startWriteIdx = idx + 1;
                                            writer.Flush();
                                            stream.Position = 0;
                                            if (!LoadJSON(stream))
                                                return;
                                            //WriteStream(stream);
                                            stream = new MemoryStream();
                                            writer = new StreamWriter(stream);
                                            if (artistsVisited % 1000 == 0)
                                            {
                                                Trace.WriteLine($"{artistsVisited} {artists.Count()} l:{linesRead + idx + 1}");
                                            }
                                            artistsVisited++;
                                        }
                                        else if (nBrackets < 0)
                                            Debugger.Break();
                                    }
                                }
                                break;
                            case '\\':
                                if (!inDblQuotes)
                                    Debugger.Break();
                                escapeNext = true;
                                break;
                        }
                        if (escapeThis) escapeNext = false;
                    }
                    writer.Write(blockdata, startWriteIdx, charsRead - startWriteIdx);
                    linesRead += charsRead;
                }
            }

            FileStream outfile = File.Open(@"artists.json", FileMode.Create, FileAccess.Write);
            StreamWriter artistwriter = new StreamWriter(outfile);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(artistwriter, artists);
            artistwriter.Flush();
        }

        static int outidx = 0;
        void WriteStream(Stream stream)
        {
            stream.Position = 0;
            FileStream fs = File.Open($"c:\\out{outidx++}.json", FileMode.Create, FileAccess.Write);
            stream.CopyTo(fs);
            fs.Flush();
            fs.Close();
        }
       
        object? ReadUntil(JsonTextReader reader, JsonToken type, int depth, ParseType[] parse, bool hotpath)
        {
            string propsearch = string.Empty;
            int arrayidx = -1;
            if (hotpath && parse[depth].ptype == PType.Property)
                propsearch = parse[depth].name;
            if (hotpath && parse[depth].ptype == PType.ArrayIdx)
                arrayidx = parse[depth].idx;
            bool isHotProp = false;
            int curidx = 0;
            object ?obj = null;
            while (reader.Read() &&
                reader.TokenType != type)
            {
                bool hot = isHotProp;
                if (curidx == arrayidx)
                    hot = true;
                if (reader.TokenType == JsonToken.StartObject)
                {
                    obj = ReadUntil(reader, JsonToken.EndObject, depth + 1, parse, hot);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    obj = ReadUntil(reader, JsonToken.EndArray, depth + 1, parse, hot);
                }
                else if (propsearch != string.Empty && reader.TokenType == JsonToken.PropertyName &&
                    propsearch == (string)reader.Value)
                {
                    isHotProp = true;
                }
                else if (hot)
                    obj = reader.Value;
                if (hot)
                {
                    break;
                }
                curidx++;
            }

            return obj;
        }

        object ?ReadJSON(Stream stream, ParseType[] parse)
        {
            FileStreamOptions fso = new FileStreamOptions();
            using (StreamReader sr = new StreamReader(stream, null, true, -1, true))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                reader.CloseInput = false;
                object? obj = null;
                try
                {
                    reader.Read();
                    if (reader.TokenType == JsonToken.StartArray)
                    {
                        obj =ReadUntil(reader, JsonToken.EndArray, 0, parse, true);
                    }
                    else if (reader.TokenType == JsonToken.StartObject)
                    {
                        obj = ReadUntil(reader, JsonToken.EndObject, 0, parse, true);
                    }
                    else
                        throw new Exception();
                }
                catch(Exception e)
                {

                }
                return obj;
            }
        }
        bool LoadJSON(Stream stream)
        {
            JsonSerializer serializer = new JsonSerializer();
            MbArtist artist = new MbArtist();
            using (StreamReader sr = new StreamReader(stream))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                try
                {
                    int objnums = 0;
                    while (reader.Read())
                    {
                        // deserialize only when there's "{" character in the stream
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            //jw.WriteToken(reader);
                            objnums++;
                            //jw.Flush();
                            //o = serializer.Deserialize<MyObject>(reader);
                        }
                        else if (reader.TokenType == JsonToken.EndObject)
                        {
                            objnums--;
                            if (objnums == 0)
                            {
                                if (!sr.EndOfStream)
                                    break;
                                if (artist.Votes > 1)
                                {
                                    //Debug.WriteLine($"{artist.name} {artist.votes}");
                                    artists.Add(artist);
                                }
                                return true;
                            }
                        }
                        else if (reader.TokenType == JsonToken.PropertyName &&
                            reader.Depth == 1)
                        {
                            string propname = (string)reader.Value;
                            if (propname == "title")
                            {
                                reader.Read();
                                Trace.WriteLine((string)reader.Value);
                            }
                            if (propname == "name")
                            {
                                reader.Read();
                                artist.Name = (string)reader.Value;
                            }
                            if (propname == "rating")
                            {
                                while (reader.TokenType != JsonToken.EndObject)
                                {
                                    reader.Read();
                                    if (reader.TokenType == JsonToken.PropertyName &&
                                        (string)reader.Value == "votes-count")
                                    {
                                        reader.Read();
                                        artist.Votes = (long)reader.Value;
                                    }
                                }
                            }
                            if (propname == "id")
                            {
                                reader.Read();
                                artist.Id = (string)reader.Value;
                            }
                        }
                    }
                }
                catch
                {
                }
                WriteStream(stream);
                return false;
            }
        }
    }

    static class StreamSearch
    {
        public static long FindPosition(Stream stream, byte[] byteSequence)
        {
            if (byteSequence.Length > stream.Length)
                return -1;

            byte[] buffer = new byte[byteSequence.Length];

            //using (BufferedStream bufStream = new BufferedStream(stream, byteSequence.Length))
            //{
                int i;
                while ((i = stream.Read(buffer, 0, byteSequence.Length)) == byteSequence.Length)
                {
                    if (byteSequence.SequenceEqual(buffer))
                        return stream.Position - byteSequence.Length;
                    else
                        stream.Position -= byteSequence.Length - PadLeftSequence(buffer, byteSequence);
                }
            //}

            return -1;
        }

        private static int PadLeftSequence(byte[] bytes, byte[] seqBytes)
        {
            int i = 1;
            while (i < bytes.Length)
            {
                int n = bytes.Length - i;
                byte[] aux1 = new byte[n];
                byte[] aux2 = new byte[n];
                Array.Copy(bytes, i, aux1, 0, n);
                Array.Copy(seqBytes, aux2, n);
                if (aux1.SequenceEqual(aux2))
                    return i;
                i++;
            }
            return i;
        }
    }
}

