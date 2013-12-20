using System;
using System.Concurrency;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Ionic.Zip;
using Quala;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.UI;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx.Update
{
	static partial class UpdateService
	{
		public static Uri UpdateInfo { get; set; }
		public static DateTime LastCheckDate { get; private set; }
		public static Version LastCheckVersion { get; private set; }
		public static int CheckIntervalDays { get; private set; }

		// ShowUpdateMessageが表示中かどうか
		public static bool IsShowingUpdateMessage { get; private set; }

		/// <summary>
		/// アップデートが使用できる場合はtrue
		/// </summary>
		public static bool IsUpdateReady
		{
			get { return (last_checked_update_info != null); }
		}

		static XElement last_checked_update_info = null;

		static UpdateService()
		{
			UpdateInfo = new Uri("http://update.intre.net/sapfx/update.xml");
			LastCheckDate = DateTime.MinValue;
			LastCheckVersion = Assembly.GetEntryAssembly().GetName().Version;
			CheckIntervalDays = 7;

			TasktrayService.BaloonTipClicked += () =>
			{
				if (ShowUpdateMessage(UIService.PlayerWindow.WindowHelper.Handle))
					UIService.PlayerWindow.Close();
			};

			Observable.Timer(TimeSpan.FromHours(24), TimeSpan.FromHours(24))
				.ObserveOn(Scheduler.ThreadPool)
				.Subscribe(_=> OnAutoUpdateCheck());
		}

		static void OnAutoUpdateCheck()
		{
			if (LastCheckDate.AddDays(CheckIntervalDays) < DateTime.UtcNow)
			{
				Task.Factory.StartNew(() =>
				{
					if (CheckUpdate() ?? false)
					{
						var newVersion = Version.Parse(last_checked_update_info.GetOrDefaultValue("Version", "0.0.0.0"));
						if (IsUpdateReady && LastCheckVersion < newVersion)
						{
							TasktrayService.ShowBaloonTip(
								TimeSpan.FromSeconds(10),
								WinForms.ToolTipIcon.Info,
								"新しいバージョンが利用可能です！",
								"アップデートするにはここをクリックするか、メニューから選択してください。");
						}
					}
				});
			}
		}

		#region Preferences

		public static void SavePreferences()
		{
			LogService.AddDebugLog("UpdateService", "Call SavePreferences");
			var elm = PreferenceService.Load("data", "update_settings.xml") ?? new XElement("UpdateSettings");
			if(elm.Name!="UpdateSettings")elm.Name="UpdateSettings";
			//
			elm.SetAttributeValue("UpdateInfoUri", UpdateInfo);
			elm.SetAttributeValue("LastCheckVersion", LastCheckVersion);
			elm.SetAttributeValue("LastCheckDate", LastCheckDate);
			elm.SetAttributeValue("CheckIntervalDays", CheckIntervalDays);
			//
			PreferenceService.Save(elm, "data", "update_settings.xml");
		}

		public static void LoadPreferences()
		{
			LogService.AddDebugLog("UpdateService", "Call LoadPreferences");
			var elm = PreferenceService.Load("data", "update_settings.xml");
			if (elm == null || elm.Name.LocalName != "UpdateSettings") elm = null;
			UpdateInfo = elm.GetOrDefaultValue("UpdateInfoUri", UpdateInfo);
			LastCheckVersion = elm.GetOrDefaultValue("LastCheckVersion", LastCheckVersion);
			LastCheckDate = elm.GetOrDefaultValue("LastCheckDate", LastCheckDate);
			CheckIntervalDays = elm.GetOrDefaultValue("CheckIntervalDays", CheckIntervalDays).LimitMin(1);
			OnAutoUpdateCheck();
		}

		#endregion

		static readonly object check_update_sync = new object();

		/// <summary>
		/// 更新を確認する。
		/// エラー: null
		/// ない: false
		/// あった: true
		/// trueが返ったらShowUpdateMessage()を呼び出せます。
		/// </summary>
		/// <returns></returns>
		public static bool? CheckUpdate()
		{
			LogService.AddDebugLog("UpdateService", "Call CheckUpdate");
			if(NetworkInterface.GetIsNetworkAvailable() == false)
			{
				LogService.AddDebugLog("UpdateService", " - Network not available.");
				return null;
			}

			lock (check_update_sync)
			{
				try { last_checked_update_info = XElement.Load(UpdateInfo.ToString()); }
				catch (WebException e) { LogService.AddErrorLog("UpdateService", " - update.xml取得中にエラーが発生しました", e); }
				catch (SecurityException e) { LogService.AddErrorLog("UpdateService", " - update.xml取得中にエラーが発生しました", e); }
				catch (FileNotFoundException e) { LogService.AddErrorLog("UpdateService", " - update.xml取得中にエラーが発生しました", e); }
				if (last_checked_update_info == null) return null;

				// 最終チェック日更新
				LastCheckDate = DateTime.UtcNow;

				var currentVersion = Assembly.GetEntryAssembly().GetName().Version;
				Version newVersion;
				if (Version.TryParse(last_checked_update_info.GetOrDefaultValue("Version", "0.0.0.0"), out newVersion))
				{
					if (currentVersion < newVersion)
					{
						LogService.AddInfoLog("UpdateService", " - 新しいバージョンを確認しました: {0}", newVersion);
						return true;
					}
				}

				last_checked_update_info = null;
				return false;
			}
		}

		/// <summary>
		/// 更新するかどうかのメッセージダイアログを表示する
		///  - 情報チェックがまだの場合はチェックする
		///  - アップデートが不要の場合は表示せずfalseを返す
		///  - 更新するならtrue (この場合、呼び出し側の責任でアプリケーションを終了すること)
		/// </summary>
		/// <returns></returns>
		public static bool ShowUpdateMessage(IntPtr hWndParent)
		{
			if (hWndParent == IntPtr.Zero)
				throw new ArgumentException("hWndParent == 0", "hWndParent");
			if (IsShowingUpdateMessage)
				throw new InvalidOperationException("message showing...");

			if (last_checked_update_info == null)
			{
				if ((CheckUpdate() ?? false) == false)
					return false;
			}

			var desc = last_checked_update_info.Element("Description");
			var desc_string = (desc != null) ? desc.Value : "(詳細情報無し)";
			var currentVersion = Assembly.GetEntryAssembly().GetName().Version;
			var newVersion = Version.Parse(last_checked_update_info.GetOrDefaultValue("Version", "0.0.0.0"));
			try
			{
				IsShowingUpdateMessage = true;
				using (var dlg = new UpdateMessageBox())
				{
					UIService.CenterWindow(dlg.Handle, hWndParent);
					dlg.TopMost = true;
					dlg.CurrentVersionString = currentVersion.ToString();
					dlg.NewVersionString = newVersion.ToString();
					dlg.UpdateDescription = desc_string.Replace("\r\n", "\n").Replace("\n", "\r\n");	// 改行を\r\nに変更
					var nativeWindow = new WinForms.NativeWindow();
					nativeWindow.AssignHandle(hWndParent);
					try
					{
						switch (dlg.ShowDialog(nativeWindow))
						{
							case WinForms.DialogResult.Yes:
								// Update
								LastCheckVersion = newVersion;
								return ToUpdate(last_checked_update_info);
							case WinForms.DialogResult.No:
								// Download
								LastCheckVersion = newVersion;
								ToDownload(last_checked_update_info, nativeWindow);
								return false;
							case WinForms.DialogResult.Ignore:
								// Skip
								LastCheckVersion = newVersion;
								return false;
							case WinForms.DialogResult.Cancel:
								return false;
						}
					}
					finally { nativeWindow.ReleaseHandle(); }
				}
			}
			finally { IsShowingUpdateMessage = false; }
			return false;
		}

		// アップデート準備
		static bool ToUpdate(XElement xml)
		{
			// 作業用フォルダ作成
			var tempdir = Path.Combine(Path.GetTempPath(), "sap_update_");
			while (true)
			{
				var guid = Guid.NewGuid();
				var tempdirname = tempdir + guid;
				if (Directory.Exists(tempdirname) == false)
				{
					tempdir = tempdirname;
					break;
				}
			}
			// ファイルの用意
			using (var client = new WebClient())
			{
				var version = xml.GetOrDefaultValue("Version", string.Empty);
				var filename = xml.GetOrDefaultValue("File", string.Empty);
				var zipFilePath = Path.Combine(tempdir, filename);
				var newFilesPath = Path.Combine(tempdir, version);
				Directory.CreateDirectory(tempdir);
			ReTry:
				try { client.DownloadFile(new Uri(UpdateInfo, "./" + filename), zipFilePath); }
				catch (WebException e)
				{
					LogService.AddErrorLog("UpdateService", " - ダウンロード中にエラーが発生しました", e);
					WinForms.MessageBox.Show("ダウンロード中にエラーが発生しました", "SmartAudioPlayer Fx", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
					Directory.Delete(tempdir, true);
					return false;
				}
				if (ZipFile.CheckZip(zipFilePath))
				{
					using (var zip = new ZipFile(zipFilePath))
					{
						zip.ExtractAll(Path.Combine(tempdir, version));
					}
					Process.Start(new ProcessStartInfo()
					{
						Arguments = "--update \"" + Assembly.GetEntryAssembly().Location + "\"",
						FileName = Path.Combine(newFilesPath, "SmartAudioPlayer Fx.exe"),
						Verb = "runas",	// 管理者権限で実行 (一応)
					});
					return true;
				}
				else
				{
					Directory.Delete(tempdir, true);
					LogService.AddErrorLog("UpdateService", " - ダウンロードしたファイルが破損していました");
					if (WinForms.MessageBox.Show(
						"ダウンロードしたファイルが破損していました",
						"SmartAudioPlayer Fx",
						WinForms.MessageBoxButtons.RetryCancel,
						WinForms.MessageBoxIcon.Error)
						== DialogResult.Retry)
					{
						goto ReTry;
					}
				}
			}
			return false;
		}

		// ダウンロードのみ
		static void ToDownload(XElement xml, IWin32Window dialogOwner)
		{
			// ファイルの用意
			using (var client = new WebClient())
			using(var dlg = new System.Windows.Forms.SaveFileDialog())
			{
				// get saved filename
				dlg.AddExtension=true;
				dlg.CheckFileExists=false;
				dlg.FileName = xml.GetOrDefaultValue("File", string.Empty);
				dlg.Filter = "zipファイル|*.zip";
				dlg.ValidateNames = true;
				if (dlg.ShowDialog(dialogOwner) != DialogResult.OK)
				{
					return;
				}
				//
				var version = xml.GetOrDefaultValue("Version", string.Empty);
				var filename = xml.GetOrDefaultValue("File", string.Empty);
				var zipFilePath = dlg.FileName;
				var newFilesPath = Path.GetDirectoryName(zipFilePath);
				Directory.CreateDirectory(newFilesPath);
			ReTry:
				try { client.DownloadFile(new Uri(UpdateInfo, "./" + filename), zipFilePath); }
				catch (WebException e)
				{
					LogService.AddErrorLog("UpdateService", " - ダウンロード中にエラーが発生しました", e);
					WinForms.MessageBox.Show("ダウンロード中にエラーが発生しました", "SmartAudioPlayer Fx", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
					try { if(File.Exists(zipFilePath)) File.Delete(zipFilePath); }
					catch (IOException) { }
					return;
				}
				if (ZipFile.CheckZip(zipFilePath) == false)
				{
					Directory.Delete(zipFilePath, true);
					LogService.AddErrorLog("UpdateService", " - ダウンロードしたファイルが破損していました");
					if (WinForms.MessageBox.Show(
						"ダウンロードしたファイルが破損していました",
						"SmartAudioPlayer Fx",
						WinForms.MessageBoxButtons.RetryCancel,
						WinForms.MessageBoxIcon.Error)
						== DialogResult.Retry)
					{
						goto ReTry;
					}
				}
			}
		}

		/// <summary>
		/// コマンドライン引数(--update)の処理
		/// (一時フォルダ内の実行ファイルから処理されるように設計されています)
		/// true: update終了、プログラム終了要求
		/// </summary>
		/// <param name="args"></param>
		public static bool OnUpdate(string[] args)
		{
			if (args == null || args.Length == 0) return false;
			var currentAppExe = args.AsEnumerable()
				.SkipWhile(i=>!string.Equals(i, "--update", StringComparison.CurrentCultureIgnoreCase))
				.Skip(1)
				.FirstOrDefault();
			if (string.IsNullOrWhiteSpace(currentAppExe))
				return false;

			// update_argが終了するまで待つ
			Process.GetProcessesByName(Path.GetFileName(currentAppExe))
				.Run(p => p.WaitForExit());

			// バックアップ＆コピー
			var src_dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			var dest_dir = Path.GetDirectoryName(currentAppExe);
			var old_version_dir = Path.Combine(dest_dir, "previous version");
			if(Directory.Exists(old_version_dir))
				Directory.Delete(old_version_dir, true);
			Directory.CreateDirectory(old_version_dir);
			Directory.EnumerateFiles(src_dir).Run(i =>
			{
				var filename = Path.GetFileName(i);
				var targetfile = Path.Combine(dest_dir, filename);
				if (File.Exists(targetfile))
				{
					File.Copy(targetfile, Path.Combine(old_version_dir, filename), true);
					try { File.Delete(targetfile); }
					catch (Exception e) { LogService.AddErrorLog("UpdateService", "OnUpdate", e); }
				}
				int retrycount = 0;
			retry:
				retrycount++;
				try { File.Copy(i, targetfile, true); }
				catch (Exception e)
				{
					LogService.AddErrorLog("UpdateService", "OnUpdate", e);
					Thread.Sleep(300);
					// 10かいやってダメなら諦める
					if (retrycount < 10)
						goto retry;
					else
						LogService.AddErrorLog("UpdateService", "OnUpdate", "ファイル(" + filename + ")のコピーに失敗しました");
				}
			});

			// 新しいバージョンを起動＆後処理
			Process.Start(new ProcessStartInfo()
			{
				Arguments = "--post-update \"" + Assembly.GetEntryAssembly().Location + "\"",
				FileName = currentAppExe,
			});
			return true;
		}

		/// <summary>
		/// コマンドライン引数(--post-update)の処理
		/// (新しい実行ファイルから処理されるように設計されています)
		/// </summary>
		/// <param name="args"></param>
		public static void OnPostUpdate(string[] args)
		{
			if (args == null || args.Length == 0) return;
			var tempAppExe = args.AsEnumerable()
				.SkipWhile(i => !string.Equals(i, "--post-update", StringComparison.CurrentCultureIgnoreCase))
				.Skip(1)
				.FirstOrDefault();
			if (string.IsNullOrWhiteSpace(tempAppExe))
				return;

			// update_argが終了するまで待つ
			Process.GetProcessesByName(Path.GetFileName(tempAppExe))
				.Run(p => p.WaitForExit());

			// 一時ファイル削除 (一応簡易チェックする)
			var tempDir = Path.GetDirectoryName(Path.GetDirectoryName(tempAppExe));
			if (tempDir.StartsWith(Path.Combine(Path.GetTempPath(), "sap_update_"), StringComparison.CurrentCultureIgnoreCase))
			{
				try
				{
					if (Directory.Exists(tempDir))
						Directory.Delete(tempDir, true);
				}
				catch (Exception e)
				{
					LogService.AddErrorLog("UpdateService", "OnPostUpdate", e);
				}
			}

			LogService.AddInfoLog("UpdateService", " - 更新完了");
			WinForms.MessageBox.Show("アップデートが正常に完了しました", "SmartAudioPlayer Fx", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
		}

	}
}
