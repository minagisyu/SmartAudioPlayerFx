using System;
using System.Threading;

namespace Quala
{
	public sealed partial class AppMutexManager : IDisposable
	{
		string name = null;
		Mutex _mutex = null;

		public void Dispose()
		{
			if (_mutex == null) return;

			lock (_mutex)
			{
				// ReleaseMutex()呼ばないとMutexが残る...
				_mutex.ReleaseMutex();
				_mutex.Dispose();
				_mutex = null;
			}
		}

		// Nameが設定されるとMutexが生成される
		public string Name
		{
			get { return name; }
			set
			{
				this.Dispose();
				name = value;
				_mutex = new Mutex(false, value);
			}
		}

		// Mutexを調べることでインスタンス状態をチェック
		public bool ExistApplicationInstance()
		{
			if (_mutex == null) return false;
			
			lock(_mutex)
			{
				return (_mutex.WaitOne(0) == false);
			}
		}

	}
}
