using System;
using System.Runtime.InteropServices;

namespace Quala.Text
{
	/// <summary>
	/// StrCmpLogical APIを使用したExplorer風(数字を特別扱いする)StringComparer
	/// </summary>
	public class LogicalStringComparer : StringComparer
	{
		[DllImport("Shlwapi.dll", EntryPoint = "StrCmpLogicalW", CharSet = CharSet.Unicode)]
		static extern int CompareStringLogical(string x, string y);

		static LogicalStringComparer comparer = null;
		public static LogicalStringComparer Comparer
		{
			get { return comparer ?? (comparer = new LogicalStringComparer()); }
		}

		public override int Compare(string x, string y)
		{
			return CompareStringLogical(x, y);
		}

		public override bool Equals(string x, string y)
		{
			return x.Equals(y);
		}

		public override int GetHashCode(string str)
		{
			return str.GetHashCode();
		}
	}
}
