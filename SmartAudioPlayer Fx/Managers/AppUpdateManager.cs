using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Xml.Linq;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Views;
using WinForms = System.Windows.Forms;
using Reactive.Bindings.Extensions;
using System.IO.Compression;
using Quala;
using Quala.Extensions;

namespace SmartAudioPlayerFx.Managers
{
	[Require(typeof(XmlPreferencesManager))]
	[Require(typeof(TaskIconManager))]
	sealed class AppUpdateManager : IDisposable
	{
		#region ctor

		// 基本プロパティ群
		public Uri UpdateInfo { get; set; }
		public DateTime LastCheckDate { get; private set; }
		public Version LastCheckVersion { get; private set; }
		public int CheckIntervalDays { get; private set; }
		public bool IsAutoUpdateCheckEnabled { get; set; }

		readonly object TasktrayTag = new object(); // BaloonTip用タグ
		readonly CompositeDisposable _disposables = new CompositeDisposable();

		public AppUpdateManager()
		{
			// Preferences
			ManagerServices.PreferencesManager.UpdateSettings
				.Subscribe(x => LoadUpdatePreferences(x))
				.AddTo(_disposables);
			ManagerServices.PreferencesManager.SerializeRequestAsObservable()
				.Subscribe(_ => SavePreferences(ManagerServices.PreferencesManager.UpdateSettings.Value))
				.AddTo(_disposables);


			// タスクトレイ連携
			ManagerServices.TaskIconManager.BaloonTipClickedAsObservable()
				.Where(x => x == TasktrayTag)
				.Subscribe(async _ =>
				{
					if (App.Current.MainWindow == null) return;
					if (await ShowUpdateMessageAsync(new WindowInteropHelper(App.Current.MainWindow).EnsureHandle()))
					{
						App.Current.MainWindow.Close();
					}
				})
				.AddTo(_disposables);

			// 定期チェック
			Observable.Timer(TimeSpan.Zero, TimeSpan.FromDays(1))
				.Subscribe(_ => OnAutoUpdateCheck())
				.AddTo(_disposables);
		}
		public void Dispose()
		{
			_disposables.Dispose();
		}

		void LoadUpdatePreferences(XElement element)
		{
			UpdateInfo = element.GetAttributeValueEx("UpdateInfoUri", new Uri("http://update.intre.net/sapfx/update.xml"));
			LastCheckVersion = element.GetAttributeValueEx("LastCheckVersion", Assembly.GetExecutingAssembly().GetName().Version);
			LastCheckDate = element.GetAttributeValueEx("LastCheckDate", DateTime.MinValue);
			CheckIntervalDays = Math.Min(1, element.GetAttributeValueEx("CheckIntervalDays", 7));
			IsAutoUpdateCheckEnabled = element.GetAttributeValueEx("IsAutoUpdateCheckEnabled", true);
		//	ManagerServices.PreferencesManagerJson.UpdateSettings.Value
		}
		void SavePreferences(XElement element)
		{
			element
				.SetAttributeValueEx("UpdateInfoUri", UpdateInfo)
				.SetAttributeValueEx("LastCheckVersion", LastCheckVersion)
				.SetAttributeValueEx("LastCheckDate", LastCheckDate)
				.SetAttributeValueEx("CheckIntervalDays", CheckIntervalDays)
				.SetAttributeValueEx("IsAutoUpdateCheckEnabled", IsAutoUpdateCheckEnabled);
			//
			ManagerServices.PreferencesManagerJson.UpdateSettings
				.SetValueToJson("UpdateInfoUri", UpdateInfo)
				.SetValueToJson("LastCheckVersion", LastCheckVersion)
				.SetValueToJson("LastCheckDate", LastCheckDate)
				.SetValueToJson("CheckIntervalDays", CheckIntervalDays)
				.SetValueToJson("IsAutoUpdateCheckEnabled", IsAutoUpdateCheckEnabled);
		}
		void OnAutoUpdateCheck()
		{
			if (IsAutoUpdateCheckEnabled == false) return;
			if (LastCheckDate.AddDays(CheckIntervalDays) >= DateTime.UtcNow) return;
			CheckUpdate()
				.ToObservable()
				.Where(x=>x!=null)
				.Where(_ => IsUpdateReady)
				.Where(x => x >= LastCheckVersion)
				.ObserveOnUIDispatcher()
				.Subscribe(x =>
				{
					ManagerServices.TaskIconManager.ShowBaloonTip(TimeSpan.FromSeconds(10), WinForms.ToolTipIcon.Info,
						"新しいバージョンが利用可能です！",
						"アップデートするにはここをクリックするか、メニューから選択してください。",
						TasktrayTag);
				});
		}

