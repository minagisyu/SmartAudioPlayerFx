using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Views;
using SmartAudioPlayerFx.Views.Options;
using Quala.Win32.Dialog;

namespace SmartAudioPlayerFx.Managers
{
//	[Standalone]
	sealed class TaskIconManager : IDisposable
	{
		#region ctor

		NotifyIcon tray;
		object latestClickedTag = null;

		public TaskIconManager()
		{
			if (App.Current != null && App.Current.Dispatcher != Dispatcher.CurrentDispatcher)
				throw new InvalidOperationException("call on UIThread!!");

			// Tasktray作成
			tray = new NotifyIcon();
			tray.Text = "SmartAudioPlayer Fx";
			tray.Icon = new Icon(App.GetResourceStream(new Uri("/Resources/SAPFx.ico", UriKind.Relative)).Stream);
			tray.BalloonTipClicked += delegate
			{
				if (BaloonTipClicked != null)
					BaloonTipClicked(latestClickedTag);
			};
			tray.Visible = true;
		}

		public void Dispose()
		{
			if (tray != null)
			{
				tray.Visible = false;
				if (tray.ContextMenu != null)
				{
					tray.ContextMenu.Dispose();
				}
				tray.Dispose();
				tray = null;
			}

			BaloonTipClicked = null;
		}

		#endregion

		/// <summary>
		/// バルーンチップがクリックされたときに発生。
		/// ShowBaloonTip()のclickedTagが引数として渡されます。
		/// </summary>
		public event Action<object> BaloonTipClicked;

		/// <summary>
		/// バルーンチップを表示します
		/// </summary>
		/// <param name="timeout"></param>
		/// <param name="icon"></param>
		/// <param name="title"></param>
		/// <param name="message"></param>
		/// <param name="clickedTag">BaloonTipClickedイベントに渡されるオブジェクト。識別にどうぞ。</param>
		public void ShowBaloonTip(TimeSpan timeout, ToolTipIcon icon, string title, string message, object clickedTag)
		{
			latestClickedTag = clickedTag;
			tray.ShowBalloonTip((int)timeout.TotalMilliseconds, title, message, icon);
		}

		public void SetMenuItems()
		{
			if (tray == null) return;
			if (tray.ContextMenu != null) return;
			tray.ContextMenu = new ContextMenu();
			tray.ContextMenu.Popup += (s, _) =>
			{
				// メニュー内容を動的に変更
				var menu = s as ContextMenu;
				if (menu == null) return;
				menu.MenuItems.Clear();
				menu.MenuItems.AddRange(TaskIconManager.CreateWinFormsMenuItems());
			};
		}

		#region ContextMenu

