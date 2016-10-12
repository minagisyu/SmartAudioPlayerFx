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
	/// キー名はStringComparer.InvariantCultureIgnoreCaseで比較される
	/// </remarks>
	public sealed class ReferenceManager : IDisposable
	{
		// Type別、Key別、IDisposable
		ConcurrentDictionary<Type, ConcurrentDictionary<string, DisposableWrapper>> instance =
			new ConcurrentDictionary<Type, ConcurrentDictionary<string, DisposableWrapper>>();
		CompositeDisposable disposables = new CompositeDisposable();

		// Type別のコレクションを取得
		ConcurrentDictionary<string, DisposableWrapper> GetTypeDictionary<T>()
		{
			var type = typeof(T);
			var typeDic = instance.GetOrAdd(type, (t) => new ConcurrentDictionary<string, DisposableWrapper>(StringComparer.InvariantCultureIgnoreCase));
			return typeDic;
		}

		/// <summary>
		/// objを取得、存在しない場合は作成
		/// 不要になったらDisposeObjectで破棄処理を呼び出せます
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public T Get<T>(string key = "")
			where T : class, new()
		{
			// Key名でオブジェクトを取得
			var d = GetTypeDictionary<T>().GetOrAdd(key, (k) =>
			{
				var rd = new DisposableWrapper(new T());
				disposables.Add(rd);
				return rd;
			});

			// 返却用オブジェクト
			return (T)d.Object;
		}
		public ReferenceManager Get<T>(Action<T> action)
			where T : class, new()
		{
			return Get<T>("", action);
		}
		public ReferenceManager Get<T>(string key, Action<T> action)
			where T : class, new()
		{
			// Key名でオブジェクトを取得
			var d = GetTypeDictionary<T>().GetOrAdd(key, (k) =>
			{
				var rd = new DisposableWrapper(new T());
				disposables.Add(rd);
				return rd;
			});

			// 返却用オブジェクト
			action?.Invoke((T)d.Object);
			return this;
		}

		public bool DisposeObject<T>(string key = "")
			where T : class, new()
		{
			// Key名でオブジェクトを取得
			DisposableWrapper d;
			if(GetTypeDictionary<T>().TryRemove(key, out d))
			{
				d.Dispose();
				return true;
			}
			return false;
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
