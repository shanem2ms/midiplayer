using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using static midilib.MidiPlayer;
using System.Threading.Tasks;
using System.Xml;
using static midilib.MidiDb;

namespace midilib
{
    public class MidiDb
    {
        public class Fi
        {
            public string Name { get; }

            string nameLower;
            public string NmLwr => nameLower;
            public string Location { get; }
            public Fi(string name) :
                this(name, null)
            {
            }

            public Fi(string name, string location)
            {
                Name = name;
                nameLower = name.ToLower();
                Location = location;
            }
        }
        
        HttpClient httpClient = new HttpClient();
        Fi[] midiFiles; 
        public string searchStr;
        string homedir;
        string midiCacheDir;
        public string HomeDir => homedir;

        public string SearchStr
        {
            get => searchStr;
            set
            {
                searchStr = value;
            }
        }

        public IEnumerable<Fi> FilteredMidiFiles
        {
            get
            {
                if (searchStr != null &
                    searchStr?.Trim().Length > 2)
                {
                    string ssLower = searchStr.ToLower();
                    return midiFiles.Where(fi => fi.NmLwr.Contains(ssLower));
                }
                else
                    return midiFiles;
            }

        }
        public MidiDb()
        {
            homedir = GetHomeDir();
            midiCacheDir = Path.Combine(homedir, "midi");
            if (!Directory.Exists(midiCacheDir))
                Directory.CreateDirectory(midiCacheDir);
        }
        string GetHomeDir()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return documents;
        }

        public async Task<bool> Initialize()
        {
            string mappingsFile = Path.Combine(homedir, "mappings.json");
            if (!File.Exists(mappingsFile))
            {
                var response = await httpClient.GetAsync(MidiPlayer.AwsBucketUrl + "mappings.json");
                Stream inputstream = await response.Content.ReadAsStreamAsync();
                inputstream.Seek(0, SeekOrigin.Begin);
                FileStream fs = File.OpenWrite(mappingsFile);
                inputstream.CopyTo(fs);
                fs.Close();
                fs.Close();
            }
            string jsonstr = await File.ReadAllTextAsync(mappingsFile);
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonstr);
            List<Fi> midFileLsit = new List<Fi>();
            foreach (var kv in result)
            {
                string name = kv.Key;
                string url = kv.Value;
                midFileLsit.Add(new Fi(name, kv.Value));
            }

            this.midiFiles = midFileLsit.ToArray();
            return true;
        }
        public async Task<string> GetLocalFile(Fi mfi)
        {
            string cacheFile = Path.Combine(midiCacheDir, mfi.Location);
            if (!File.Exists(cacheFile))
            {
                string dir = Path.GetDirectoryName(cacheFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var response = await httpClient.GetAsync(MidiPlayer.AwsBucketUrl + mfi.Location);
                Stream inputstream = await response.Content.ReadAsStreamAsync();
                inputstream.Seek(0, SeekOrigin.Begin);
                FileStream fs = File.OpenWrite(cacheFile);
                inputstream.CopyTo(fs);
                fs.Close();
            }

            return cacheFile;
        }


    }
}
