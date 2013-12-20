using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Quala
{
	// ログサービス --- タイプ・ソース・メッセージ・重要度などで構成したログデータを管理
	public static class LogService
	{
		public static int MaxLogs { get; set; }
		static readonly ConcurrentQueue<Item> logs;
		public static event Action<Item> LogAdded;

		static LogService()
		{
			MaxLogs = 1000;
			logs = new ConcurrentQueue<Item>();
			LogAdded += log => { Debugger.Log(0, log.Source, log + Environment.NewLine); };
		}

		#region AddLog

		// ログ
		static void AddLog(LogType logType, string source, string message)
		{
			var log = new Item() { Time = DateTime.Now, Type = logType, Source = source, Message = message, };
			logs.Enqueue(log);
			Task.Factory.StartNew(() =>
			{
				if (logs.Count > MaxLogs)
				{
					Item i;
					logs.TryDequeue(out i);
				}
			});
			if (log != null && LogAdded != null)
				LogAdded(log);
		}
		// 情報ログ
		public static void AddInfoLog(string source, string format, params object[] args)
		{
			AddLog(LogType.INFO, source, string.Format(format, args));
		}
		// 警告ログ
		public static void AddWarningLog(string source, string format, params object[] args)
		{
			AddLog(LogType.WARNING, source, string.Format(format, args));
		}
		// エラーログ
		public static void AddErrorLog(string source, string format, params object[] args)
		{
			AddLog(LogType.ERROR, source, string.Format(format, args));
		}
		// エラーログ(例外)
		public static void AddErrorLog(string source, string text, Exception ex)
		{
			AddLog(LogType.ERROR, source, text + Environment.NewLine + ex);
		}
		// テストログ
		public static void AddTestLog(string source, string format, params object[] args)
		{
			AddLog(LogType.TEST, source, string.Format(format, args));
		}
		// デバッグログ
		[Conditional("DEBUG")]
		public static void AddDebugLog(string source, string format, params object[] args)
		{
			AddLog(LogType.DEBUG, source, string.Format(format, args));
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
			INFO = 0,		// 一般情報
			WARNING = 1,	// 警告
			ERROR = 2,		// エラー
			TEST = 3,		// テスト
			DEBUG = 4,		// デバッグ
		}

	}
}
