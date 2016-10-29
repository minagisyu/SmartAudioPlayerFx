using System;
using System.Threading;

namespace Quala
{
	[SingletonService]
	public sealed partial class AppMutexManager : IDisposable
	{
		Mutex _mutex;

		public AppMutexManager(string name)
		{
			_mutex = new Mutex(false, name);
		}

		public void Dispose()
		{
			// ReleaseMutex()呼ばないとMutexが残る...
			_mutex.ReleaseMutex();
			_mutex.Dispose();
			_mutex = null;
		}

		// Mutexを調べることでインスタンス状態をチェック
		public bool ExistApplicationInstance()
		{
			return (_mutex?.WaitOne(0) == false);
		}

	}
}
