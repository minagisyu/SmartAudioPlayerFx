using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Quala
{
	public sealed partial class Logging : IDisposable
	{
		readonly object lockObj = new object();
		FileInfo logfile = null;
		bool headerWrited = false;
		LogType _minLogLevel = LogType.INFO;

		public string LogFilename
		{
			get { return logfile?.FullName; }
			set
			{
				lock (lockObj)
				{
					logfile = new FileInfo(value);
					headerWrited = false;
				}
			}
		}
		public LogType MinLogLevel
		{
			get { return _minLogLevel; }
			set
			{
				lock (lockObj)
				{
					_minLogLevel = value;
				}
			}
		}

		void IDisposable.Dispose() { }

		void Append(Item log)
		{
			if (log.Type < MinLogLevel) return;

			Debugger.Log(0, log.Source, log + Environment.NewLine);

			lock (lockObj)
			{
				if (logfile == null) return;

				using (var stream = logfile.AppendText())
				{
					if (headerWrited == false)
					{
						var asm = Assembly.GetEntryAssembly();
						var version = FileVersionInfo.GetVersionInfo(asm.Location);
						stream.WriteLine();
						stream.WriteLine("--------------------------------------------------------------------------------");
						stream.WriteLine($"{version.FileDescription} ver.{version.ProductVersion}");
						stream.WriteLine();
						headerWrited = true;
					}

					stream.WriteLine(log.ToString());
				}
			}
		}

		#region AddLog

		// カスタムログ
		public void AddLog(LogType logType, string message)
		{
			var log = new Item()
			{
				Time = DateTime.Now,
				Type = logType,
				Source = new StackTrace().GetFrame(2).GetMethod().DeclaringType.Name,
				Message = message,
			};
			Append(log);
		}

		// 情報ログ
		public void AddInfoLog(string format, params object[] args)
			=> AddLog(LogType.INFO, string.Format(format, args));

		// 警告ログ
		public void AddWarningLog(string format, params object[] args)
			=> AddLog(LogType.WARNING, string.Format(format, args));

		// エラーログ
		public void AddErrorLog(string format, params object[] args)
			=> AddLog(LogType.ERROR, string.Format(format, args));

		// エラーログ(例外)
		public void AddErrorLog(string text, Exception ex)
			=> AddLog(LogType.ERROR, text + Environment.NewLine + ex);

		// クリティカルエラーログ
		public void AddCriticalErrorLog(string format, params object[] args)
			=> AddLog(LogType.CRITICAL_ERROR, string.Format(format, args));

		// クリティカルエラーログ(例外)
		public void AddCriticalErrorLog(string text, Exception ex)
			=> AddLog(LogType.CRITICAL_ERROR, text + Environment.NewLine + ex);

		// テストログ
		public void AddTestLog(string format, params object[] args)
			=> AddLog(LogType.TEST, string.Format(format, args));

		// デバッグログ
		public void AddDebugLog(string format, params object[] args)
			=> AddLog(LogType.DEBUG, string.Format(format, args));

		// QualaLibrayデバッグログ
		public void AddLibraryDebugLog(string format, params object[] args)
			=> AddLog(LogType.LBRARY_DEBUG, string.Format(format, args));

		#endregion
		#region AddLogAsync

		// カスタムログ
		public async Task AddLogAsync(LogType logType, string message)
			=> await Task.Run(() => AddLog(logType, message));

		// 情報ログ
		public async Task AddInfoLogAsync(string format, params object[] args)
			=> await Task.Run(() => AddLog(LogType.INFO, string.Format(format, args)));

		// 警告ログ
		public async Task AddWarningLogAsync(string format, params object[] args)
			=> await Task.Run(() => AddLog(LogType.WARNING, string.Format(format, args)));

		// エラーログ
		public async Task AddErrorLogAsync(string format, params object[] args)
			=> await Task.Run(() => AddLog(LogType.ERROR, string.Format(format, args)));

		// エラーログ(例外)
		public async Task AddErrorLogAsync(string text, Exception ex)
			=> await Task.Run(() => AddLog(LogType.ERROR, text + Environment.NewLine + ex));

		// クリティカルエラーログ
		public async Task AddCriticalErrorLogAsync(string format, params object[] args)
			=> await Task.Run(() => AddLog(LogType.CRITICAL_ERROR, string.Format(format, args)));

		// クリティカルエラーログ(例外)
		public async Task AddCriticalErrorLogAsync(string text, Exception ex)
			=> await Task.Run(() => AddLog(LogType.CRITICAL_ERROR, text + Environment.NewLine + ex));

		// テストログ
		public async Task AddTestLogAsync(string format, params object[] args)
			=> await Task.Run(() => AddLog(LogType.TEST, string.Format(format, args)));

		// デバッグログ
		public async Task AddDebugLogAsync(string format, params object[] args)
			=> await Task.Run(() => AddLog(LogType.DEBUG, string.Format(format, args)));

		// QualaLibrayデバッグログ
		public async Task AddLibraryDebugLogAsync(string format, params object[] args)
			=> await Task.Run(() => AddLog(LogType.LBRARY_DEBUG, string.Format(format, args)));

		#endregion

	}
}
