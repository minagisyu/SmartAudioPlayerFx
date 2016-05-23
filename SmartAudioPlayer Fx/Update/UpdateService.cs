using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Ionic.Zip;
using Quala;
using SmartAudioPlayerFx.Properties;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx.Update
{
	static partial class UpdateService
	{
		static readonly object UpdateServiceTag = new object();	// BaloonTip用タグ

		// 基本プロパティ群
		public static Uri UpdateInfo { get; set; }
		public static DateTime LastCheckDate { get; private set; }
		public static Version LastCheckVersion { get; private set; }
		public static int CheckIntervalDays { get; private set; }
		public static bool IsAutoUpdateCheckEnabled { get; set; }

		static UpdateService()
		{
			// 初期設定
			UpdateInfo = new Uri("http://update.intre.net/sapfx/update.xml");
			LastCheckDate = DateTime.MinValue;
			LastCheckVersion = Assembly.GetEntryAssembly().GetName().Version;
			CheckIntervalDays = 7;
			IsAutoUpdateCheckEnabled = true;
		}

		public static void Start()
		{
			// タスクトレイ連携
			TasktrayService.BaloonTipClicked += tag =>
			{
				if (tag != UpdateServiceTag) return;
				if (ShowUpdateMessage(UIService.PlayerWindow.WindowHelper.Handle))
					UIService.PlayerWindow.Close();
			};

			// 設定連携
			Preferences.Loaded += LoadPreferences;
			Preferences.Saving += SavePreferences;
			LoadPreferences(null, EventArgs.Empty);

			// 定期チェック
			Observable.Timer(TimeSpan.Zero, TimeSpan.FromDays(1))
				.Subscribe(_=> OnAutoUpdateCheck());
		}

		static void OnAutoUpdateCheck()
		{
			if (IsAutoUpdateCheckEnabled == false) return;
			if (LastCheckDate.AddDays(CheckIntervalDays) >= DateTime.UtcNow) return;

			CheckUpdate(newVersion =>
			{
				if (IsUpdateReady == false) return;
				if (LastCheckVersion >= newVersion) return;

				TasktrayService.ShowBaloonTip(TimeSpan.FromSeconds(10), WinForms.ToolTipIcon.Info,
					"新しいバージョンが利用可能です！",
					"アップデートするにはここをクリックするか、メニューから選択してください。",
					UpdateServiceTag);
			},
			null);
		}

		#region Preferences

		static void LoadPreferences(object sender, EventArgs e)
		{
			Preferences.UpdateSettings
				.GetAttributeValueEx((object)null, _ => UpdateInfo, "UpdateInfoUri")
				.GetAttributeValueEx((object)null, _ => LastCheckVersion)
				.GetAttributeValueEx((object)null, _ => LastCheckDate)
				.GetAttributeValueEx((object)null, _ => CheckIntervalDays, v => v.LimitMin(1))
				.GetAttributeValueEx((object)null, _ => IsAutoUpdateCheckEnabled);
		}
		public static void SavePreferences(object sender, CancelEventArgs e)
		{
			Preferences.UpdateSettings
				.SetAttributeValueEx("UpdateInfoUri", UpdateInfo)
				.SetAttributeValueEx(() => LastCheckVersion)
				.SetAttributeValueEx(() => LastCheckDate)
				.SetAttributeValueEx(() => CheckIntervalDays)
				.SetAttributeValueEx(() => IsAutoUpdateCheckEnabled);
		}

		#endregion

		static XElement last_checked_update_info = null;
		static readonly object check_update_sync = new object();

		/// <summary>
		/// アップデートが使用できる場合はtrue
		/// </summary>
		public static bool IsUpdateReady
		{
			get { return (last_checked_update_info != null); }
		}

		/// <summary>
		/// ShowUpdateMessageが表示中かどうか
		/// </summary>
		public static bool IsShowingUpdateMessage { get; private set; }

		/// <summary>
		/// 更新を確認する。
		/// 現在より新しいバージョンがあればそのバージョン番号を引数にコールバックを呼び出します。
		/// </summary>
		/// <param name="newVersionCallback">新しいバージョンが発見されたときに、そのバージョン番号を引数に呼び出される</param>
		/// <param name="checkComplateCallback">チェック処理が完了したときに呼び出される</param>
		public static void CheckUpdate(Action<Version> newVersionCallback, Action checkComplateCallback)
		{
			LogService.AddDebugLog("Call CheckUpdate");
			Observable.Start(() =>
			{
				if (NetworkInterface.GetIsNetworkAvailable() == false)
				{
					LogService.AddDebugLog(" - Network not available.");
					if (checkComplateCallback != null)
						checkComplateCallback();
					return;
				}

				lock (check_update_sync)
				{
					try { last_checked_update_info = XElement.Load(UpdateInfo.ToString()); }
					catch (Exception e)
					{
						LogService.AddErrorLog(" - update.xml取得中にエラーが発生しました", e);
						if (checkComplateCallback != null)
							checkComplateCallback();
						return;
					}

					// 最終チェック日更新
					LastCheckDate = DateTime.UtcNow;

					var currentVersion = Assembly.GetEntryAssembly().GetName().Version;
					Version newVersion;
					if (Version.TryParse(last_checked_update_info.GetAttributeValueEx("Version", "0.0.0.0"), out newVersion))
					{
						if (currentVersion < newVersion)
						{
							LogService.AddInfoLog(" - 新しいバージョンを確認しました: {0}", newVersion);
							if(newVersionCallback != null)
								newVersionCallback(newVersion);
							if (checkComplateCallback != null)
								checkComplateCallback();
							return;
						}
					}

					last_checked_update_info = null;
					if (checkComplateCallback != null)
						checkComplateCallback();
					return;
				}
			});
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
				// 実行の完了を待つエレガントな方法はないものか・・・
				using (var ev = new ManualResetEventSlim())
				{
					CheckUpdate(null, () => ev.Set());
					ev.Wait();
					if (last_checked_update_info == null) return false;
				}
			}

			var desc = last_checked_update_info.Element("Description");
			var desc_string = (desc != null) ? desc.Value : "(詳細情報無し)";
			var currentVersion = Assembly.GetEntryAssembly().GetName().Version;
			var newVersion = Version.Parse(last_checked_update_info.GetAttributeValueEx("Version", "0.0.0.0"));
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

		// ダウンロードのみ
		static void ToDownload(XElement xml, IWin32Window dialogOwner)
		{
			// ファイルの用意
			using (var dlg = new System.Windows.Forms.SaveFileDialog())
			{
				// get saved filename
				dlg.AddExtension = true;
				dlg.CheckFileExists = false;
				dlg.FileName = xml.GetAttributeValueEx("File", string.Empty);
				dlg.Filter = "zipファイル|*.zip";
				dlg.ValidateNames = true;
				if (dlg.ShowDialog(dialogOwner) != DialogResult.OK)
				{
					return;
				}
				//
				var version = xml.GetAttributeValueEx("Version", string.Empty);
				var filename = xml.GetAttributeValueEx("File", string.Empty);
				var zipFilePath = dlg.FileName;
				var newFilesPath = Path.GetDirectoryName(zipFilePath);
				Directory.CreateDirectory(newFilesPath);
				//
				Action downloadProcess = null;
				downloadProcess = new Action(() =>
				{
					try
					{
						using (var client = new WebClient())
						{
							client.DownloadFile(new Uri(UpdateInfo, "./" + filename), zipFilePath);
						}
					}
					catch (WebException e)
					{
						LogService.AddErrorLog(" - ダウンロード中にエラーが発生しました", e);
						WinForms.MessageBox.Show("ダウンロード中にエラーが発生しました", "SmartAudioPlayer Fx", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
						try { if (File.Exists(zipFilePath)) File.Delete(zipFilePath); }
						catch (IOException) { }
						return;
					}
					if (ZipFile.CheckZip(zipFilePath))
					{
						LogService.AddInfoLog(" - ダウンロードが完了しました");
						WinForms.MessageBox.Show("ダウンロードが完了しました", "SmartAudioPlayer Fx");
					}
					else
					{
						Directory.Delete(zipFilePath, true);
						LogService.AddErrorLog(" - ダウンロードしたファイルが破損していました");
						if (WinForms.MessageBox.Show(
							"ダウンロードしたファイルが破損していました",
							"SmartAudioPlayer Fx",
							WinForms.MessageBoxButtons.RetryCancel,
							WinForms.MessageBoxIcon.Error)
							== DialogResult.Retry)
						{
							if (downloadProcess != null)
								Observable.Start(downloadProcess);
						}
					}
				});
				Observable.Start(downloadProcess);
			}
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
				var version = xml.GetAttributeValueEx("Version", string.Empty);
				var filename = xml.GetAttributeValueEx("File", string.Empty);
				var zipFilePath = Path.Combine(tempdir, filename);
				var newFilesPath = Path.Combine(tempdir, version);
				Directory.CreateDirectory(tempdir);
			ReTry:
				try { client.DownloadFile(new Uri(UpdateInfo, "./" + filename), zipFilePath); }
				catch (WebException e)
				{
					LogService.AddErrorLog(" - ダウンロード中にエラーが発生しました", e);
					WinForms.MessageBox.Show("ダウンロード中にエラーが発生しました", "SmartAudioPlayer Fx", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
					Directory.Delete(tempdir, true);
					return false;
				}
				if (ZipFile.CheckZip(zipFilePath))
				{
					using (var zip = new ZipFile(zipFilePath))
					{
						zip.ExtractAll(Path.Combine(tempdir, version), ExtractExistingFileAction.OverwriteSilently);
					}
					// newVersion.exe --update "oldVersion.exe" で呼び出す -> OnUpdate()へ
					var psi = new ProcessStartInfo()
					{
						Arguments = "--update \"" + Assembly.GetEntryAssembly().Location + "\"",
						FileName = Path.Combine(newFilesPath, "SmartAudioPlayer Fx.exe"),
						Verb = "runas",	// 管理者権限で実行 (一応)
					};
					try { Process.Start(psi); }
					catch (Exception e)
					{
						LogService.AddErrorLog("--updateのプロセス起動に失敗", e);
						if (WinForms.MessageBox.Show(
							"アップデート用プロセスの起動に失敗しました",
							"SmartAudioPlayer Fx",
							WinForms.MessageBoxButtons.RetryCancel,
							WinForms.MessageBoxIcon.Error)
							== DialogResult.Retry)
						{
							goto ReTry;
						}
						Directory.Delete(tempdir, true);
						return false;
					}
					return true;
				}
				else
				{
					Directory.Delete(tempdir, true);
					LogService.AddErrorLog(" - ダウンロードしたファイルが破損していました");
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
			Directory.Delete(tempdir, true);
			return false;
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
					catch (Exception e) { LogService.AddErrorLog("OnUpdate", e); }
				}
				int retrycount = 0;
			retry:
				retrycount++;
				try { File.Copy(i, targetfile, true); }
				catch (Exception e)
				{
					LogService.AddErrorLog("OnUpdate", e);
					Thread.Sleep(300);
					// 10回やってダメなら諦める
					if (retrycount < 10)
						goto retry;
					else
						LogService.AddErrorLog("OnUpdate", "ファイル(" + filename + ")のコピーに失敗しました");
				}
			});

			// 新しいバージョンを起動＆後処理
			// currentAppExeは新しいバージョンに置き換えられている
			// newVersionCopied.exe --post-update "newVersionTemp.exe" で呼び出す -> OnPostUpdate()へ
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
					LogService.AddErrorLog("OnPostUpdate", e);
				}
			}

			LogService.AddInfoLog("UpdateService", " - 更新完了");
			WinForms.MessageBox.Show("アップデートが完了しました", "SmartAudioPlayer Fx", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
		}

	}
}
