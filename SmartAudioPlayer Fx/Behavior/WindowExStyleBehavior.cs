using System;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Quala.Interop.Win32;

namespace SmartAudioPlayerFx.Behavior
{
	/// <summary>
	/// WS_EX_xxxを設定するビヘイビア。初回一括変更のみの手抜き実装
	/// </summary>
	class WindowExStyleBehavior : Behavior<Window>
	{
		public bool NoActivate { get; set; }
		public bool ToolWindow { get; set; }

		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.SourceInitialized += OnSourceInitialized;
		}

		void OnSourceInitialized(object sender, EventArgs e)
		{
			var windowHelper = new WindowInteropHelper(AssociatedObject);

			var exstyle = (WS_EX)API.GetWindowLong(windowHelper.Handle, GWL.EXSTYLE);
			if (NoActivate)
				exstyle |= WS_EX.NOACTIVATE;
			if (ToolWindow)
				exstyle |= WS_EX.TOOLWINDOW;
			API.SetWindowLong(windowHelper.Handle, GWL.EXSTYLE, (IntPtr)exstyle);
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			AssociatedObject.SourceInitialized -= OnSourceInitialized;
		}

	}
}
