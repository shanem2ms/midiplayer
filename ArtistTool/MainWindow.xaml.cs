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

namespace ArtistTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ArtistDb ArtistDb { get; set; }
        MidiDb midiDb;
        MusicBrainz mb = new MusicBrainz();
        public MusicBrainz Mb => mb;
        public event PropertyChangedEventHandler? PropertyChanged;
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();

            //MusicBrainz mb = new MusicBrainz();
            //mb.LoadReleaseFromDatabase(@"C:\Users\shane\Documents\20230603-001001\mbdump\release");
            //mb.LoadArtistsFromDatabase(@"C:\Users\shane\Documents\20230603-001001\mbdump\artist");
            //mb.LoadFromDatabase(@"C:\Users\shane\Documents\20230603-001001\mbdump\artist");
            Task.Run(Startup);
        }

        async Task<bool> Startup()
        {
            mb.LoadArtists();            

            MidiDb db = new MidiDb();
            await db.InitializeMappings();
            await db.InitSongList(false);
            this.midiDb = db;
            this.ArtistDb = new ArtistDb(db);
            //await sdb.Md5();
            this.ArtistDb.BuildArtists();
            var artistWords = mb.ArtistWords;
            HashSet<Artist> artistsHash = new HashSet<Artist>();
            foreach (var aw in this.ArtistDb.Words)
            {
                List<MbArtist> arts;
                if (artistWords.TryGetValue(aw.word, out arts))
                {
                    Artist newartist;
                    if (arts.Count == 1)
                    {
                        if (!artistsHash.TryGetValue(new Artist() { Name = arts[0].Name }, out newartist))
                        {
                            newartist = new Artist() { Name = arts[0].Name, songs = aw.songs.ToList(),
                                votes = (int)arts[0].Votes };
                            artistsHash.Add(newartist);
                        }
                        else
                        {
                            newartist.songs.AddRange(aw.songs);
                        }
                    }
                }

            }

            ArtistDb.Artists.AddRange(artistsHash);

            ArtistDb.Artists.Sort((a, b) => b.votes.CompareTo(a.votes));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ArtistDb)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Mb)));
            return true;
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
            ArtistDb.Artists.Add(new Artist() { Name = "The Various Artists"});
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
