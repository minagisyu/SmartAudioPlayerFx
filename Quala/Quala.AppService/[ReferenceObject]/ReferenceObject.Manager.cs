using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Quala
{
	partial class ReferenceObject
	{
		/// <summary>
		/// ReferenceObjectのインスタンス生成、破棄を参照カウント式で管理する
		/// </summary>
		/// <remarks>
		/// メインメソッド:
		/// GetAsync() / 生成 or 参照カウント＋＋
		/// UsingAsync() / GetAsync, ラムダ式, Disposeのセット
		/// Dispose() / 参照カウント-- or 破棄
		/// 
		/// 必要なパラメータ:
		/// Type(T) / ReferenceObject継承クラス
		/// Key / 複数のTの参照を管理するためのID文字列、null or 空文字可(nullは空文字扱い)
		/// </remarks>
		public sealed class Manager : IDisposable
		{
			// Type別、Key別、AppServiceインスタンス
			ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> instance;
			CompositeDisposable disposables;

			public Manager()
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
}
