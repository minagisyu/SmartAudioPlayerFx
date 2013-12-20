﻿/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.Framework
{
// nunit.framework.dllを巻き込みたくないので別アセンブリにすることを検討中...

// neue cc - テストを簡単にするほんの少しの拡張メソッド
// http://neue.cc/2010/08/02_270.html
// NUnitを使うように修正
public static class Test
{
	// extensions

	/// <summary>IsNull(value)</summary>
	public static void Is<T>(this T value)
	{
		Assert.IsNull(value);
	}

	/// <summary>AreEqual(expected, actual)</summary>
	public static void Is<T>(this T actual, T expected, string message = "")
	{
		Assert.AreEqual(expected, actual, message);
	}

	/// <summary>IsTrue(expected(actual))</summary>
	public static void Is<T>(this T actual, Func<T, bool> expected, string message = "")
	{
		Assert.IsTrue(expected(actual), message);
	}

	/// <summary>AreEqual(expected, actual)</summary>
	public static void Is<T>(this IEnumerable<T> actual, IEnumerable<T> expected, string message = "")
	{
		CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), message);
	}

	/// <summary>AreEqual(expected, actual)</summary>
	public static void Is<T>(this IEnumerable<T> actual, params T[] expected)
	{
		Is(actual, expected.AsEnumerable());
	}

	/// <summary>IsTrue(expected(actual))</summary>
	public static void Is<T>(this IEnumerable<T> actual, IEnumerable<Func<T, bool>> expected)
	{
		actual.Zip(expected, (v, pred) => pred(v))
			.Select((v, i) => new { cond = v, msg = "Index = " + i, })
			.Run(v => Assert.IsTrue(v.cond, v.msg));
	}

	/// <summary>IsTrue(expected(actual))</summary>
	public static void Is<T>(this IEnumerable<T> actual, params Func<T, bool>[] expected)
	{
		Is(actual, expected.AsEnumerable());
	}

	#region テストケース作成用ジェネレータ

	// generator

	public class Case<T1> : IEnumerable<Tuple<T1>>
	{
		List<Tuple<T1>> tuples = new List<Tuple<T1>>();

		public void Add(T1 item1)
		{
			tuples.Add(Tuple.Create(item1));
		}

		public IEnumerator<Tuple<T1>> GetEnumerator() { return tuples.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	public class Case<T1, T2> : IEnumerable<Tuple<T1, T2>>
	{
		List<Tuple<T1, T2>> tuples = new List<Tuple<T1, T2>>();

		public void Add(T1 item1, T2 item2)
		{
			tuples.Add(Tuple.Create(item1, item2));
		}

		public IEnumerator<Tuple<T1, T2>> GetEnumerator() { return tuples.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	public class Case<T1, T2, T3> : IEnumerable<Tuple<T1, T2, T3>>
	{
		List<Tuple<T1, T2, T3>> tuples = new List<Tuple<T1, T2, T3>>();

		public void Add(T1 item1, T2 item2, T3 item3)
		{
			tuples.Add(Tuple.Create(item1, item2, item3));
		}

		public IEnumerator<Tuple<T1, T2, T3>> GetEnumerator() { return tuples.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	public class Case<T1, T2, T3, T4> : IEnumerable<Tuple<T1, T2, T3, T4>>
	{
		List<Tuple<T1, T2, T3, T4>> tuples = new List<Tuple<T1, T2, T3, T4>>();

		public void Add(T1 item1, T2 item2, T3 item3, T4 item4)
		{
			tuples.Add(Tuple.Create(item1, item2, item3, item4));
		}

		public IEnumerator<Tuple<T1, T2, T3, T4>> GetEnumerator() { return tuples.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	public class Case<T1, T2, T3, T4, T5> : IEnumerable<Tuple<T1, T2, T3, T4, T5>>
	{
		List<Tuple<T1, T2, T3, T4, T5>> tuples = new List<Tuple<T1, T2, T3, T4, T5>>();

		public void Add(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
		{
			tuples.Add(Tuple.Create(item1, item2, item3, item4, item5));
		}

		public IEnumerator<Tuple<T1, T2, T3, T4, T5>> GetEnumerator() { return tuples.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	public class Case<T1, T2, T3, T4, T5, T6> : IEnumerable<Tuple<T1, T2, T3, T4, T5, T6>>
	{
		List<Tuple<T1, T2, T3, T4, T5, T6>> tuples = new List<Tuple<T1, T2, T3, T4, T5, T6>>();

		public void Add(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
		{
			tuples.Add(Tuple.Create(item1, item2, item3, item4, item5, item6));
		}

		public IEnumerator<Tuple<T1, T2, T3, T4, T5, T6>> GetEnumerator() { return tuples.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	public class Case<T1, T2, T3, T4, T5, T6, T7> : IEnumerable<Tuple<T1, T2, T3, T4, T5, T6, T7>>
	{
		List<Tuple<T1, T2, T3, T4, T5, T6, T7>> tuples = new List<Tuple<T1, T2, T3, T4, T5, T6, T7>>();

		public void Add(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
		{
			tuples.Add(Tuple.Create(item1, item2, item3, item4, item5, item6, item7));
		}

		public IEnumerator<Tuple<T1, T2, T3, T4, T5, T6, T7>> GetEnumerator() { return tuples.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	#endregion
}
}
*/

#region ** original (test.tt) / T4 Template ******************************
/*
<#@ assembly Name="System.Core.dll" #>
<#@ import namespace="System.Linq" #>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
 
namespace NUnit.Framework
{
    public static class Test
    {
        // extensions
 
        /// <summary>IsNull</summary>
        public static void Is<T>(this T value)
        {
            Assert.IsNull(value);
        }
 
        public static void Is<T>(this T actual, T expected, string message = "")
        {
            Assert.AreEqual(expected, actual, message);
        }
 
        public static void Is<T>(this T actual, Func<T, bool> expected, string message = "")
        {
            Assert.IsTrue(expected(actual), message);
        }
 
        public static void Is<T>(this IEnumerable<T> actual, IEnumerable<T> expected, string message = "")
        {
            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), message);
        }
 
        public static void Is<T>(this IEnumerable<T> actual, params T[] expected)
        {
            Is(actual, expected.AsEnumerable());
        }
 
        public static void Is<T>(this IEnumerable<T> actual, IEnumerable<Func<T, bool>> expected)
        {
            var count = 0;
            foreach (var cond in actual.Zip(expected, (v, pred) => pred(v)))
            {
                Assert.IsTrue(cond, "Index = " + count++);
            }
        }
 
        public static void Is<T>(this IEnumerable<T> actual, params Func<T, bool>[] expected)
        {
            Is(actual, expected.AsEnumerable());
        }
 
        // generator
 
<#
for(var i = 1; i < 8; i++)
{
#>
 
        public class Case<#= MakeT(i) #> : IEnumerable<Tuple<#= MakeT(i) #>>
        {
            List<Tuple<#= MakeT(i) #>> tuples = new List<Tuple<#= MakeT(i) #>>();
 
            public void Add(<#= MakeArgs(i) #>)
            {
                tuples.Add(Tuple.Create(<#= MakeParams(i) #>));
            }
 
            public IEnumerator<Tuple<#= MakeT(i) #>> GetEnumerator() { return tuples.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
<#
}
#>
    }
}
<#+
     string MakeT(int count)
     {
          return "<" + String.Join(", ", Enumerable.Range(1, count).Select(i => "T" + i)) + ">";
     }
 
     string MakeArgs(int count)
     {
          return String.Join(", ", Enumerable.Range(1, count).Select(i => "T" + i + " item" + i));
     }
 
     string MakeParams(int count)
     {
          return String.Join(", ", Enumerable.Range(1, count).Select(i => "item" + i));
     }
#>
*/
#endregion
