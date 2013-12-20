using System;
using System.Collections.Generic;

namespace Quala
{
	partial class Extension
	{
		// List<>関係
		public static int BinarySearch<T>(this List<T> list, T item, Comparison<T> comparer)
		{
			return list.BinarySearch(item, new BinarySearchComparer<T>(comparer));
		}

		sealed class BinarySearchComparer<T> : IComparer<T>
		{
			Comparison<T> _comparer;

			public BinarySearchComparer(Comparison<T> comparer)
			{
				this._comparer = comparer;
			}

			int IComparer<T>.Compare(T x, T y)
			{
				if(x == null && y == null) return 0;
				if(x != null)
				{
					if(y == null) return -1;
					if(x.Equals(y)) return 0;
					return _comparer(x, y);
				}
				return 1;
			}
		}
	}
}
