using System;
using System.Collections.Concurrent;

namespace Quala
{
	public static partial class AppService
	{
		static ConcurrentDictionary<Type, IDisposable> _disposables =
			new ConcurrentDictionary<Type, IDisposable>();

		static AppService()
		{
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
		}

		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			foreach (var d in _disposables)
			{
				try { d.Value?.Dispose(); }
				catch { }
			}
			_disposables.Clear();
		}

		public static T GetContext<T>()
			where T : class, IDisposable, new()
		{
			IDisposable obj;
			if (_disposables.TryGetValue(typeof(T), out obj) && obj is T)
			{
				return (T)obj;
			}
			else
			{
				T obj2 = new T();
				_disposables[typeof(T)] = obj2;
				return obj2;
			}
		}

		public static Logging Log { get { return GetContext<Logging>(); } }
		public static AppMutex AppMutex { get { return GetContext<AppMutex>(); } }
		public static Storage Storage { get { return GetContext<Storage>(); } }
		public static Preference Preference { get { return GetContext<Preference>(); } }

	}
}
