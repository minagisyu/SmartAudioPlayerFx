using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;

namespace Quala
{
	/// <summary>
	/// 複数参照されるオブジェクトを参照カウント式で管理
	/// </summary>
	/// <remarks>
	/// アプリケーション全体で利用されるモデルクラス等を一挙に管理
	/// クラス名とキー名でいつでも作成・取得できる
	/// </remarks>
	public sealed class ReferenceManager : IDisposable
	{
		// Type別、Key別、RefCountDisposable>>DisposableWrapper>>T
		ConcurrentDictionary<Type, ConcurrentDictionary<string, RefCountDisposable>> instance =
			new ConcurrentDictionary<Type, ConcurrentDictionary<string, RefCountDisposable>>();
		CompositeDisposable disposables = new CompositeDisposable();

		/// <summary>
		/// objを取得、不要になったらIDisposable.Disposeで破棄(参照カウントが減る)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public IDisposable GetOrCreate<T>(out T obj, string key = "")
			where T : class, new()
		{
			// Type別のコレクションを取得
			var type = typeof(T);
			var typeDic = instance.GetOrAdd(type, (t) => new ConcurrentDictionary<string, RefCountDisposable>());

			// Key名でオブジェクトを取得
			var disposable = typeDic.GetOrAdd(key, (k) => new RefCountDisposable(new DisposableWrapper(new T())));
			obj = (T)((DisposableWrapper)disposable.GetDisposable()).Object;

			// 破棄用のIDisposableを用意
			return Disposable.Create(() => disposable.Dispose());
		}

		public void Dispose()
		{
			disposables.Dispose();
		}

		// 内部保持のオブジェクトがIDisposableならDispose()時にDisposeを呼ぶクラス
		sealed class DisposableWrapper : IDisposable
		{
			public object Object { get; set; }
			public DisposableWrapper(object obj) { Object = obj; }
			public void Dispose() { (Object as IDisposable)?.Dispose(); }
		}

	}

	static class ReferenceManagerExtension
	{
		/// <summary>
		/// 一時利用
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static void Using<T>(this ReferenceManager mgr, Action<T> act, string key = "")
			where T : class, new()
		{
			T obj;
			using (mgr.GetOrCreate<T>(out obj, key))
			{
				act?.Invoke(obj);
			}
		}

	}

}
