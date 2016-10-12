using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;

namespace Quala
{
	/// <summary>
	/// 複数参照されるオブジェクトを管理
	/// </summary>
	/// <remarks>
	/// アプリケーション全体で利用されるモデルクラス等を一挙に管理
	/// クラス名とキー名でいつでも作成・取得できる
	/// キー名はStringComparer.CurrentCultureIgnoreCaseで比較される
	/// </remarks>
	public sealed class RefCountReferenceManager : IDisposable
	{
		// Type別、Key別、IDisposable
		ConcurrentDictionary<Type, ConcurrentDictionary<string, RefCountDisposable>> instance =
			new ConcurrentDictionary<Type, ConcurrentDictionary<string, RefCountDisposable>>();
		CompositeDisposable disposables = new CompositeDisposable();

		// Type別のコレクションを取得
		ConcurrentDictionary<string, RefCountDisposable> GetTypeDictionary<T>()
		{
			var type = typeof(T);
			var typeDic = instance.GetOrAdd(type, (t) => new ConcurrentDictionary<string, RefCountDisposable>(StringComparer.CurrentCultureIgnoreCase));
			return typeDic;
		}

		/// <summary>
		/// objを取得、存在しない場合は作成
		/// 不要になったらIDisposable.Disposeで破棄(参照カウントが減る)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="disposable"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		T Get<T>(out IDisposable disposable, string key = "")
			where T : class, new()
		{
			// Key名でオブジェクトを取得
			var d = GetTypeDictionary<T>().GetOrAdd(key, (k) =>
			{
				var rd = new RefCountDisposable(new DisposableWrapper(new T()));
				disposables.Add(rd);
				return rd;
			});

			// 返却用オブジェクト
			var t = (T)d.GetDisposable();

			// 破棄用のIDisposableを用意
			disposable = Disposable.Create(() => d.Dispose());

			return t;
		}

		IDisposable Get<T>(out T value, string key = "")
			where T : class, new()
		{
			// Key名でオブジェクトを取得
			var d = GetTypeDictionary<T>().GetOrAdd(key, (k) =>
			{
				var rd = new RefCountDisposable(new DisposableWrapper(new T()));
				disposables.Add(rd);
				return rd;
			});

			// 返却用オブジェクト
			value = (T)d.GetDisposable();

			// 破棄用のIDisposableを用意
			return Disposable.Create(() => d.Dispose());
		}

		/// <summary>
		/// 参照を破棄
		/// </summary>
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

}
