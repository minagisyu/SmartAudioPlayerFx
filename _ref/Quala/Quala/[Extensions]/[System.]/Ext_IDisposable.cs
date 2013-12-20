using System;
using System.Diagnostics;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// nullチェックをするDispose()
		/// </summary>
		/// <param name="disposable"></param>
		public static void SafeDispose(this IDisposable disposable)
		{
			if(disposable != null)
			{
				disposable.Dispose();
			}
		}

		/// <summary>
		/// Using
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="disposable"></param>
		/// <param name="action"></param>
		[DebuggerHidden]
		public static void Using(this IDisposable disposable, Action action)
		{
			using(disposable)
			{
				action();
			}
		}

		/// <summary>
		/// Using
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="disposable"></param>
		/// <param name="action"></param>
		[DebuggerHidden]
		public static void Using<T>(this T disposable, Action<T> action)
			where T : IDisposable
		{
			using(disposable)
			{
				action(disposable);
			}
		}

		/// <summary>
		/// Using
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="disposable"></param>
		/// <param name="action"></param>
		[DebuggerHidden]
		public static R Using<R>(this IDisposable disposable, Func<R> func)
		{
			using(disposable)
			{
				return func();
			}
		}

		/// <summary>
		/// Using
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="disposable"></param>
		/// <param name="action"></param>
		[DebuggerHidden]
		public static R Using<T, R>(this T disposable, Func<T, R> func)
			where T : IDisposable
		{
			using(disposable)
			{
				return func(disposable);
			}
		}
	}
}
