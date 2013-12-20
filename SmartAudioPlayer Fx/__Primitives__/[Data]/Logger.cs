using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Codeplex.Reactive;
using Codeplex.Reactive.Extensions;

namespace __Primitives__
{
	static class Logger
	{
		readonly static CompositeDisposable disposable = new CompositeDisposable();
		readonly static Subject<Item> newlog_added = new Subject<Item>();
		readonly static List<Item> logs = new List<Item>();
		public static ReactiveProperty<int> MaxLogs { get; private set; }

		static Logger()
		{
			MaxLogs = new ReactiveProperty<int>(1000);
			
			newlog_added
				.ObserveOn(Scheduler.TaskPool)
				.Subscribe(x =>
				{
					Debugger.Log(0, x.Source, x + Environment.NewLine);
					lock (logs)
					{
						logs.Add(x);
						var overflow = logs.Count - (MaxLogs.Value + 1);
						if (overflow > 0)
							logs.RemoveRange(MaxLogs.Value, overflow);
					}
				})
				.AddTo(disposable);

			MaxLogs
				.ObserveOn(Scheduler.TaskPool)
				.Subscribe(x =>
				{
					lock (logs)
					{
						var overflow = logs.Count - (x + 1);
						if (overflow > 0)
							logs.RemoveRange(x, overflow);
					}
				})
				.AddTo(disposable);
		}
		#region Dispose

		public static void Dispose()
		{
			disposable.Dispose();
		}
	
		#endregion

		#region AddLog

		static void AddLog(LogType logType, string message)
		{
			var log = new Item()
			{
				Time = DateTime.Now,
				Type = logType,
				Source = new StackTrace().GetFrame(2).GetMethod().DeclaringType.Name,
				Message = message,
			};
			newlog_added.OnNext(log);
		}
		// 情報ログ
		public static void AddInfoLog(string format, params object[] args)
		{
			AddLog(LogType.INFO, string.Format(format, args));
		}
		// 警告ログ
		public static void AddWarningLog(string format, params object[] args)
		{
			AddLog(LogType.WARNING, string.Format(format, args));
		}
		// エラーログ
		public static void AddErrorLog(string format, params object[] args)
		{
			AddLog(LogType.ERROR, string.Format(format, args));
		}
		// エラーログ(例外)
		public static void AddErrorLog(string text, Exception ex)
		{
			AddLog(LogType.ERROR, text + Environment.NewLine + ex);
		}
		// クリティカルエラーログ
		public static void AddCriticalErrorLog(string format, params object[] args)
		{
			AddLog(LogType.CRITICAL_ERROR, string.Format(format, args));
		}
		// クリティカルエラーログ(例外)
		public static void AddCriticalErrorLog(string text, Exception ex)
		{
			AddLog(LogType.CRITICAL_ERROR, text + Environment.NewLine + ex);
		}
		// テストログ
		public static void AddTestLog(string format, params object[] args)
		{
			AddLog(LogType.TEST, string.Format(format, args));
		}
		// デバッグログ
		public static void AddDebugLog(string format, params object[] args)
		{
			AddLog(LogType.DEBUG, string.Format(format, args));
		}

		#endregion

		public static void Save(string filepath, LogType minLogLevel = LogType.INFO)
		{
			List<Item> log;
			lock (logs)
			{
				log = logs
					.Where(l => l.Type >= minLogLevel)
					.ToList();
			}
			if (log.Count <= 0) return;

			using (var stream = File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.None))
			using (var writer = new StreamWriter(stream))
			{
				var asm = Assembly.GetEntryAssembly();
				var version = FileVersionInfo.GetVersionInfo(asm.Location);
				writer.WriteLine();
				writer.WriteLine("--------------------------------------------------------------------------------");
				writer.WriteLine("{0} ver.{1}", version.FileDescription, version.ProductVersion);
				writer.WriteLine();
				log.ForEach(l => writer.WriteLine(l.ToString()));
			}
		}

		public static void Clear()
		{
			lock (logs)
			{
				logs.Clear();
			}
		}

		class Item
		{
			public DateTime Time { get; set; }
			public LogType Type { get; set; }
			public string Source { get; set; }
			public string Message { get; set; }
			public override string ToString()
			{
				return string.Format("[{0:G}][{1}]<{2}> {3}", Time, Type, Source, Message);
			}
		}

		// ログタイプ
		public enum LogType
		{
			INFO = 0,			// 一般情報
			WARNING = 1,		// 警告
			ERROR = 2,			// エラー
			CRITICAL_ERROR = 3,	// 重大なエラー
			TEST = 4,			// テスト
			DEBUG = 5,			// デバッグ
		}

	}
}
