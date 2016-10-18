using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Quala.Extensions
{
	// ・変更操作でバージョンが1上がるリスト
	// ・アイテムと操作時のバージョン番号を保持
	// ・任意のバージョンからのアイテムを取得できる
	// ・IListっぽいメソッドは実装するがインターフェイスは実装しない、取得は常にコピーされた配列で行う
	// ・スレッドセーフを目指す
	// ・同じ項目があった場合上書き
	public sealed class VersionedCollection<T>
	{
		ulong _version = 0;
		readonly Dictionary<T, ulong> _items; // <item, version>

		public VersionedCollection() { _items = new Dictionary<T, ulong>(); }
		public VersionedCollection(IEqualityComparer<T> comparer) { _items = new Dictionary<T, ulong>(comparer); }

		public ulong AddOrReplace(T item)
		{
			bool x;
			return AddOrReplace(item, out x);
		}
		public ulong AddOrReplace(T item, out bool isOverrided)
		{
			lock (_items)
			{
				if (_items.ContainsKey(item))
				{
					_items.Remove(item);
					isOverrided = true;
				}
				else
				{
					isOverrided = false;
				}
				_version++;
				_items.Add(item, _version);
			}
			if (isOverrided)
				_notifySubject.OnNext(new NotifyInfo(NotifyType.Update, item));
			else
				_notifySubject.OnNext(new NotifyInfo(NotifyType.Add, item));
			return _version;
		}
		public ulong Remove(T item)
		{
			lock (_items)
			{
				if (_items.ContainsKey(item) == false) return _version;
				_version++;
				_items.Remove(item);
			}
			_notifySubject.OnNext(new NotifyInfo(NotifyType.Remove, item));
			return _version;
		}
		public ulong Clear()
		{
			lock (_items)
			{
				if (_items.Count <= 0) return _version;
				_version++;
				_items.Clear();
			}
			_notifySubject.OnNext(new NotifyInfo(NotifyType.Clear, default(T)));
			return _version;
		}

		public bool Contains(T item) { lock (_items) { return _items.ContainsKey(item); } }

		public Tuple<T[], ulong> Get() { lock (_items) { return Tuple.Create(_items.Keys.ToArray(), _version); } }
		public Tuple<T[], ulong> Get(ulong minVersion) { lock (_items) { return Tuple.Create((from x in _items where (x.Value >= minVersion) select x.Key).ToArray(), _version); } }
		public IEnumerable<T> GetLatest()
		{
			var lastVersion = 0ul;
			do
			{
				var items = Get(lastVersion);
				lastVersion = items.Item2;
				foreach (var x in items.Item1)
				{
					yield return x;
				}
			} while (_version != lastVersion);
		}

		public int Count { get { lock (_items) { return _items.Count; } } }
		public ulong Version { get { lock (_items) { return _version; } } }

		Subject<NotifyInfo> _notifySubject = new Subject<NotifyInfo>();
		public IObservable<NotifyInfo> GetNotifyObservable() { return _notifySubject.AsObservable(); }

		#region define

		public enum NotifyType { Add, Remove, Update, Clear, }

		public struct NotifyInfo
		{
			public NotifyType Type { get; private set; }
			public T Item { get; private set; }

			public NotifyInfo(NotifyType type, T item)
				: this()
			{
				this.Type = type;
				this.Item = item;
			}
		}

		#endregion

	}
}
