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
                string[] allnames = name.Split(new char[] { ' ', '-', '_' });
                Name = string.Join(' ', allnames);
                nameLower = name.ToLower();
                Location = location;
            }
        }

        public MappingsFile Mappings { get; private set; }
        HttpClient httpClient = new HttpClient();
        Fi[] midiFiles; 
        public string searchStr;
        string homedir;
        string midiCacheDir;
        public string HomeDir => homedir;
        public List<string> AllSoundFonts { get; } = new List<string>();
        public event EventHandler<bool> OnIntialized;

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
            var resp = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, MidiPlayer.AwsBucketUrl + "mappings.json"));
            long contentLen = (long)resp.Content.Headers.ContentLength;
            if (!File.Exists(mappingsFile) || new System.IO.FileInfo(mappingsFile).Length != contentLen)
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
            Mappings = JsonConvert.DeserializeObject<MappingsFile>(jsonstr);
            List<Fi> midFileLsit = new List<Fi>();
            foreach (var kv in Mappings.midifiles)
            {
                string name = kv.Key;
                string url = kv.Value;
                midFileLsit.Add(new Fi(name, kv.Value));
            }

            this.midiFiles = midFileLsit.ToArray();
            this.AllSoundFonts.AddRange(Mappings.soundfonts.Keys);
            OnIntialized?.Invoke(this, true);
            return true;
        }
        public string GetLocalFileSync(Fi mfi, bool allowDownloads = true)
        {
            var task = GetLocalFile(mfi, allowDownloads);
            task.Wait();
            return task.Result;
        }
        
        public async Task<string> GetLocalFile(Fi mfi, bool allowDownloads = true)
        {
            string cacheFile = Path.Combine(midiCacheDir, mfi.Location);
            if (!File.Exists(cacheFile))
            {
                if (!allowDownloads)
                    return null;

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

    public class MappingsFile
    {
        public Dictionary<string, string> midifiles { get; set; }
        public Dictionary<string, string> soundfonts { get; set; }        
    }
}
