using System;

namespace Quala
{
	partial class Extension
	{
		public static void InvokeOrIgnore(this EventHandler handler, object sender, EventArgs e)
		{
			if(handler != null)
				handler(sender, e);
		}

		public static void InvokeOrIgnore<TEventArgs>(this EventHandler<TEventArgs> handler, object sender, TEventArgs e)
			where TEventArgs : EventArgs
		{
			if(handler != null)
				handler(sender, e);
		}
	}
}
