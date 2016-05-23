using System;
using System.Collections.Generic;
using System.Concurrency;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

namespace Quala
{
	// ログサービス --- タイプ・ソース・メッセージ・重要度などで構成したログデータを管理
	public static class LogService
	{
		public static int MaxLogs { get; set; }
		static readonly ConcurrentQueue<Item> logs;
		static readonly Subject<Item> newlog_added;
		public static event Action<Item> LogAdded;

		static LogService()
		{
			MaxLogs = 1000;
			logs = new ConcurrentQueue<Item>();
			newlog_added = new Subject<Item>(Scheduler.TaskPool);
			newlog_added.Subscribe(item =>
			{
				Debugger.Log(0, item.Source, item + Environment.NewLine);
				//
				logs.Enqueue(item);
				while (logs.Count > MaxLogs)
				{
					Item tmp;
					logs.TryDequeue(out tmp);
				}
				//
				if (LogAdded != null)
					LogAdded(item);
			});
		}

		#region AddLog

		/// <summary>
		/// ログ追加
		/// StackTraceの処理都合上、AddXXXLog()メソッド以外から呼び出さないでください
		/// </summary>
		/// <param name="logType"></param>
		/// <param name="message"></param>
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
			var log = logs
				.Where(l => l.Type >= minLogLevel)
				.ToArray();
			if (log.Length <= 0) return;

			using (var stream = File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.None))
			using (var writer = new StreamWriter(stream))
			{
				var asm = Assembly.GetEntryAssembly();
				var version = FileVersionInfo.GetVersionInfo(asm.Location);
				writer.WriteLine();
				writer.WriteLine("--------------------------------------------------------------------------------");
				writer.WriteLine("{0} ver.{1}", version.FileDescription, version.ProductVersion);
				writer.WriteLine();
				log.Run(l => writer.WriteLine(l.ToString()));
			}
		}

		public class Item
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

		// ログイベント
		public sealed class LogEventArgs : EventArgs
		{
			public Item Item { get; private set; }
			public LogEventArgs(Item item)
			{
				this.Item = item;
			}
		}
	}
}
