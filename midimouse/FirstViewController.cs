using System;
using UIKit;
using midilib;
using Foundation;
using System.Linq;

namespace midimouse
{
    public partial class FirstViewController : UIViewController
    {
        public MidiDb Db { get; set; }
        TableSource tableSource;
        public event EventHandler<MidiDb.Fi> OnSongSelected;
        public FirstViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            Initialize();
        }

        async void Initialize()
        {
            await Db.Initialize();
            uiPlaylistView.Source = tableSource = new TableSource(Db, SongSelected);
            uiPlaylistView.ReloadData();
            uiSearchTextField.EditingChanged += UiSearchTextField_EditingChanged;
        }

        void SongSelected(MidiDb.Fi song)
        {
            OnSongSelected?.Invoke(this, song);
        }

        private void UiSearchTextField_EditingChanged(object sender, EventArgs e)
        {
            Db.SearchStr = uiSearchTextField.Text;
            tableSource.Refresh();
            uiPlaylistView.ReloadData();
        }

        public override void DidReceiveMemoryWarning ()
        {
            base.DidReceiveMemoryWarning ();
            // Release any cached data, images, etc that aren't in use.
        }
    }

    public class TableSource : UITableViewSource
    {

        MidiDb.Fi[] items;
        MidiDb db;
        string CellIdentifier = "TableCell";
        public delegate void SongSelectedDel(MidiDb.Fi song);
        SongSelectedDel SongSelected;

        public void Refresh()
        {
            items = db.FilteredMidiFiles.ToArray();
        }

        public TableSource(MidiDb _db, SongSelectedDel del)
        {
            db = _db;
            SongSelected = del;
            items = db.FilteredMidiFiles.ToArray();
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
            SongSelected(items[indexPath.Row]);
        }
    }
}
