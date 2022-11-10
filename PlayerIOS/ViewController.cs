using Foundation;
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
        UITableView table;
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
            table = new UITableView(View.Bounds); // defaults to Plain style
            string[] tableItems = new string[] { "Vegetables", "Fruits", "Flower Buds", "Legumes", "Bulbs", "Tubers" };           
            table.Source = new TableSource(player);
            Add(table);
            return true;
        }

        public override void DidReceiveMemoryWarning ()
        {
            base.DidReceiveMemoryWarning ();
            // Release any cached data, images, etc that aren't in use.
        }
        void OnEngineCreate(MidiSampleProvider midiSampleProvider)
        {
            AVAudioEngineOut aVAudioEngineOut = new AVAudioEngineOut();
            aVAudioEngineOut.Init(midiSampleProvider);
            aVAudioEngineOut.Play();
        }
    }

    public class TableSource : UITableViewSource
    {

        MidiFI[] items;
        MidiPlayer player;
        string CellIdentifier = "TableCell";

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
