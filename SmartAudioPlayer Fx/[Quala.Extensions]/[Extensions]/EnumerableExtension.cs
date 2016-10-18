using System;
using System.Collections.Generic;

namespace Quala.Extensions
{
	public static class EnumerableExtension
	{
		// IEnumerable(T) から n 個ずつ要素を取得
		// http://blog.recyclebin.jp/archives/2373

		public static IEnumerable<IEnumerable<TSource>> TakeBy<TSource>(this IEnumerable<TSource> source, int count)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (count < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			using (var enumerator = source.GetEnumerator())
			{
				var values = new TSource[count];
				while (enumerator.MoveNext())
				{
					values[0] = enumerator.Current;
					for (var index = 1; index < count; index++)
					{
						if (!enumerator.MoveNext())
						{
							throw new InvalidOperationException("The number of elements is insufficient.");
						}
						values[index] = enumerator.Current;
					}
					yield return values;
				}
			}
		}

		public static IEnumerable<IEnumerable<TSource>> TakeOrDefaultBy<TSource>(this IEnumerable<TSource> source, int count)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (count < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}
			using (var enumerator = source.GetEnumerator())
			{
				var values = new TSource[count];
				while (enumerator.MoveNext())
				{
					values[0] = enumerator.Current;
					var index = 1;
					for (; index < count; index++)
					{
						if (!enumerator.MoveNext())
						{
							break;
						}
						values[index] = enumerator.Current;
					}
					for (; index < count; index++)
					{
						values[index] = default(TSource);
					}
					yield return values;
				}
			}
		}

		// 無限リピート
		public static IEnumerable<T> Repeat<T>(this IEnumerable<T> source)
		{
			while (true)
			{
				foreach (var x in source)
				{
					yield return x;
				}
			}
		}

		// foreach
		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var x in source)
			{
				action(x);
			}
		}

	}
}
