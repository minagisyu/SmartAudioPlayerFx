using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Quala.Collections.Specialized
{
	/// <summary>
	/// ロック可能なコレクション
	/// </summary>
	/// <remarks>
	/// ロックはカウント方式で、多重ロックに対応します。
	/// ロック中のコレクション操作はActionデリゲートに変換されてキューに保存され、
	/// ロックが解除されたときに処理されます。
	/// 
	/// ロック中のキューは、Add等に渡された引数をそのまま
	/// 後で呼び出すようになっているので参照型の場合はちょっと注意。
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	public class LockableCollection<T> : LockableBase, IList<T>, INotifyCollectionChanged
	{
		ObservableCollection<T> m_collection = new ObservableCollection<T>();

		public LockableCollection()
		{
			m_collection.CollectionChanged += (s, e) => OnCollectionChanged(e);
		}

		#region IList<T>

		public int IndexOf(T item)
		{
			using(Lock())
			{
				return m_collection.IndexOf(item);
			}
		}

		public void Insert(int index, T item)
		{
			if(IsLocked)
			{
				lock(LockingAction)
				{
					LockingAction.Add(() => Insert(index, item));
				}
			}
			else
			{
				m_collection.Insert(index, item);
			}
		}

		public void RemoveAt(int index)
		{
			if(IsLocked)
			{
				lock(LockingAction)
				{
					LockingAction.Add(() => RemoveAt(index));
				}
			}
			else
			{
				m_collection.RemoveAt(index);
			}
		}

		public T this[int index]
		{
			get
			{
				using(Lock())
				{
					return m_collection[index];
				}
			}
			set
			{
				if(IsLocked)
				{
					lock(LockingAction)
					{
						LockingAction.Add(() => this[index] = value);
					}
				}
				else
				{
					m_collection[index] = value;
				}
			}
		}

		#endregion
		#region ICollection<T>

		public void Add(T item)
		{
			if(IsLocked)
			{
				lock(LockingAction)
				{
					LockingAction.Add(() => Add(item));
				}
			}
			else
			{
				m_collection.Add(item);
			}
		}

		public void Clear()
		{
			if(IsLocked)
			{
				lock(LockingAction)
				{
					LockingAction.Add(() => Clear());
				}
			}
			else
			{
				m_collection.Clear();
			}
		}

		public bool Contains(T item)
		{
			using(Lock())
			{
				return m_collection.Contains(item);
			}
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			using(Lock())
			{
				m_collection.CopyTo(array, arrayIndex);
			}
		}

		public int Count
		{
			get
			{
				using(Lock())
				{
					return m_collection.Count;
				}
			}
		}

		public bool IsReadOnly
		{
			get { return ((ICollection<T>)m_collection).IsReadOnly; }
		}

		/// <summary>
		/// 項目を削除。
		/// ロック中は削除されないためfalseが返ります。
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(T item)
		{
			if(IsLocked)
			{
				lock(LockingAction)
				{
					LockingAction.Add(() => Remove(item));
					return false;
				}
			}
			else
			{
				return m_collection.Remove(item);
			}
		}

		#endregion
		#region IEnumerable<T>

		public IEnumerator<T> GetEnumerator()
		{
			return m_collection.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
		#region ICollectionChanged

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if(CollectionChanged != null)
				CollectionChanged(this, e);
		}

		#endregion
	}
}
