using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// ForEach
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		[DebuggerHidden]
		public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
		{
			ForEach<T>(sequence, action, null);
		}

		/// <summary>
		/// ForEach
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <param name="onComplate">例外が発生しても通知します</param>
		[DebuggerHidden]
		public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action, Action onComplate)
		{
			try { foreach (var item in sequence) { action(item); } }
			finally { if (onComplate != null) onComplate(); }
		}

		/// <summary>
		/// ForEach
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="action"></param>
		/// <param name="onComplate">例外が発生しても通知します</param>
		/// <param name="onException"></param>
		[DebuggerHidden]
		public static void ForEach<T, TEx>(this IEnumerable<T> sequence, Action<T> action, Action onComplate, Action<TEx> onException)
			where TEx : Exception
		{
			try { foreach (var item in sequence) { action(item); } }
			catch (TEx ex) { if (onException != null) onException(ex); }
			finally { if (onComplate != null) onComplate(); }
		}

	}
}