		#endregion

		XElement last_checked_update_info = null;
		readonly object check_update_sync = new object();

		/// <summary>
		/// アップデートが使用できる場合はtrue
		/// </summary>
		public bool IsUpdateReady
		{
			get { return (last_checked_update_info != null); }
		}

		/// <summary>
		/// ShowUpdateMessageが表示中かどうか
		/// </summary>
		public bool IsShowingUpdateMessage { get; private set; }

		public async Task<Version> CheckUpdate()
		{
			AppService.Log.AddDebugLog("Call CheckUpdate");

			if (!NetworkInterface.GetIsNetworkAvailable())
			{
				AppService.Log.AddDebugLog(" - Network not available.");
				return null;
			}

			return await Task.Run(() =>
			{
				lock (check_update_sync)
				{
#if UPDATE_TEST
					try { last_checked_update_info = XElement.Load("http://update.intre.net/sapfx/update_test.xml"); }
#else
					try { last_checked_update_info = XElement.Load(UpdateInfo.ToString()); }
#endif
					catch (Exception e)
					{
						AppService.Log.AddErrorLog(" - update.xml取得中にエラーが発生しました", e);
						return null;
					}

					// 最終チェック日更新
					LastCheckDate = DateTime.UtcNow;

					var currentVersion = Assembly.GetEntryAssembly().GetName().Version;
					Version newVersion;
					if (Version.TryParse(last_checked_update_info.GetAttributeValueEx("Version", "0.0.0.0"), out newVersion))
					{
						if (currentVersion < newVersion)
						{
							AppService.Log.AddInfoLog(" - 新しいバージョンを確認しました: {0}", newVersion);
							return newVersion;
						}
					}

					last_checked_update_info = null;
					return null;
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
		public async Task<bool> ShowUpdateMessageAsync(IntPtr hWndParent)
		{
			if (hWndParent == IntPtr.Zero) throw new ArgumentException("hWndParent == 0", "hWndParent");
			if (IsShowingUpdateMessage) throw new InvalidOperationException("message showing...");

			if (last_checked_update_info == null)
			{
				CheckUpdate().Wait();	// 実行終了を待つ
				if (last_checked_update_info == null)
					return false;
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
					App.Current.CenterWindow(dlg.Handle, hWndParent);
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
								return await ToUpdateAsync(last_checked_update_info);
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
		void ToDownload(XElement xml, System.Windows.Forms.IWin32Window dialogOwner)
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
					return;
				//
				var filename = xml.GetAttributeValueEx("File", string.Empty);
				var downloadUri = new Uri(UpdateInfo, "./" + filename);
				var zipFilePath = dlg.FileName;
				DownloadAndCheckZipFile(downloadUri, zipFilePath)
					.Where(x => x)
					.Subscribe(x => WinForms.MessageBox.Show("ダウンロードが完了しました", "SmartAudioPlayer Fx"));
			}
		}

		// アップデート準備
		async Task<bool> ToUpdateAsync(XElement xml)
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
			var version = xml.GetAttributeValueEx("Version", string.Empty);
			var filename = xml.GetAttributeValueEx("File", string.Empty);
			var downloadUri = new Uri(UpdateInfo, "./" + filename);
			var zipFilePath = Path.Combine(tempdir, filename);
			var newFilesPath = Path.Combine(tempdir, version);
			Directory.CreateDirectory(tempdir);
			if (await DownloadAndCheckZipFile(downloadUri, zipFilePath) == false)
				return false;	// キャンセルされた

			using(var zip = ZipFile.OpenRead(zipFilePath))
			{
				zip.ExtractToDirectory(Path.Combine(tempdir, version), true);
			}
			// newVersion.exe --update "oldVersion.exe" で呼び出す -> OnUpdate()へ
			var psi = new ProcessStartInfo()
			{
				Arguments = "--update \"" + Assembly.GetEntryAssembly().Location + "\"",
				FileName = Path.Combine(newFilesPath, "SmartAudioPlayer Fx.exe"),
				Verb = "runas",	// 管理者権限で実行 (一応)
			};

		ReTry:
			try { Process.Start(psi); }
			catch (Exception ex)
			{
				AppService.Log.AddErrorLog("--updateのプロセス起動に失敗", ex);
				var result = WinForms.MessageBox.Show(
					"アップデート用プロセスの起動に失敗しました",
					"SmartAudioPlayer Fx",
					WinForms.MessageBoxButtons.RetryCancel,
					WinForms.MessageBoxIcon.Error);
				if (result != DialogResult.Retry)
					Directory.Delete(tempdir, true);
				if (result == DialogResult.Retry)
					goto ReTry;
				else
					return false;
			}
			return true;
		}

		/// <summary>
		/// zipファイルをダウンロードしファイルの破損をチェックします。
		/// エラーが発生した場合リトライするか聞きます。
		/// </summary>
		/// <param name="downloadUri"></param>
		/// <param name="zipFilePath"></param>
		/// <returns>true=正常完了、false=キャンセルされた</returns>
		IObservable<bool> DownloadAndCheckZipFile(Uri downloadUri, string zipFilePath)
		{
			if (downloadUri == null || string.IsNullOrEmpty(downloadUri.AbsolutePath) ||
				string.IsNullOrEmpty(zipFilePath))
				throw new ArgumentException();

			var newFilesPath = Path.GetDirectoryName(zipFilePath);
			Directory.CreateDirectory(newFilesPath);

			// シーケンスのbool?はresult値: true=成功/false=失敗/null=リトライ
			return Observable.Using(
				() => new WebClient(),
				x => Observable.Start<bool?>(() =>
				{
					x.DownloadFile(downloadUri, zipFilePath);
					return true;	// リトライは要らないのでフラグ立てない
				}))
				.Catch<bool?, WebException>(ex =>
				{
					AppService.Log.AddErrorLog(" - ダウンロード中にエラーが発生しました", ex);
					var result = WinForms.MessageBox.Show(
						"ダウンロード中にエラーが発生しました",
						"SmartAudioPlayer Fx",
						WinForms.MessageBoxButtons.RetryCancel,
						WinForms.MessageBoxIcon.Error);
					try { if (File.Exists(zipFilePath)) File.Delete(zipFilePath); }
					catch (IOException) { }
					// リトライフラグを立ててretuen
					// false or nullだとWhereで値が通らないのでRepeatされる
					if (result == DialogResult.Retry)
						return Observable.Return<bool?>(null);
					else
						return Observable.Return<bool?>(false);
				})
				// 破損チェックなし
				/*
				.Where(x =>
				{
					// リトライフラグ(値を持ってない)なら通さない = 次のRepeatで処理繰り返し
					if (x.HasValue == false) return false;
					// ダウンロードが正常終了し、zipファイルをチェックして破損してたら繰り返すか聞く
					if (x.Value == true)
					{
						bool validZip;
						try { validZip = ZipFile.CheckZip(zipFilePath); }
						catch { validZip = false; }
						if (validZip == false)
						{
							Logger.AddErrorLog(" - ダウンロードしたファイルが破損していました");
							var result = WinForms.MessageBox.Show(
								"ダウンロードしたファイルが破損していました", "SmartAudioPlayer Fx",
								WinForms.MessageBoxButtons.RetryCancel, WinForms.MessageBoxIcon.Error);
							try { File.Delete(zipFilePath); }
							catch (IOException) { }
							// リトライなら値を通さない (次でリピート)
							if (result == DialogResult.Retry)
								return false;
							// キャンセルなら例外
							throw new Exception();
						}
					}
					return true; // 通す (正常終了 or キャンセル)
				})*/
				.Catch<bool?, Exception>(_ => Observable.Return<bool?>(false))	// 例外出たら失敗を返す
				.Repeat()
				.Take(1)
				.Select(x => x.Value)
				.Do(x =>
				{
					if (x)
						AppService.Log.AddInfoLog(" - ダウンロードが完了しました ({0} => {1})", downloadUri, zipFilePath);
					else
						AppService.Log.AddInfoLog(" - ダウンロードをキャンセルしました ({0} => {1})", downloadUri, zipFilePath);
				});
		}

		/// <summary>
		/// コマンドライン引数(--update)の処理
		/// (一時フォルダ内の実行ファイルから処理されるように設計されています)
		/// true: update終了、プログラム終了要求
		/// </summary>
		/// <param name="args"></param>
		public bool OnUpdate(string[] args)
		{
			if (args == null || args.Length == 0) return false;
			var currentAppExe = args
				.SkipWhile(i=>!string.Equals(i, "--update", StringComparison.CurrentCultureIgnoreCase))
				.Skip(1)
				.FirstOrDefault();
			if (string.IsNullOrWhiteSpace(currentAppExe))
				return false;

			// update_argが終了するまで待つ
			Process.GetProcessesByName(Path.GetFileName(currentAppExe))
				.ForEach(p => p.WaitForExit());

			// バックアップ＆コピー
			var src_dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			var dest_dir = Path.GetDirectoryName(currentAppExe);
			var old_version_dir = Path.Combine(dest_dir, "previous version");
			if(Directory.Exists(old_version_dir))
				Directory.Delete(old_version_dir, true);

			Directory.CreateDirectory(old_version_dir);
			Directory.EnumerateFiles(src_dir).ForEach(i =>
			{
				var filename = Path.GetFileName(i);
				var targetfile = Path.Combine(dest_dir, filename);
				if (File.Exists(targetfile))
				{
					File.Copy(targetfile, Path.Combine(old_version_dir, filename), true);
					try { File.Delete(targetfile); }
					catch (Exception e) { AppService.Log.AddErrorLog("OnUpdate", e); }
				}
				int retrycount = 0;
			retry:
				retrycount++;
				try { File.Copy(i, targetfile, true); }
				catch (Exception e)
				{
					AppService.Log.AddErrorLog("OnUpdate", e);
					Thread.Sleep(300);
					// 10回やってダメなら諦める
					if (retrycount < 10)
						goto retry;
					else
						AppService.Log.AddErrorLog("OnUpdate", "ファイル(" + filename + ")のコピーに失敗しました");
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
		public void OnPostUpdate(string[] args)
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
				.ForEach(p => p.WaitForExit());

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
					AppService.Log.AddErrorLog("OnPostUpdate", e);
				}
			}

			AppService.Log.AddInfoLog("UpdateService", " - 更新完了");
			WinForms.MessageBox.Show("アップデートが完了しました", "SmartAudioPlayer Fx", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
		}

	}

	public static class ZipArchiveExtensions
	{
		public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
		{
			if (!overwrite)
			{
				archive.ExtractToDirectory(destinationDirectoryName);
				return;
			}
			foreach (ZipArchiveEntry file in archive.Entries)
			{
				string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
				if (file.Name == "")
				{// Assuming Empty for Directory
					Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
					continue;
				}
				file.ExtractToFile(completeFileName, true);
			}
		}
	}

}
