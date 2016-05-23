using Quala.Win32;
using System;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;

namespace Quala.WPF.Behavior
{
	/// <summary>
	/// WS_EX_TOOLWINDOWを設定するビヘイビア。
	/// 初回変更のみの手抜き実装。
	/// </summary>
	public class ToolWindowBehavior : Behavior<Window>
	{
		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.SourceInitialized += OnSourceInitialized;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			AssociatedObject.SourceInitialized -= OnSourceInitialized;
		}

		void OnSourceInitialized(object sender, EventArgs e)
		{
			var windowHelper = new WindowInteropHelper(AssociatedObject);

			var exstyle = (WinAPI.WS_EX)WinAPI.GetWindowLong(windowHelper.Handle, WinAPI.GWL.EXSTYLE);
			exstyle |= WinAPI.WS_EX.TOOLWINDOW;
			WinAPI.SetWindowLong(windowHelper.Handle, WinAPI.GWL.EXSTYLE, (IntPtr)exstyle);
		}

	}
}
