using System;
using UIKit;
using midilib;
using Foundation;

namespace midimouse
{
    public partial class SecondViewController : UIViewController
    {
        MidiPlayer player;
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
                synthListTableSource = new SynthListTableSource(player, SynthSelect);
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

        MidiPlayer player;
        string CellIdentifier = "SynthListCell";
        string[] items;
        public delegate void SynthSelectedDel(string synth);
        SynthSelectedDel SynthSelected;

        public void Refresh()
        {
            items = player.AllSoundFonts.ToArray();
        }

        public SynthListTableSource(MidiPlayer _player, SynthSelectedDel del)
        {
            player = _player;
            SynthSelected = del;
            items = player.AllSoundFonts.ToArray();
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


