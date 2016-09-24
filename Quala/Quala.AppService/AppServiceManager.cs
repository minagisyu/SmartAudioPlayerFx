using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Quala
{
	// インスタンス管理
	// AppServiceの作成, 参照カウント管理、インスタンス破棄
	public sealed class AppServiceManager : IDisposable
	{
		// Type別、Key別、AppServiceインスタンス
		ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> instance;
		CompositeDisposable disposables;

		public AppServiceManager()
		{
			instance = new ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>();
			disposables = new CompositeDisposable();
		}

		public async Task<T> GetAsync<T>(string key = "")
			where T : class, IDisposable, new()
		{
			var type = typeof(T);
			ConcurrentDictionary<string, object> typeDic;

			if (instance.TryGetValue(type, out typeDic) == false)
			{
				typeDic = new ConcurrentDictionary<string, object>();
				instance.TryAdd(type, typeDic);

			}

			object obj;
			if (typeDic.TryGetValue(key, out obj) == false)
			{
				obj = await Task.Run(() => new T());
				typeDic.TryAdd(key, obj);
				if (obj is IDisposable)
				{
					// RefCountDisposable
					disposables.Add(obj as IDisposable);
				}
			}

			return obj as T;
		}

		public void Dispose()
		{
			disposables.Dispose();
		}

	}
}
