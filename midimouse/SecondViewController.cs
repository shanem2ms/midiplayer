using System;
using UIKit;
using midilib;
using Foundation;
using System.Linq;

namespace midimouse
{
    public partial class SecondViewController : UIViewController
    {
        MidiPlayer player;
        MidiDb db;
        TimeSpan currentSongTime;
        SynthListTableSource synthListTableSource;
        public MidiPlayer Player
        {
            get => player;
            set
            {
                player = value;
                OnPlayerSet();
            }
        }

        public MidiDb Db
        {
            get => db;
            set
            {
                db = value;
                OnDbSet();
            }
        }
        public SecondViewController (IntPtr handle) : base (handle)
        {
        }

        void OnPlayerSet()
        {
            player.OnChannelEvent += Player_OnChannelEvent;
            player.OnPlaybackTime += Player_OnPlaybackTime;
            player.OnPlaybackStart += Player_OnPlaybackStart;
            player.OnPlaybackComplete += Player_OnPlaybackComplete;
        }

        void SynthSelect(string synth)
        {
            player.ChangeSoundFont(synth).ContinueWith((action) =>
            {
                BeginInvokeOnMainThread(() =>
                {
                    synthName.Text = player.CurrentSoundFont;
                });
            });
        }

        public void OnPlayerInitialized()
        {
            BeginInvokeOnMainThread(() =>
            {
                synthListTableView.Source = synthListTableSource;
                synthListTableView.ReloadData();
                synthName.Text = player.CurrentSoundFont;
            });
        }

        void OnDbSet()
        {
            BeginInvokeOnMainThread(() =>
            {
                synthListTableSource = new SynthListTableSource(db, SynthSelect);
                synthListTableView.Source = synthListTableSource;
                synthListTableView.ReloadData();
                synthName.Text = player.CurrentSoundFont;
            });
        }

        private void Player_OnPlaybackStart(object sender, MidiPlayer.PlaybackStartArgs e)
        {
            currentSongTime = e.timeSpan;
            songLabel.Text = e.file.Name;
            songPosSldier.Value = songPosSldier.MinValue;            
        }

        private void Player_OnPlaybackComplete(object sender, bool e)
        {
        }

        private void Player_OnPlaybackTime(object sender, TimeSpan e)
        {
            BeginInvokeOnMainThread(() =>
            {
                float lerp = (float)(e.TotalMilliseconds / currentSongTime.TotalMilliseconds);
                songPosSldier.Value =
                    songPosSldier.MinValue + (songPosSldier.MaxValue - songPosSldier.MinValue) * lerp;
            });
        }

        private void Player_OnChannelEvent(object sender, MidiPlayer.ChannelEvent e)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
        }

        public override void DidReceiveMemoryWarning ()
        {
            base.DidReceiveMemoryWarning ();
            // Release any cached data, images, etc that aren't in use.
        }
    }

    public class SynthListTableSource : UITableViewSource
    {

        MidiDb db;
        string CellIdentifier = "SynthListCell";
        string[] items;
        public delegate void SynthSelectedDel(string synth);
        SynthSelectedDel SynthSelected;

        public void Refresh()
        {
            items = db.Mappings.soundfonts.Keys.ToArray();
        }

        public SynthListTableSource(MidiDb _db, SynthSelectedDel del)
        {
            db = _db;
            SynthSelected = del;
            items = db.AllSoundFonts.ToArray();
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return items.Length;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier);
            string item = items[indexPath.Row];

            //if there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier);
            }

            cell.TextLabel.Text = item;

            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            SynthSelected(items[indexPath.Row]);
        }
    }

}


