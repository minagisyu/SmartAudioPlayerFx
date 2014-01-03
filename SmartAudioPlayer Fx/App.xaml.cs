using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Codeplex.Reactive;
using Codeplex.Reactive.Extensions;
using SmartAudioPlayer;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;
using SmartAudioPlayerFx.Views;
using SmartAudioPlayerFx.Views.Options;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);
			WinForms.Application.DoEvents();
			UIDispatcherScheduler.Initialize();
#if !DEBUG
			// Exception Handling
			AppDomain.CurrentDomain.UnhandledException += (_, x) =>
				ShowExceptionMessage(x.ExceptionObject as Exception);
			this.DispatcherUnhandledException += (_, x) =>
				ShowExceptionMessage(x.Exception);
#endif
			var logFilename = Preferences.CreateFullPath("SmartAudioPlayer Fx.log");
			Logger.SetLogFileName(logFilename);

			// アップデートチェック
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (HandleUpdateProcess(e.Args))
				return;

			// 多重起動の抑制
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (CheckApplicationInstance())
				return;

			// Services Initialize
			var dbFilename = Preferences.CreateFullPath("data", "media.db");
			ManagerServices.Initialize(dbFilename);
			Exit += delegate { ManagerServices.Dispose(); };

			// WindowShow, SetTrayIconMenus
			var window = new Views.MainWindow();
			this.MainWindow = window;
			window.Show();
			App.Current.SessionEnding += delegate { window.Close(); };	// LogOff
			ManagerServices.TaskIconManager.SetMenuItems(() => CreateWinFormsMenuItems(window));

			// 定期保存
			Observable.Timer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5))
				.ObserveOnUIDispatcher()
				.Subscribe(_ => ManagerServices.PreferencesManager.Save());
		}

		bool HandleUpdateProcess(string[] args)
		{
			AppUpdateManager.OnPostUpdate(args);
			if (AppUpdateManager.OnUpdate(args))
			{
				Shutdown(-2);
				return true;
			}
			return false;
		}
		bool CheckApplicationInstance()
		{
#if !DEBUG
			var mutex = new Mutex(false, "SmartAudioPlayer Fx");
			if (mutex.WaitOne(0, false))
			{
				// 新規起動
				this.Exit += delegate
				{
					// ReleaseMutex()呼ばないとMutexが残る...
					mutex.ReleaseMutex();
					mutex.Dispose();
				};
				return false;
			}
			else
			{
				// すでに起動しているインスタンスがある
				Logger.AddInfoLog("多重起動を確認しました。");
				WinForms.MessageBox.Show(
					"多重起動は出来ません。アプリケーションを終了します。",
					"SmartAudioPlayer Fx");
				mutex.Dispose();
				Shutdown(-1);
				return true;
			}
#else
			return false;
#endif
		}

		static void ShowExceptionMessage(Exception ex)
		{
			// todo: 専用のダイアログ使う？
			Logger.AddCriticalErrorLog("UnhandledException", ex);
			var message = string.Format(
				"未処理の例外エラーが発生しました{0}" +
				"----------------------------------------{0}" +
				"{1}",
				Environment.NewLine,
				ex);
			using (var dlg = new MessageDialog())
			{
				dlg.Title = "SmartAudioPlayer Fx";
				dlg.HeaderMessage = "未処理の例外エラーが発生しました";
				dlg.DescriptionMessage = ex.ToString();
				dlg.ShowDialog();
			}
		}

		#region ContextMenu

		// Tasktray & PlayerWindowで使う
		public static WinForms.MenuItem[] CreateWinFormsMenuItems(MainWindow window)
		{
			var items = CreateMenuItems(window);
			return ConvertToWinFormsMenuItems(items).ToArray();
		}
		static IEnumerable<MenuItemDefinition> CreateMenuItems(MainWindow window)
		{
			var is_videodraw = window.MediaListWindow.ViewModel.IsVideoDrawing.Value;
			var is_repeat = ManagerServices.JukeboxManager.IsRepeat.Value;
			var is_random = (ManagerServices.JukeboxManager.SelectMode.Value == JukeboxManager.SelectionMode.Random);
			var is_sequential = (ManagerServices.JukeboxManager.SelectMode.Value == JukeboxManager.SelectionMode.Filename);
			yield return new MenuItemDefinition("再生モード", subitems: new[]
			{
				new MenuItemDefinition("リピート", is_repeat, () => ManagerServices.JukeboxManager.IsRepeat.Value=!is_repeat),
				new MenuItemDefinition("-"),
				new MenuItemDefinition("ランダム", is_random, () => ManagerServices.JukeboxManager.SelectMode.Value = JukeboxManager.SelectionMode.Random),
				new MenuItemDefinition("ファイル名順", is_sequential, () => ManagerServices.JukeboxManager.SelectMode.Value=JukeboxManager.SelectionMode.Filename)
			});

			var vol = ManagerServices.AudioPlayerManager.Volume;
			var vol_is_max = vol >= 1.0;
			var vol_is_min = vol <= 0.0;
			var vol_text = "ボリューム (" + (vol * 100.0).ToString("F0") + "%)";
			yield return new MenuItemDefinition(vol_text, subitems: new[]
			{
				new MenuItemDefinition("上げる", enabled: !vol_is_max, clicked: () => ManagerServices.AudioPlayerManager.Volume = ManagerServices.AudioPlayerManager.Volume+0.1),
				new MenuItemDefinition("下げる", enabled: !vol_is_min, clicked: () => ManagerServices.AudioPlayerManager.Volume = ManagerServices.AudioPlayerManager.Volume-0.1),
			});

			yield return new MenuItemDefinition("-");
			var play_pause_text = (ManagerServices.AudioPlayerManager.IsPaused) ? "再生" : "一時停止";
			yield return new MenuItemDefinition(play_pause_text, clicked: () => ManagerServices.AudioPlayerManager.PlayPause());
			yield return new MenuItemDefinition("スキップ", clicked: async () => await ManagerServices.JukeboxManager.SelectNext(true));
			yield return new MenuItemDefinition("始めから再生", clicked: () => ManagerServices.AudioPlayerManager.Replay());
			yield return new MenuItemDefinition("再生履歴", subitems: CreateRecentPlayMenuItems());
			yield return new MenuItemDefinition("-");
			yield return new MenuItemDefinition("開く", subitems: CreateRecentFolderMenuItems());
			yield return new MenuItemDefinition("-");
			var window_show_hide_text = window.IsVisible ? "ウィンドウを隠す" : "ウィンドウを表示する";
			yield return new MenuItemDefinition(window_show_hide_text, clicked: () => window.WindowShowHideToggle());
			yield return new MenuItemDefinition("ウィンドウを画面右下へ移動", clicked: () => window.ResetWindowPosition());
			yield return new MenuItemDefinition("-");
			yield return new MenuItemDefinition("アップデート", enabled: !ManagerServices.AppUpdateManager.IsShowingUpdateMessage, is_visibled: ManagerServices.AppUpdateManager.IsUpdateReady, clicked: async () => await OnUpdate());
			yield return new MenuItemDefinition("オプション", enabled: !option_dialog_opened, clicked: OpenOptionDialog);
			yield return new MenuItemDefinition("終了", clicked: () => window.Close());
		}
		static IEnumerable<WinForms.MenuItem> ConvertToWinFormsMenuItems(IEnumerable<MenuItemDefinition> items)
		{
			foreach (var i in items)
			{
				var item = new WinForms.MenuItem();
				item.Text = i.Name;
				item.Checked = i.Checked;
				item.Enabled = i.Enabled;
				item.Visible = i.Visibled;
				if (i.Clicked != null)
				{
					var i2 = i;
					item.Click += delegate { i2.Clicked(); };
				}
				if (i.SubItems != null)
				{
					item.MenuItems.AddRange(ConvertToWinFormsMenuItems(i.SubItems).ToArray());
				}
				yield return item;
			}
		}

		static bool folder_dialog_opened = false;
		static bool option_dialog_opened = false;
		static MenuItemDefinition[] CreateRecentFolderMenuItems()
		{
			var head = new[]
			{
				new MenuItemDefinition("フォルダ", clicked: OpenFolderDialog, enabled: !folder_dialog_opened),
				new MenuItemDefinition("-"),
			};
			//
			var current = ManagerServices.MediaDBViewManager.FocusPath.Value;
			var items = ManagerServices.RecentsManager.GetRecentsOpenedFolder()
				.Select(f => new MenuItemDefinition(f,
					is_checked: f.Equals(current, StringComparison.CurrentCultureIgnoreCase),
					clicked: delegate
					{
						Task.Run(() =>
						{
							ManagerServices.MediaDBViewManager.FocusPath.Value = f;
							ManagerServices.JukeboxManager.ViewFocus.Value.Dispose();
							ManagerServices.JukeboxManager.ViewFocus.Value = new MediaDBViewFocus(f);
						});
					}))
				.ToArray();
			if (!items.Any())
				items = new[] { new MenuItemDefinition("履歴はありません", enabled: false), };
			//
			var ret = head.Concat(items).ToArray();
			return ret;
		}
		static MenuItemDefinition[] CreateRecentPlayMenuItems()
		{
			var currentItem = ManagerServices.JukeboxManager.CurrentMedia.Value;
			var current = (currentItem != null) ? currentItem.FilePath : string.Empty;
			var ret = ManagerServices.RecentsManager.GetRecentsPlayItems(20)
				.Select((f, i) => new MenuItemDefinition(Path.GetFileName(f),
					is_checked: f.Equals(current, StringComparison.CurrentCultureIgnoreCase),
					clicked: delegate { ManagerServices.JukeboxManager.SelectPrevious(i); }))
				.ToArray();
			return ret.Any() ? ret : new[] { new MenuItemDefinition("履歴はありません", enabled: false), };
		}
		static async Task OnUpdate()
		{
			if (await ManagerServices.AppUpdateManager.ShowUpdateMessage(new WindowInteropHelper(App.Current.MainWindow).EnsureHandle()))
				App.Current.Shutdown();
		}
		static void OpenFolderDialog()
		{
			if (folder_dialog_opened) return;
			try
			{
				folder_dialog_opened = true;
				using (var dlg = new FolderBrowserDialogEx())
				{
					dlg.UseNewDialog = true;
					dlg.SelectedPath = ManagerServices.MediaDBViewManager.FocusPath.Value;
					var nativeWindow = new WinForms.NativeWindow();
					nativeWindow.AssignHandle(new WindowInteropHelper(App.Current.MainWindow).EnsureHandle());
					try
					{
						if (dlg.ShowDialog(nativeWindow) == WinForms.DialogResult.OK)
						{
							Task.Run(() =>
							{
								ManagerServices.MediaDBViewManager.FocusPath.Value = dlg.SelectedPath;
								ManagerServices.JukeboxManager.ViewFocus.Value.Dispose();
								ManagerServices.JukeboxManager.ViewFocus.Value = new MediaDBViewFocus(dlg.SelectedPath);
							});
						}
					}
					finally { nativeWindow.ReleaseHandle(); }
				}
			}
			finally { folder_dialog_opened = false; }
		}
		static void OpenOptionDialog()
		{
			if (option_dialog_opened) return;

			var nativeWindow = new WinForms.NativeWindow();
			try
			{
				option_dialog_opened = true;
				ManagerServices.ShortcutKeyManager.SuspressKeyEvent = true;
				var handle = new WindowInteropHelper(App.Current.MainWindow).EnsureHandle();
				nativeWindow.AssignHandle(handle);
				using (var dlg = new OptionDialog())
				{
					dlg.CenterWindow(handle);
					var mw = (MainWindow)App.Current.MainWindow;
					dlg.InactiveOpacity = (int)(mw.ViewModel.InactiveOpacity.Value * 100.0);
					dlg.DeactiveOpacity = (int)(mw.ViewModel.DeactiveOpacity.Value * 100.0);
					if (dlg.ShowDialog(nativeWindow) == WinForms.DialogResult.OK)
					{
						mw.ViewModel.InactiveOpacity.Value = dlg.InactiveOpacity / 100.0;
						mw.ViewModel.DeactiveOpacity.Value = dlg.DeactiveOpacity / 100.0;
						mw.ForceDeactivateAnimation();
					}
				}
			}
			finally
			{
				option_dialog_opened = false;
				var skm = ManagerServices.ShortcutKeyManager;
				if (skm != null)
				{
					skm.SuspressKeyEvent = false;
				}
				nativeWindow.ReleaseHandle();
			}
		}

		struct MenuItemDefinition
		{
			public string Name;
			public bool Enabled;
			public bool Checked;
			public Action Clicked;
			public MenuItemDefinition[] SubItems;
			public bool Visibled;

			public MenuItemDefinition(
				string name,
				bool is_checked = false,
				Action clicked = null,
				MenuItemDefinition[] subitems = null,
				bool enabled = true,
				bool is_visibled = true)
			{
				this.Name = name;
				this.Enabled = enabled;
				this.Checked = is_checked;
				this.Clicked = clicked;
				this.SubItems = subitems;
				this.Visibled = is_visibled;
			}
		}

		#endregion
	}
}
