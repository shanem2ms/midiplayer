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
	[Register ("SecondViewController")]
	partial class SecondViewController
	{
		[Outlet]
		UIKit.UILabel songLabel { get; set; }

		[Outlet]
		UIKit.UISlider songPosSldier { get; set; }

		[Outlet]
		UIKit.UITableView synthListTableView { get; set; }

		[Outlet]
		UIKit.UILabel synthName { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (synthName != null) {
				synthName.Dispose ();
				synthName = null;
			}

			if (songLabel != null) {
				songLabel.Dispose ();
				songLabel = null;
			}

			if (songPosSldier != null) {
				songPosSldier.Dispose ();
				songPosSldier = null;
			}

			if (synthListTableView != null) {
				synthListTableView.Dispose ();
				synthListTableView = null;
			}
		}
	}
}
