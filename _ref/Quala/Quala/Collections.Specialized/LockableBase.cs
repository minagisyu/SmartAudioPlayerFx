using System;
using System.Collections.Generic;
using System.Threading;

namespace Quala.Collections.Specialized
{
	/// <summary>
	/// ロック機構
	/// </summary>
	/// <remarks>
	/// ロックはカウント方式で、多重ロックに対応します。
	/// ロック中のコレクション操作はActionデリゲートに変換されてキューに保存され、
	/// ロックが解除されたときに処理されます。
	/// 
	/// ロック中のキューは、Add等に渡された引数をそのまま
	/// 後で呼び出すようになっているので参照型の場合はちょっと注意。
	/// </remarks>
	public class LockableBase
	{
		int m_lockCount = 0;

		public LockableBase()
		{
			LockingAction = new List<Action>();
		}

		/// <summary>
		/// ロック中かどうか
		/// </summary>
		public bool IsLocked
		{
			get { return (m_lockCount > 0); }
		}

		/// <summary>
		/// ロック解除後の動作をキューイングするためのリスト。
		/// これに追加してください。
		/// </summary>
		protected List<Action> LockingAction { get; private set; }

		/// <summary>
		/// ロックします。
		/// </summary>
		public LockScope Lock()
		{
			return OnLock();
		}

		protected virtual LockScope OnLock()
		{
			Interlocked.Increment(ref m_lockCount);
			return new LockScope(this);
		}

		/// <summary>
		/// ロックの状態を解除します。
		/// 複数回ロックされていた場合は、そのカウント数を減らします。
		/// Lock()で渡されたLockScopeでUnlock出来ない場合のみの利用推奨。
		/// </summary>
		/// <returns>ロックカウント</returns>
		public void Unlock()
		{
			if(IsLocked)
				OnUnlock();
		}

		protected virtual void OnUnlock()
		{
			if(Interlocked.Decrement(ref m_lockCount) == 0)
			{
				lock(LockingAction)
				{
					for(var n = 0; n < LockingAction.Count; n++)
						LockingAction[n]();
					LockingAction.Clear();
				}
			}
		}

		/// <summary>
		/// usingでアンロック出来るようにするためのスコープ
		/// ファイナライザではアンロックされません。
		/// </summary>
		public class LockScope : IDisposable
		{
			LockableBase m_lockable;
			bool disposed = false;

			internal LockScope(LockableBase lockable)
			{
				m_lockable = lockable;
			}

			#region IDisposable

			~LockScope() { Dispose(false); }
			void IDisposable.Dispose() { Dispose(true); }

			#endregion

			void Dispose(bool disposing)
			{
				if(disposed) return;

				if(disposing)
				{
					if(m_lockable != null)
						m_lockable.Unlock();
					GC.SuppressFinalize(this);
				}

				disposed = true;
			}

			/// <summary>
			/// ロックの状態を解除します。
			/// </summary>
			public void Unlock() { Dispose(true); }

			/// <summary>
			/// ロックカウント数を取得
			/// </summary>
			public int LockCount
			{
				get { return (m_lockable != null) ? m_lockable.m_lockCount : 0; }
			}

		}

	}
}
