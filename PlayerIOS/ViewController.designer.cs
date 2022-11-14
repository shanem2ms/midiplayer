// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace PlayerIOS
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		UIKit.UITextField searchTextField { get; set; }

		[Outlet]
		UIKit.UITableView uiTableView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (uiTableView != null) {
				uiTableView.Dispose ();
				uiTableView = null;
			}

			if (searchTextField != null) {
				searchTextField.Dispose ();
				searchTextField = null;
			}
		}
	}
}
