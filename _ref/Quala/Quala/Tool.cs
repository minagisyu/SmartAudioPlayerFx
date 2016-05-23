using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Quala
{
	/// <summary>
	/// 汎用ユーティリティ
	/// </summary>
	public static partial class Tool
	{
		/// <summary>
		/// 管理者権限を持っているか確認します。
		/// </summary>
		/// <returns></returns>
		/// <see cref="http://blogs.msdn.com/tsmatsuz/archive/2007/01/25/windows-vista-uac-part-2.aspx"/>
		public static bool IsAdministratorRole()
		{
			var usrId = WindowsIdentity.GetCurrent();
			var p = new WindowsPrincipal(usrId);
			return p.IsInRole(@"BUILTIN\Administrators");
		}

		/// <summary>
		/// 指定されたパスがディレクトリパスかどうか確認します。
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool? IsDirectoryPath(string path)
		{
			try
			{
				FileAttributes attr = File.GetAttributes(path);
				return ((attr & FileAttributes.Directory) != 0);
			}
			catch(FileNotFoundException) { }
			return null;
		}

		/// <summary>
		/// 管理者権限でアプリケーションを起動。
		/// Vista以前のOSでは失敗します。
		/// </summary>
		/// <returns>
		/// true: 成功、現在のアプリケーションを終了してください。
		/// false: 失敗。
		/// </returns>
		public static bool RestartApplicationWithAdministratorRole()
		{
			if(Environment.OSVersion.Version.Major < 6)
				return false;

			try
			{
				Process.Start(new ProcessStartInfo()
				{
					UseShellExecute = true,
					WorkingDirectory = Environment.CurrentDirectory,
					FileName = Assembly.GetEntryAssembly().Location,
					Verb = "runas",
				});
				return true;
			}
			catch { }
			return false;
		}

		/// <summary>
		/// アプリケーションを開始した実行可能ファイルのディレクトリパスを取得。
		/// </summary>
		public static string StartupPath
		{
			get { return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location); }
		}

		/// <summary>
		/// アプリケーションを開始した実行可能ファイルの
		/// ディレクトリパスにファイル名を追加して返す。
		/// </summary>
		public static string StartupPathWith(string filename)
		{
			return Path.Combine(StartupPath, filename);
		}

		// TODO:
		// 「C:\TEST」などの場所は標準ユーザーでも権限があるはずなのに無いといわれる…。
		public static bool IsGotWriteAccessPermission(string path)
		{
			var rule = GetCurrentAccessRule(path);
			return ((rule != null) && ((rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write));
		}

		// 現在のユーザーが持っている指定パスのFileSystemAccessRuleを得る
		public static FileSystemAccessRule GetCurrentAccessRule(string path)
		{
			var fileSecurity = File.GetAccessControl(path);
			var rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)).OfType<FileSystemAccessRule>();
			var currentIdentity = WindowsIdentity.GetCurrent();
			var sids = new[] { currentIdentity.User }.Concat(currentIdentity.Groups);

			// アクセスルール内にユーザーSIDがある？無ければグループSIDも探す
			return rules.FirstOrDefault(rule => sids.Contains(rule.IdentityReference));
		}
	
	}
}
