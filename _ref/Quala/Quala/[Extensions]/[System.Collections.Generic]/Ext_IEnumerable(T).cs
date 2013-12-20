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
			if (sequence == null) yield break;
			if (count == 0) yield break;

			var it = sequence.GetEnumerator();
			if (it == null) yield break;
			var items = new List<T>(count);
			while (true)
			{
				try { if(it.MoveNext() == false) break; }
				catch (NullReferenceException) { break; }
				catch (AggregateException) { break; }

				var i = it.Current;
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

	}
}
