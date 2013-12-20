using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// handlerがnull以外ならhandlerを実行します。
		/// Invoke()された場合はtrueが返ります。
		/// </summary>
		/// <param name="handler"></param>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <returns>Invoke()された場合はtrueが返ります。</returns>
		public static bool InvokeOrIgnore(this NotifyCollectionChangedEventHandler handler, object sender, NotifyCollectionChangedEventArgs e)
		{
			if(handler != null)
			{
				handler(sender, e);
				return true;
			}
			return false;
		}

	}
}
