using Foundation;
using CoreGraphics;
using System;
using UIKit;
using midiplayer;
using NAudio.Wave;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Linq;

namespace PlayerIOS
{
    public partial class ViewController : UIViewController
    {
        MidiPlayer player;
        AVAudioEngineOut aVAudioEngineOut;
        TableSource tableSource;
        public ViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            // Perform any additional setup after loading the view, typically from a nib.
            player = new MidiPlayer();
            Init();
        }

        async Task<bool> Init()
        {
            await player.Initialize(OnEngineCreate);
            player.SetVolume(100);
            searchTextField.EditingChanged += SearchTextField_EditingChanged;
            uiTableView.Source = tableSource = new TableSource(player);
            return true;
        }

        private void SearchTextField_EditingChanged(object sender, EventArgs e)
        {
            player.SearchStr = searchTextField.Text;
            tableSource.Refresh();
            uiTableView.ReloadData();
        }

        public override void DidReceiveMemoryWarning ()
        {
            base.DidReceiveMemoryWarning ();
            // Release any cached data, images, etc that aren't in use.
        }
        void OnEngineCreate(MidiSampleProvider midiSampleProvider)
        {
            aVAudioEngineOut = new AVAudioEngineOut();
            aVAudioEngineOut.Init(midiSampleProvider);
            aVAudioEngineOut.Play();
        }
    }

    public class TableSource : UITableViewSource
    {

        MidiFI[] items;
        MidiPlayer player;
        string CellIdentifier = "TableCell";

        public void Refresh()
        {
            items = player.FilteredMidiFiles.ToArray();            
        }

        public TableSource(MidiPlayer _player)
        {
            player = _player;
            items = player.FilteredMidiFiles.ToArray();
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return items.Length;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier);
            string item = items[indexPath.Row].Name;

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
            player.PlaySong(items[indexPath.Row]);
        }
    }
}
