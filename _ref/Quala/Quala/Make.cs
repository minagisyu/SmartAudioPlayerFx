using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Quala
{
	/// <summary>
	/// LINQ to Object等のファクトリーメソッド
	/// 一部はQuala.ExtensionsやSystem.Linq.Enumerableと機能がかぶる・・・
	/// アイディアはNyaRuRuさんとこからパク(ry
	/// </summary>
	public static class Make
	{
		/// <summary>
		/// NAME => "hoge" という感じでDictionaryを初期化。
		/// Keyはstring固定です。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="exprs"></param>
		/// <returns></returns>
		/// <see cref="http://d.hatena.ne.jp/NyaRuRu/20071211/p3"/>
		public static Dictionary<string, T> Dictionary<T>(params Expression<Func<Object, T>>[] exprs)
		{
			return exprs.ToDictionary(
				expr => expr.Parameters[0].Name,
				expr => expr.Compile().Invoke(null));
		}

	}
}
