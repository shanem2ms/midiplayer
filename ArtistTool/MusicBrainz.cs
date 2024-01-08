using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ArtistTool
{
    public class MusicBrainz
    {
     
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

        public string[] ReadTracks(Stream str)
        {
            ParseType[] parsestr = new ParseType[]
            {
                new ParseType() { ptype = PType.Property, idx = 0, name = "media" },
                new ParseType() { ptype = PType.ArrayIdx, idx = 0, name = "" },
                new ParseType() { ptype = PType.Property, idx = 0, name = "tracks" },
                new ParseType() { ptype = PType.ArrayIdx, idx = -2, name = "" },
                new ParseType() { ptype = PType.Property, idx = 0, name = "title" },
            };

            object? val = ReadJSON(str, parsestr);
            if (val != null)
            {
                List<object> lobs = (List<object>)val;
                return lobs.Cast<string>().ToArray();
            }
            return null;
        }


        public void LoadReleaseFromDatabase(string jsonfile)
        {
            bool dosql = true;

            SQLiteConnection sqlite_conn = null;
            // Create a new database connection:
            if (dosql)
            {
                if (File.Exists("database.db"))
                    File.Delete("database.db");
                sqlite_conn = new SQLiteConnection("Data Source=database.db; Version = 3; New=True; Compress = True; ");
                sqlite_conn.Open();
                {
                    SQLiteCommand sqlite_cmd;
                    string Createsql = "CREATE TABLE Titles(artistKey TEXT, title TEXT, song TEXT)";
                    sqlite_cmd = sqlite_conn.CreateCommand();
                    sqlite_cmd.CommandText = Createsql;
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
            Stopwatch sw = new Stopwatch();
            string artistid = @"7c7f9c94-dee8-4903-892b-6cf44652e2de";
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
                bool complete = false;
                sw.Start();
                if (dosql)
                {
                    SQLiteCommand sqlite_cmd;
                    sqlite_cmd = sqlite_conn.CreateCommand();
                    sqlite_cmd.CommandText = $"BEGIN TRANSACTION; ";
                    sqlite_cmd.ExecuteNonQuery();
                }

                while (!complete)
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
                                            try
                                            {
                                                string aid = ReadArtistId(stream);
                                                stream.Position = 0;
                                                string[] tracks = ReadTracks(stream);
                                                stream.Position = 0;
                                                string title = ReadTitle(stream);

                                                if (dosql)
                                                {
                                                    title = title.Replace("'", "''");
                                                    if (tracks != null)
                                                    {
                                                        foreach (var track in tracks)
                                                        {
                                                            string tr = track.Replace("'", "''");
                                                            SQLiteCommand sqlite_cmd;
                                                            sqlite_cmd = sqlite_conn.CreateCommand();
                                                            sqlite_cmd.CommandText = $"INSERT INTO Titles (artistKey, title, song) VALUES('{aid}', '{title}', '{tr}'); ";
                                                            sqlite_cmd.ExecuteNonQuery();
                                                        }
                                                    }
                                                }
                                            }
                                            catch
                                            {
                                            }
                                            /*
                                                stream.Position = 0;
                                                FileStream of = File.Open($"{artistsVisited}.json", FileMode.Create, FileAccess.Write);
                                                stream.WriteTo(of);
                                                of.Close();*/
                                            stream = new MemoryStream();
                                            writer = new StreamWriter(stream);
                                            if (artistsVisited % 1000 == 0)
                                            {
                                                if (dosql)
                                                {
                                                    SQLiteCommand sqlite_cmd;
                                                    sqlite_cmd = sqlite_conn.CreateCommand();
                                                    sqlite_cmd.CommandText = $"COMMIT; ";
                                                    sqlite_cmd.ExecuteNonQuery();
                                                    sqlite_cmd = sqlite_conn.CreateCommand();
                                                    sqlite_cmd.CommandText = $"BEGIN TRANSACTION; ";
                                                    sqlite_cmd.ExecuteNonQuery();
                                                }

                                                float percent = (float)(s.Position) / s.Length;
                                                long ms = sw.ElapsedMilliseconds;
                                                float totalMs = ms / percent;
                                                float remainMs = totalMs - ms;
                                                int minutes = (int)(remainMs / (1000 * 60));
                                                int hours = minutes / 60;
                                                minutes = minutes % 60;
                                                Trace.WriteLine($"{artistsVisited} {percent * 100}: Remaining {hours}:{minutes}");
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
            if (dosql)
            {
                SQLiteCommand sqlite_cmd;
                sqlite_cmd = sqlite_conn.CreateCommand();
                sqlite_cmd.CommandText = $"COMMIT; ";
                sqlite_cmd.ExecuteNonQuery();
                sqlite_conn.Close();
            }
        }
        public void LoadArtistsFromDatabase(string jsonfile)
        {
            bool dosql = true;
            if (File.Exists("database.db"))
                File.Delete("database.db");

            SQLiteConnection sqlite_conn = null;
            // Create a new database connection:
            if (dosql)
            {
                sqlite_conn = new SQLiteConnection("Data Source=database.db; Version = 3; New=True; Compress = True; ");
                sqlite_conn.Open();
                {
                    SQLiteCommand sqlite_cmd;
                    string Createsql = "CREATE TABLE Artists(artistKey TEXT, name TEXT, votes NUMBER)";
                    sqlite_cmd = sqlite_conn.CreateCommand();
                    sqlite_cmd.CommandText = Createsql;
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
            Stopwatch sw = new Stopwatch();
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
                bool complete = false;
                sw.Start();
                if (dosql)
                {
                    SQLiteCommand sqlite_cmd;
                    sqlite_cmd = sqlite_conn.CreateCommand();
                    sqlite_cmd.CommandText = $"BEGIN TRANSACTION; ";
                    sqlite_cmd.ExecuteNonQuery();
                }

                while (!complete)
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
                                            try
                                            {
                                                stream.Position = 0;
                                                string id = (string)ReadJSON(stream, new ParseType[]
                                                {
                                                    new ParseType() { ptype = PType.Property, idx = 0, name = "id" }
                                                });

                                                stream.Position = 0;
                                                string name = (string)ReadJSON(stream, new ParseType[]
                                                {
                                                    new ParseType() { ptype = PType.Property, idx = 0, name = "name" }
                                                });

                                                stream.Position = 0;
                                                long votes = (long)ReadJSON(stream, new ParseType[]
                                                {
                                                    new ParseType() { ptype = PType.Property, idx = 0, name = "rating" },
                                                    new ParseType() { ptype = PType.Property, idx = 0, name = "votes-count" },
                                                });
                                                if (dosql)
                                                {
                                                    string nm = name.Replace("'", "''");
                                                    SQLiteCommand sqlite_cmd;
                                                    sqlite_cmd = sqlite_conn.CreateCommand();
                                                    sqlite_cmd.CommandText = $"INSERT INTO Artists (artistKey, name, votes) VALUES('{id}', '{nm}', {votes}); ";
                                                    sqlite_cmd.ExecuteNonQuery();

                                                }
                                            }
                                            catch
                                            {
                                            }

                                            stream.Position = 0;
                                            FileStream of = File.Open($"{artistsVisited}.json", FileMode.Create, FileAccess.Write);
                                            stream.WriteTo(of);
                                            of.Close();
                                            stream = new MemoryStream();
                                            writer = new StreamWriter(stream);
                                            if (artistsVisited % 1000 == 0)
                                            {
                                                if (dosql)
                                                {
                                                    SQLiteCommand sqlite_cmd;
                                                    sqlite_cmd = sqlite_conn.CreateCommand();
                                                    sqlite_cmd.CommandText = $"COMMIT; ";
                                                    sqlite_cmd.ExecuteNonQuery();
                                                    sqlite_cmd = sqlite_conn.CreateCommand();
                                                    sqlite_cmd.CommandText = $"BEGIN TRANSACTION; ";
                                                    sqlite_cmd.ExecuteNonQuery();
                                                }

                                                float percent = (float)(s.Position) / s.Length;
                                                long ms = sw.ElapsedMilliseconds;
                                                float totalMs = ms / percent;
                                                float remainMs = totalMs - ms;
                                                int minutes = (int)(remainMs / (1000 * 60));
                                                int hours = minutes / 60;
                                                minutes = minutes % 60;
                                                Trace.WriteLine($"{artistsVisited} {percent * 100}: Remaining {hours}:{minutes}");
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
            if (dosql)
            {
                SQLiteCommand sqlite_cmd;
                sqlite_cmd = sqlite_conn.CreateCommand();
                sqlite_cmd.CommandText = $"COMMIT; ";
                sqlite_cmd.ExecuteNonQuery();
                sqlite_conn.Close();
            }
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
            if (hotpath && depth < parse.Length && parse[depth].ptype == PType.Property)
                propsearch = parse[depth].name;
            if (hotpath && depth < parse.Length && parse[depth].ptype == PType.ArrayIdx)
                arrayidx = parse[depth].idx;
            bool isHotProp = false;
            int curidx = 0;
            object? obj = null;
            List<object> arrayVals = null;
            if (arrayidx == -2)
                arrayVals = new List<object>();
            while (reader.Read() &&
                reader.TokenType != type)
            {
                bool hot = isHotProp;
                isHotProp = false;
                if (curidx == arrayidx ||
                    arrayidx == -2)
                    hot = true;
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var ret = ReadUntil(reader, JsonToken.EndObject, depth + 1, parse, hot);
                    if (hot) obj = ret;
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    var ret = ReadUntil(reader, JsonToken.EndArray, depth + 1, parse, hot);
                    if (hot) obj = ret;
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
                    if (arrayidx == -2)
                        arrayVals.Add(obj);
                }
                curidx++;
            }

            return arrayidx == -2 ? arrayVals : obj;
        }

        object? ReadJSON(Stream stream, ParseType[] parse)
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
                        obj = ReadUntil(reader, JsonToken.EndArray, 0, parse, true);
                    }
                    else if (reader.TokenType == JsonToken.StartObject)
                    {
                        obj = ReadUntil(reader, JsonToken.EndObject, 0, parse, true);
                    }
                    else
                        throw new Exception();
                }
                catch (Exception e)
                {

                }
                return obj;
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

