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

            string[] searchTokens;
            public string[]SearchTokens { get => searchTokens; }
            public Fi(string name) :
                this(name, null)
            {
            }

            public Fi(string name, string location)
            {
                Name = name;
                nameLower = name.ToLower();
                int idx = nameLower.IndexOf('.');
                if (idx > 0)
                    nameLower = nameLower.Substring(0, idx);
                searchTokens = nameLower.Split(new char[] { ' ', '-', '_' });
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
        IEnumerable<Fi> filteredFiles = null;

        Dictionary<string, List<Fi>> searchTree = new Dictionary<string, List<Fi>>();

        public class SoundFontDesc
        {
            public string Name { get; set; }
            public int Length { get; set; }
            public bool IsCached { get; set; }
        }
        public List<SoundFontDesc> AllSoundFonts { get; } = new List<SoundFontDesc>();
        public event EventHandler<bool> OnIntialized;

        public event EventHandler<bool> OnSearchResults;


        public SoundFontDesc SFDescFromName(string sfname)
        {
            return AllSoundFonts.FirstOrDefault(sf => sf.Name == sfname);
        }

        public string SearchStr
        {
            get => searchStr;
            set
            {
                searchStr = value;
                OnSearchStringChanged();
            }
        }

        async void OnSearchStringChanged()
        {
            IEnumerable<Fi> fis = await GetSearchResults(this.searchStr);
            this.filteredFiles = fis;
            OnSearchResults?.Invoke(this, true);
        }

        async Task<IEnumerable<Fi>> GetSearchResults(string searchStr)
        {
            if (searchStr != null &
                searchStr?.Trim().Length > 2)
            {
                string ssLower = searchStr.ToLower().Trim();
                string[] searchTerms = ssLower.Split(new char[] { ' ', '-', '_' });
                HashSet<Fi> fiFinal = null;
                foreach (string term in searchTerms)
                {
                    if (term.Length > 2)
                    {
                        var results = searchTree.Where(kv => kv.Key.Contains(term));
                        HashSet<Fi> fi = results.SelectMany(kv => kv.Value).ToHashSet();
                        if (fiFinal == null)
                            fiFinal = fi;
                        else
                            fiFinal.IntersectWith(fi);
                    }
                }
                return fiFinal;
            }
            return null;
        }

        public IEnumerable<Fi> FilteredMidiFiles
        {
            get
            {
                return filteredFiles != null ? filteredFiles : midiFiles;
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

        void GetAllMidiFiles(List<Fi> allFiles, DirectoryInfo di)
        {
            var dirs = di.GetDirectories();
            foreach (var childDir in dirs)
            {
                GetAllMidiFiles(allFiles, childDir);
            }

            foreach (var mfile in di.GetFiles("*.mid"))
            {
                Fi fi = new Fi(mfile.Name, mfile.FullName);
                allFiles.Add(fi);
            }
        }

        bool IsSoundfontInstalled(string soundFontPath)
        {
            string cacheFile = Path.Combine(this.homedir, soundFontPath);
            return File.Exists(cacheFile);
        }
        public async Task<string> InstallSoundFont(MidiDb.SoundFontDesc sfDesc)
        {
            string cacheFile = Path.Combine(this.homedir, sfDesc.Name);
            if (!File.Exists(cacheFile))
            {
                HttpClient httpClient = new HttpClient();
                var response = await httpClient.GetAsync(MidiPlayer.AwsBucketUrl + "sf/" + sfDesc.Name);
                Stream inputstream = await response.Content.ReadAsStreamAsync();
                inputstream.Seek(0, SeekOrigin.Begin);
                FileStream fs = File.OpenWrite(cacheFile);
                inputstream.CopyTo(fs);
                fs.Close();
            }

            return cacheFile;
        }
        List<Fi> GetMidiFiles(string folder)
        {
            DirectoryInfo di = new DirectoryInfo(folder);
            List<Fi> allFiles = new List<Fi>();
            GetAllMidiFiles(allFiles, di);
            this.midiFiles = allFiles.ToArray();
            return allFiles;
        }

        TaskCompletionSource<bool> IsMappingsInitialized = new TaskCompletionSource<bool>();
        public async Task<bool> InitializeMappings()
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
            this.AllSoundFonts.Clear();
            var sfonts = Mappings.soundfonts.Select(kv => new SoundFontDesc() { Name = kv.Key, Length = 0, IsCached = IsSoundfontInstalled(kv.Key) });
            this.AllSoundFonts.AddRange(sfonts);
            IsMappingsInitialized.SetResult(true);
            return true;
        }

        TaskCompletionSource<bool> isSonglistInitialized = new TaskCompletionSource<bool>();

        public Task<bool> Initialized => isSonglistInitialized.Task;

        public async Task<bool> InitSongList(bool fromCache)
        {
            await IsMappingsInitialized.Task;
            if (fromCache)
            {
                var midFileLsit = GetMidiFiles(this.midiCacheDir);
                this.midiFiles = midFileLsit.ToArray();
            }
            else
            {
                List<Fi> midFileLsit = new List<Fi>();
                foreach (var kv in Mappings.midifiles)
                {
                    string name = kv.Key;
                    string url = kv.Value;
                    midFileLsit.Add(new Fi(name, kv.Value));
                }
                this.midiFiles = midFileLsit.ToArray();
            }
            BuildSearchTree();
            OnIntialized?.Invoke(this, true);
            isSonglistInitialized.SetResult(true);
            return true;
        }

        void BuildSearchTree()
        {
            foreach (Fi fi in midiFiles)
            {
                foreach (string token in fi.SearchTokens)
                {
                    List<Fi> fiList;
                    if (!searchTree.TryGetValue(token, out fiList))
                    {
                        fiList = new List<Fi>();
                        searchTree.Add(token, fiList);
                    }
                    fiList.Add(fi);
                }
            }
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

        public MidiDb.Fi GetSongByName(string name)
        {
            return this.midiFiles.First(fi => fi.Name == name);
        }
    }

    public class MappingsFile
    {
        public Dictionary<string, string> midifiles { get; set; }
        public Dictionary<string, string> soundfonts { get; set; }        
    }
}
