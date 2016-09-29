using System;
using System.Threading;

namespace Quala
{
	public sealed partial class AppMutex : IDisposable
	{
		string name = null;
		Mutex _mutex = null;

		public void Dispose()
		{
			if (_mutex != null)
			{
				lock (_mutex)
				{
					// ReleaseMutex()呼ばないとMutexが残る...
					_mutex.ReleaseMutex();
					_mutex.Dispose();
					_mutex = null;
				}
			}
		}

		public string Name
		{
			get { return name; }
			set
			{
				((IDisposable)this).Dispose();
				name = value;
				_mutex = new Mutex(false, value);
			}
		}

		public bool ExistApplicationInstance()
		{
			if (_mutex == null) return false;
			lock(_mutex)
			{
				if(_mutex.WaitOne(0, false))
				{
					// 新規起動
					return false;
				}
				else
				{
					// すでに起動しているインスタンスがある
					return true;
				}
			}
		}

	}
}
