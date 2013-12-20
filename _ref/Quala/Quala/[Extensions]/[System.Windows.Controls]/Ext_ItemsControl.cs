using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace Quala
{
	// ItemsControlUtil http://pro.art55.jp/?eid=1161413
	partial class Extension
	{
		public static void GoBottom(this ItemsControl itemsControl)
		{
			var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
			panel.SetVerticalOffset(double.PositiveInfinity);
		}

		public static void GoTop(this ItemsControl itemsControl)
		{
			var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
			panel.SetVerticalOffset(0);
		}

		public static void GoRight(this ItemsControl itemsControl)
		{
			var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
			panel.SetHorizontalOffset(double.PositiveInfinity);
		}

		public static void GoLeft(this ItemsControl itemsControl)
		{
			var panel = (VirtualizingStackPanel)itemsControl.FindItemsHostPanel();
			panel.SetHorizontalOffset(0);
		}

		public static Panel FindItemsHostPanel(this ItemsControl itemsControl)
		{
			return FindItemsHostPanel(itemsControl.ItemContainerGenerator, itemsControl);
		}

	}
}
