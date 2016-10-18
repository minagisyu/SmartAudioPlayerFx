using System;
using System.Collections.Generic;

namespace Quala.Extensions
{
	public sealed class CustomEqualityComparer<T>
		: IEqualityComparer<T>
	{
		readonly Func<T, int> _getHashCodeFunc;
		readonly Func<T, T, bool> _isEqualFunc;

		public CustomEqualityComparer(Func<T, int> getHashCode, Func<T, T, bool> isEqual)
		{
			_getHashCodeFunc = getHashCode;
			_isEqualFunc = isEqual;
		}

		public bool Equals(T x, T y) => _isEqualFunc(x, y);
		public int GetHashCode(T obj) => _getHashCodeFunc(obj);
	}
}
