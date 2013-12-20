using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace Quala.Collections.Specialized
{
	/// <summary>
	/// ObservableCollectionのスレッドセーフ版を目指したクラス
	/// CollectionChangedイベントはDispatcher.BeginInvokeを通じて配信されます。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Obsolete("少し動作が不安定です(CollectionChanged通知時)")]
	public class ConcurrentObservableCollection<T> : IList<T>, INotifyCollectionChanged
	{
		Dispatcher dispatcher;
		ConcurrentDictionary<int, T> collection;
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		ManualResetEventSlim insertlock;

		public ConcurrentObservableCollection(Dispatcher dispatcher = null, IEnumerable<T> items = null)
		{
			this.dispatcher = dispatcher;
			collection = (items != null) ?
				new ConcurrentDictionary<int, T>(items.Select((v, i) => new KeyValuePair<int, T>(i, v))) :
				new ConcurrentDictionary<int, T>();
			insertlock = new ManualResetEventSlim(true);
		}

		public void Add(T item)
		{
			insertlock.Wait();
			while (collection.TryAdd(collection.Count, item) == false) ;
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		public void Clear()
		{
			insertlock.Wait();
			collection.Clear();
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public bool Contains(T item)
		{
			return collection.Any(v => object.Equals(v.Value, item));
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotSupportedException();
		}

		public int Count
		{
			get { return collection.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			insertlock.Wait();
			KeyValuePair<int, T> value;
			try { value = collection.First(v => object.Equals(v.Value, item)); }
			catch (InvalidOperationException) { return false; }

			T tmp;
			var ret = collection.TryRemove(value.Key, out tmp);
			if (ret)
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, tmp, value.Key));

			return ret;
		}

		public IEnumerator<T> GetEnumerator()
		{
			foreach (var i in collection.Select(v => v.Value))
				yield return i;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return ((IEnumerator<T>)this.GetEnumerator());
		}

		public int IndexOf(T item)
		{
			KeyValuePair<int, T> value;
			try { value = collection.First(v => object.Equals(v.Value, item)); }
			catch (InvalidOperationException) { return -1; }
			return value.Key;
		}

		public void Insert(int index, T item)
		{
			// 別スレッドで変更されると困るのでロック
			lock (insertlock)
			{
				insertlock.Reset();
				try
				{
					// キーの移動処理
					if (collection.ContainsKey(index))
					{
						for (var i = collection.Count; i > index; i--)
							collection.AddOrUpdate(i, collection[i - 1], (_, __) => collection[i - 1]);
					}
					collection[index] = item;
				}
				finally { insertlock.Set(); }
			}
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
		}

		public void RemoveAt(int index)
		{
			insertlock.Wait();
			T tmp;
			if(collection.TryRemove(index, out tmp))
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, tmp, index));
		}

		// 存在しないインデックスはdefault(T)が返る仕様
		public T this[int index]
		{
			get
			{
				T ret;
				return collection.TryGetValue(index, out ret) ? ret : default(T);
			}
			set
			{
				insertlock.Wait();
				T old;
				if (collection.TryGetValue(index, out old))
				{
					collection.AddOrUpdate(index, value, (i, v) => value);
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old, index));
				}
			}
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged == null) return;

			if (dispatcher == null ||
				Thread.CurrentThread == dispatcher.Thread)
				CollectionChanged(this, e);
			else
				dispatcher.BeginInvoke(new Action(() => CollectionChanged(this, e)));
		}

	}
}
