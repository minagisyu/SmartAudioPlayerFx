using System;
using System.Collections.Generic;

namespace __Primitives__
{
	sealed class CustomEqualityComparer<T> : IEqualityComparer<T>
	{
		readonly Func<T, int> getHashCodeFunc;
		readonly Func<T, T, bool> isEqualFunc;

		public CustomEqualityComparer(Func<T, int> getHashCode, Func<T, T, bool> isEqual)
		{
			this.getHashCodeFunc = getHashCode;
			this.isEqualFunc = isEqual;
		}

		public bool Equals(T x, T y) { return this.isEqualFunc(x, y); }
		public int GetHashCode(T obj) { return this.getHashCodeFunc(obj); }
	}
}
