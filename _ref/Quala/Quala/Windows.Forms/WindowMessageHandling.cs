using System;
using System.Windows.Forms;

namespace Quala.Windows.Forms
{
	public delegate void WindowMessageHandler(object sender, ref Message m);

	public sealed class WindowMessageHandling : NativeWindow
	{
		public event WindowMessageHandler WindowMessage;

		protected override void WndProc(ref Message m)
		{
			if(WindowMessage != null && Handle != IntPtr.Zero)
				WindowMessage(this, ref m);

			base.WndProc(ref m);
		}
	}
}
