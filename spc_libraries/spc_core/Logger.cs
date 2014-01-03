using System;
using System.Diagnostics;
using System.Reflection;

namespace SmartAudioPlayer
{
	public static class Logger
	{
		static DefaultTraceListener _listener;

		static Logger()
		{
			_listener = new DefaultTraceListener();
		}
		public static void SetLogFileName(string logFileName)
		{
			_listener.LogFileName = logFileName;
		}

		#region AddLog

		static bool _headerWrited = false;
		static void AddLog(LogType logType, string message)
		{
			lock (_listener)
			{
				if (!_headerWrited)
				{
					_headerWrited = true;
					var asm = Assembly.GetEntryAssembly();
					var version = FileVersionInfo.GetVersionInfo(asm.Location);
					_listener.WriteLine(string.Empty);
					_listener.WriteLine("--------------------------------------------------------------------------------");
					_listener.WriteLine(string.Format("{0} ver.{1}", version.FileDescription, version.ProductVersion));
					_listener.WriteLine(string.Empty);
				}
				var log = new Item()
				{
					Time = DateTime.Now,
					Type = logType,
					Source = new StackTrace().GetFrame(2).GetMethod().DeclaringType.Name,
					Message = message,
				};
				_listener.WriteLine(log.ToString());
			}
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

		class Item
		{
			public DateTime Time { get; set; }
			public LogType Type { get; set; }
			public string Source { get; set; }
			public string Message { get; set; }
			public override string ToString()
			{
				return string.Format("{0:G} [{1}] <{2}> : {3}", Time, Type, Source, Message);
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
