using System;
using System.Collections.Generic;
using System.Threading;
using Quala.Collections.Specialized;

namespace Quala.Threading
{
	public sealed class SinglePumpThread : IDisposable
	{
		Thread thread;
		PriorityQueue<KeyValuePair<Action<AdditionalInfo>, Action>> queue = new PriorityQueue<KeyValuePair<Action<AdditionalInfo>, Action>>();
		ManualResetEvent ev = new ManualResetEvent(false);
		AdditionalInfo info = new AdditionalInfo();

		public SinglePumpThread() : this("SinglePumpThread", ThreadPriority.Lowest) { }
		public SinglePumpThread(string threadName) : this(threadName, ThreadPriority.Lowest) { }
		public SinglePumpThread(ThreadPriority priority) : this("SinglePumpThread", priority) { }
		public SinglePumpThread(string threadName, ThreadPriority priority)
		{
			thread = new Thread(OnThread);
			thread.IsBackground = true;
			thread.Name = threadName;
			thread.Priority = priority;
			thread.Start();
		}

		#region IDisposable

		~SinglePumpThread() { Dispose(false); }
		public void Dispose() { Dispose(true); }

		void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (thread.IsAlive)
				{
					thread.Abort();
				}
				ev.Close();
				GC.SuppressFinalize(this);
			}
		}

		#endregion

		public void Add(Action<AdditionalInfo> action)
		{
			Add(0, action, null);
		}
		public void Add(Action<AdditionalInfo> action, Action onComplate)
		{
			Add(0, action, onComplate);
		}
		public void Add(int priority, Action<AdditionalInfo> action)
		{
			Add(priority, action, null);
		}
		public void Add(int priority, Action<AdditionalInfo> action, Action onComplate)
		{
			lock (queue)
			{
				queue.Enqueue(
					priority,
					new KeyValuePair<Action<AdditionalInfo>,Action>(action, onComplate));
				ev.Set();
			}
		}

		public void ProcessWait()
		{
			ProcessWait(int.MinValue, null, null);
		}
		public void ProcessWait(Action<AdditionalInfo> action)
		{
			ProcessWait(0, action, null);
		}
		public void ProcessWait(Action<AdditionalInfo> action, Action onComplate)
		{
			ProcessWait(0, action, onComplate);
		}
		public void ProcessWait(int priority, Action<AdditionalInfo> action)
		{
			ProcessWait(priority, action, null);
		}
		public void ProcessWait(int priority, Action<AdditionalInfo> action, Action onComplate)
		{
			using (var tmpEv = new ManualResetEvent(false))
			{
				lock (queue)
				{
					queue.Enqueue(
						priority,
						new KeyValuePair<Action<AdditionalInfo>, Action>(
							action,
							() =>
							{
								try
								{
									if (onComplate != null)
										onComplate();
								}
								finally { tmpEv.Set(); }
							}));
					ev.Set();
				}
				tmpEv.WaitOne();
			}
		}

		public T ProcessWait<T>(Func<AdditionalInfo, T> func)
		{
			return ProcessWait<T>(0, func, null);
		}
		public T ProcessWait<T>(Func<AdditionalInfo, T> func, Action onComplate)
		{
			return ProcessWait<T>(0, func, onComplate);
		}
		public T ProcessWait<T>(int priority, Func<AdditionalInfo, T> func)
		{
			return ProcessWait<T>(priority, func, null);
		}
		public T ProcessWait<T>(int priority, Func<AdditionalInfo, T> func, Action onComplate)
		{
			using (var tmpEv = new ManualResetEvent(false))
			{
				T result = default(T);
				lock (queue)
				{
					queue.Enqueue(
						priority,
						new KeyValuePair<Action<AdditionalInfo>, Action>(
							info =>
							{
								result = (func != null) ? func(info) : default(T);
							},
							() =>
							{
								try
								{
									if (onComplate != null)
										onComplate();
								}
								finally { tmpEv.Set(); }
							}));
					ev.Set();
				}
				tmpEv.WaitOne();
				return result;
			}
		}

		public void CurrentCancel()
		{
			info.IsCancelling = false;
		}

		void OnThread()
		{
			try
			{
				while (true)
				{
					if (queue.Count <= 0)
					{
						ev.WaitOne();
						continue;
					}
					// [MEMO]
					// ev.WaitOne()中にキャンセルになったものを解除＆初期化
					info.IsCancelling = false;

					KeyValuePair<Action<AdditionalInfo>, Action> actions;
					lock (queue) { actions = queue.Dequeue(); ev.Reset(); }
					try { actions.Key(info); }
					catch (Exception) { }
					finally
					{
						if (actions.Value != null)
							actions.Value();
					}
				}
			}
			catch (ThreadAbortException) { }
		}

		public class AdditionalInfo
		{
			public bool IsCancelling { get; internal set; }
		}

	}
}
