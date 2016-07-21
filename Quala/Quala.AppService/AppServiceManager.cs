using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Quala
{
	// インスタンス管理
	public class AppServiceManager : IDisposable
	{
		ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> instance;
		CompositeDisposable disposables;

		public AppServiceManager()
		{
			instance = new ConcurrentDictionary<Type, ConcurrentDictionary<string, object>>();
			disposables = new CompositeDisposable();
		}

		public T Get<T>(string key = "")
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
			if(typeDic.TryGetValue(key, out obj) == false)
			{
				obj = new T();
				typeDic.TryAdd(key, obj);
				if(obj is IDisposable)
				{
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
