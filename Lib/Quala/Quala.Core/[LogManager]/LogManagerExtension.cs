using System;
using System.Runtime.CompilerServices;

namespace Quala
{
	public static class LogManagerExtension
	{
		// 情報ログ
		public static void AddInfoLog(this LogManager manager, string message,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
			=> manager.AddLog(LogManager.Level.INFO, message, file, line, member);

		// 警告ログ
		public static void AddWarningLog(this LogManager manager, string message,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
			=> manager.AddLog(LogManager.Level.WARNING, message, file, line, member);

		// エラーログ
		public static void AddErrorLog(this LogManager manager, string message,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
			=> manager.AddLog(LogManager.Level.ERROR, message, file, line, member);

		// エラーログ(例外)
		public static void AddErrorLog(this LogManager manager, string text, Exception ex,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
			=> manager.AddLog(LogManager.Level.ERROR, $"{text}{Environment.NewLine}{ex}", file, line, member);

		// クリティカルエラーログ
		public static void AddCriticalErrorLog(this LogManager manager, string message,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
			=> manager.AddLog(LogManager.Level.CRITICAL_ERROR, message, file, line, member);

		// クリティカルエラーログ(例外)
		public static void AddCriticalErrorLog(this LogManager manager, string text, Exception ex,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
			=> manager.AddLog(LogManager.Level.CRITICAL_ERROR, $"{text}{Environment.NewLine}{ex}", file, line, member);

		// テストログ
		public static void AddTestLog(this LogManager manager, string message,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
			=> manager.AddLog(LogManager.Level.TEST, message, file, line, member);

		// デバッグログ
		public static void AddDebugLog(this LogManager manager, string message,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
			=> manager.AddLog(LogManager.Level.DEBUG, message, file, line, member);
	}
}
