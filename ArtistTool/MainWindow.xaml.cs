using midilib;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using Amazon.Runtime;

namespace ArtistTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ArtistDb ArtistDb { get; set; }
        MidiDb midiDb;
        MbDb mb = new MbDb();
        public MbDb Mb => mb;
        public event PropertyChangedEventHandler? PropertyChanged;
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            //MusicBrainz mb = new MusicBrainz();
            //mb.LoadReleaseFromDatabase(@"C:\Users\shane\Documents\20230603-001001\mbdump\release");
            //mb.LoadArtistsFromDatabase(@"C:\Users\shane\Documents\20230603-001001\mbdump\artist");
            Task.Run(Startup);
        }

        async Task<bool> Startup()
        {
            MidiDb db = new MidiDb();
            await db.InitializeMappings();
            await db.InitSongList(false);
            this.midiDb = db;
            this.ArtistDb = new ArtistDb(db);
            //await sdb.Md5();
            //BuildFromDb();

            this.ArtistDb.Load("Artists.json");
            ArtistDb.BuildSongWords();
            bool doTitles = false;
            if (doTitles)
            {
                mb.LoadTitles();

                var titleWords = mb.TitleWords;
                foreach (var song in this.ArtistDb.Songs)
                {
                    if (song.Words == null)
                        continue;

                    song.Words.RemoveWhere(w => mb.Common100.Contains(w) || char.IsDigit(w[0]));
                    if (song.Words.Count < 2)
                        continue;
                    var allTitles = song.Words.Select((w) => { HashSet<int> outVal = null; titleWords.TryGetValue(w, out outVal); return outVal; }).Where(w => w != null).ToArray();
                    if (allTitles.Length > 1)
                    {
                        HashSet<int> set = new HashSet<int>(allTitles[0]);
                        for (int i = 1; i < allTitles.Length; i++)
                        {
                            set.IntersectWith(allTitles[i]);
                        }
                        if (set.Count > 0)
                        {
                            Debug.WriteLine(song.Name);
                        }
                    }
                    //song.Words
                }
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArtistDb)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mb)));
            int assignedSongs = ArtistDb.Songs.Where(s => s.Artist != null).Count();
            Dispatcher.BeginInvoke(() =>
            {
                StatusTb.Text = $"Assigned = {assignedSongs}.   Unassigned = {ArtistDb.Songs.Count() - assignedSongs}";
            });
            return true;
        }

        void BuildFromDb()
        {
            mb.LoadArtists();
            this.ArtistDb.BuildSongWords();
            var artistWords = mb.ArtistWords;
            HashSet<Artist> artistsHash = new HashSet<Artist>();
            foreach (var songw in this.ArtistDb.Words)
            {
                List<MbArtist> arts;
                if (artistWords.TryGetValue(songw.word, out arts))
                {
                    Artist newartist;
                    foreach (var aaw in arts)
                    {
                        string[] awords = aaw.words;
                        foreach (var song in songw.songs)
                        {
                            if (awords.All(w => song.Words.Contains(w)))
                            {
                                if (!artistsHash.TryGetValue(new Artist() { Name = aaw.Name }, out newartist))
                                {
                                    newartist = new Artist()
                                    {
                                        Name = aaw.Name,
                                    };
                                    artistsHash.Add(newartist);
                                }
                                if (song.Artist == null || song.Artist.Name.Length < newartist.Name.Length)
                                    song.Artist = newartist;
                            }
                        }
                    }
                }

            }

            foreach (var song in ArtistDb.Songs)
            {
                if (song.Artist != null)
                {
                    song.Artist.Songs.Add(song);
                }
            }

            artistsHash.RemoveWhere(a => a.Songs.Count() == 0);
            ArtistDb.BuildSongWords();
            ArtistDb.Artists.AddRange(artistsHash);
            ArtistDb.Save("Artists.json");
        }
        private void searchTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string txt = tb.Text.ToLower();
            Word w = this.ArtistDb.Words.FirstOrDefault(w => w.Text.StartsWith(txt));
            if (w != null)
            {
                WordsLB.SelectedItem = w;
                WordsLB.ScrollIntoView(w);
            }
        }

        private void NewArtistBtn_Click(object sender, RoutedEventArgs e)
        {
            ArtistDb.Artists.Add(new Artist() { Name = "The Various Artists" });
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArtistDb)));
        }

        private void ArtistSearchTb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text.Length > 3)
            {
                ArtistDb.SetArtistFilter(tb.Text);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArtistDb)));
            }
        }

        private void mbArtistSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Text.Length > 3)
            {
                Mb.SetArtistFilter(tb.Text);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mb)));
            }
            else if (tb.Text.Length == 0)
                Mb.SetArtistFilter(tb.Text);
        }

        private void mbVotesFilter_LostFocus(object sender, RoutedEventArgs e)
        {

        }
    }
}
