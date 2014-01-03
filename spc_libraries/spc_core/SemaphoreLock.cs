using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartAudioPlayer
{
	// http://uk-taniyama.cocolog-nifty.com/tools/2013/07/winrt-2e1c.html からコピペ
	// RxのAsyncLockと名前がカブるのでSemaphoreLockと改名
	public class SemaphoreLock
	{
		private SemaphoreSlim _semaphore;
		public SemaphoreLock()
		{
			_semaphore = new SemaphoreSlim(1);
		}
		public SemaphoreLock(int initialCount)
		{
			_semaphore = new SemaphoreSlim(initialCount);
		}
		public SemaphoreLock(int initialCount, int maxCount)
		{
			_semaphore = new SemaphoreSlim(initialCount, maxCount);
		}
		public class Locker : IDisposable
		{
			private SemaphoreSlim _semaphore;
			internal Locker(SemaphoreSlim semaphore)
			{
				this._semaphore = semaphore;
			}
			public void Dispose()
			{
				_semaphore.Release();
			}
		}
		public async Task<Locker> LockAsync()
		{
			await _semaphore.WaitAsync();
			return new Locker(_semaphore);
		}
	}
}
