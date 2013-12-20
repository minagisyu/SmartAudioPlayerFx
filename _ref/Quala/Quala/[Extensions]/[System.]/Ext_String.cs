using System;
using System.Collections.Generic;

namespace Quala
{
	partial class Extension
	{
		// static string.Format()とバッティングしちゃうのでリネーム。
		/// <summary>
		/// String.Format()
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string FormatBy(this string format, params object[] args)
		{
			return string.Format(format, args);
		}

		/// <summary>
		/// String.Format()
		/// </summary>
		/// <param name="format"></param>
		/// <param name="provider"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static string FormatBy(this string format, IFormatProvider provider, params object[] args)
		{
			return string.Format(provider, format, args);
		}

		/// <summary>
		/// stringオブジェクトがnull、もしくは空なのかどうか (空白はEmptyになりません)
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static bool IsNullOrEmpty(this string str)
		{
			return IsNullOrEmpty(str, false);
		}

		/// <summary>
		/// stringオブジェクトがnull、空、空白のみなのかどうか。
		/// 空白には" "(半角スペース)と"　"(全角スペース)が含まれます。
		/// </summary>
		/// <param name="str"></param>
		/// <param name="ignoreWhiteSpace">
		/// 文字列の最初の空白を無視(空扱い)するかどうか (trueは空白を無視することを意味します)
		/// </param>
		/// <returns></returns>
		public static bool IsNullOrEmpty(this string str, bool ignoreWhiteSpace)
		{
			if(str != null)
			{
				if(str.Length > 0)
					foreach(var c in str)
						if((c == ' ' || c == '　') == false)
							return false;
			}
			return true;
		}

		/// <summary>
		/// 文字列が同一かどうかをカルチャを使用して比較します (大文字小文字は区別されます)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool IsEqual(this string a, string b)
		{
			return IsEqual(a, b, false);
		}

		/// <summary>
		/// 文字列が同一かどうかをカルチャを使用して比較します
		/// </summary>
		/// <param name="str"></param>
		/// <param name="target"></param>
		/// <param name="ignoreCase">
		/// 文字の大文字小文字を考慮しないかどうか (trueは大文字小文字の違いを無視することを意味します)
		/// </param>
		/// <returns></returns>
		public static bool IsEqual(this string a, string b, bool ignoreCase)
		{
			return string.Equals(a, b, ignoreCase ?
				StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
		}
	}
}