		// Tasktray & PlayerWindowで使う
		public static MenuItem[] CreateWinFormsMenuItems()
		{
			return ConvertToWinFormsMenuItems(CreateMenuItems()).ToArray();
		}
		static IEnumerable<MenuItem> ConvertToWinFormsMenuItems(IEnumerable<MenuItemDefinition> items)
		{
			foreach (var i in items)
			{
				var item = new MenuItem();
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
		static IEnumerable<MenuItemDefinition> CreateMenuItems()
		{
			var mw = ((MainWindow)App.Current.MainWindow);
			//
			var is_videodraw = mw.MediaListWindow.ViewModel.IsVideoDrawing.Value;
			var is_repeat = ManagerServices.JukeboxManager.IsRepeat.Value;
			var is_random = (ManagerServices.JukeboxManager.SelectMode.Value == JukeboxManager.SelectionMode.Random);
			var is_sequential = (ManagerServices.JukeboxManager.SelectMode.Value == JukeboxManager.SelectionMode.Filename);
			yield return new MenuItemDefinition("再生モード", subitems: new[]
			{
				new MenuItemDefinition("リピート", is_repeat, ()=> ManagerServices.JukeboxManager.IsRepeat.Value=!is_repeat),
				new MenuItemDefinition("-"),
				new MenuItemDefinition("ランダム", is_random, ()=> ManagerServices.JukeboxManager.SelectMode.Value = JukeboxManager.SelectionMode.Random),
				new MenuItemDefinition("ファイル名順", is_sequential, ()=> ManagerServices.JukeboxManager.SelectMode.Value=JukeboxManager.SelectionMode.Filename)
			});
			var vol = ManagerServices.AudioPlayerManager.Volume;
			var vol_is_max = vol >= 1.0;
			var vol_is_min = vol <= 0.0;
			var vol_text = "ボリューム (" + (vol * 100.0).ToString("F0") + "%)";
			yield return new MenuItemDefinition(vol_text, subitems: new[]
			{
				new MenuItemDefinition("上げる", enabled: !vol_is_max, clicked: ()=>ManagerServices.AudioPlayerManager.Volume = ManagerServices.AudioPlayerManager.Volume+0.1),
				new MenuItemDefinition("下げる", enabled: !vol_is_min, clicked: ()=>ManagerServices.AudioPlayerManager.Volume = ManagerServices.AudioPlayerManager.Volume-0.1),
			});
			yield return new MenuItemDefinition("-");
			var play_pause_text = (ManagerServices.AudioPlayerManager.IsPaused) ? "再生" : "一時停止";
			yield return new MenuItemDefinition(play_pause_text, clicked: () => ManagerServices.AudioPlayerManager.PlayPause());
			yield return new MenuItemDefinition("スキップ", clicked: () => ManagerServices.JukeboxManager.SelectNext(true));
			yield return new MenuItemDefinition("始めから再生", clicked: () => ManagerServices.AudioPlayerManager.Replay());
			yield return new MenuItemDefinition("再生履歴", subitems: CreateRecentPlayMenuItems());
			yield return new MenuItemDefinition("-");
			yield return new MenuItemDefinition("開く", subitems: CreateRecentFolderMenuItems());
			yield return new MenuItemDefinition("-");
			var window_show_hide_text = mw.IsVisible ? "ウィンドウを隠す" : "ウィンドウを表示する";
			yield return new MenuItemDefinition(window_show_hide_text, clicked: () => mw.WindowShowHideToggle());
			yield return new MenuItemDefinition("ウィンドウを画面右下へ移動", clicked: () => mw.ResetWindowPosition());
			yield return new MenuItemDefinition("-");
			yield return new MenuItemDefinition("アップデート", enabled: !ManagerServices.AppUpdateManager.IsShowingUpdateMessage, is_visibled: ManagerServices.AppUpdateManager.IsUpdateReady, clicked: OnUpdate);
			yield return new MenuItemDefinition("オプション", enabled: !option_dialog_opened, clicked: OpenOptionDialog);
			yield return new MenuItemDefinition("終了", clicked: () => mw.Close());
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
		static async void OnUpdate()
		{
			if (await ManagerServices.AppUpdateManager.ShowUpdateMessageAsync(new WindowInteropHelper(App.Current.MainWindow).EnsureHandle()))
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
					var nativeWindow = new NativeWindow();
					nativeWindow.AssignHandle(new WindowInteropHelper(App.Current.MainWindow).EnsureHandle());
					try
					{
						if (dlg.ShowDialog(nativeWindow) == DialogResult.OK)
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

			var nativeWindow = new NativeWindow();
			try
			{
				option_dialog_opened = true;
				ManagerServices.ShortcutKeyManager.SuspressKeyEvent = true;
				var handle = new WindowInteropHelper(App.Current.MainWindow).EnsureHandle();
				nativeWindow.AssignHandle(handle);
				using (var dlg = new OptionDialog())
				{
					App.Current.CenterWindow(dlg.Handle, handle);
					var mw = (MainWindow)App.Current.MainWindow;
					dlg.InactiveOpacity = (int)(mw.ViewModel.InactiveOpacity.Value * 100.0);
					dlg.DeactiveOpacity = (int)(mw.ViewModel.DeactiveOpacity.Value * 100.0);
					if (dlg.ShowDialog(nativeWindow) == DialogResult.OK)
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
				ManagerServices.ShortcutKeyManager.SuspressKeyEvent = false;
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

	static class TaskIconManagerExtensions
	{
		public static IObservable<object> BaloonTipClickedAsObservable(this TaskIconManager manager)
		{
			return Observable.FromEvent<object>(
				v => manager.BaloonTipClicked += v,
				v => manager.BaloonTipClicked -= v);
		}
	}
}
