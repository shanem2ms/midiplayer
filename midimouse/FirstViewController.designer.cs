// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace midimouse
{
	[Register ("FirstViewController")]
	partial class FirstViewController
	{
		[Outlet]
		UIKit.UITableView uiPlaylistView { get; set; }

		[Outlet]
		UIKit.UITextField uiSearchTextField { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (uiSearchTextField != null) {
				uiSearchTextField.Dispose ();
				uiSearchTextField = null;
			}

			if (uiPlaylistView != null) {
				uiPlaylistView.Dispose ();
				uiPlaylistView = null;
			}
		}
	}
}
