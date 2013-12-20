using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// シーケンスをcountで分割します
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		[DebuggerHidden]
		public static IEnumerable<IEnumerable<T>> TakeWithSplit<T>(this IEnumerable<T> sequence, int count)
		{
			return TakeWithSplit(sequence, count, CancellationToken.None);
		}

		/// <summary>
		/// シーケンスをcountで分割します
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="sequence"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		[DebuggerHidden]
		public static IEnumerable<IEnumerable<T>> TakeWithSplit<T>(this IEnumerable<T> sequence, int count, CancellationToken token)
		{
			var items = new List<T>(count);
			foreach (var i in sequence)
			{
				items.Add(i);
				if ((items.Count % count) == 0)
				{
					yield return items.AsReadOnly();
					items.Clear();
				}
				if (token != null && token.IsCancellationRequested)
					yield break;
			}
			if (items.Count != 0)
				yield return items.AsReadOnly();
		}

	/*	[DebuggerHidden]
		public static void Run<T>(this IEnumerable<T> sequence, Action<T> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");
			foreach (var i in sequence)
				action(i);
		}
	*/
	}
}
