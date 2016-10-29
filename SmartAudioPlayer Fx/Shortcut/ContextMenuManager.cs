using Quala.Win32.Dialog;
using SmartAudioPlayerFx.AppUpdate;
using SmartAudioPlayerFx.MediaDB;
using SmartAudioPlayerFx.MediaPlayer;
using SmartAudioPlayerFx.Views;
using SmartAudioPlayerFx.Views.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;

namespace SmartAudioPlayerFx.Shortcut
{
	class ContextMenuManager
	{
		MainWindow _main_window;
		JukeboxManager _jukebox;
		AudioPlayerManager _audio_player;
		AppUpdateManager _app_update;
		MediaDBViewManager _media_db_view;
		RecentsManager _recents;
		ShortcutKeyManager _shortcut_key;

		public ContextMenuManager(MainWindow main_window,
			JukeboxManager jukebox, AudioPlayerManager audio_player,
			AppUpdateManager app_update,
			MediaDBViewManager media_db_view,
			RecentsManager recents,
			ShortcutKeyManager shortcut_key)
		{
			_main_window = main_window;
			_jukebox = jukebox;
			_audio_player = audio_player;
			_app_update = app_update;
			_media_db_view = media_db_view;
			_recents = recents;
			_shortcut_key = shortcut_key;
		}

		// Tasktray & PlayerWindowで使う
		public MenuItem[] CreateWinFormsMenuItems()
		{
			return ConvertToWinFormsMenuItems(CreateMenuItems()).ToArray();
		}
		IEnumerable<MenuItem> ConvertToWinFormsMenuItems(IEnumerable<MenuItemDefinition> items)
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
		IEnumerable<MenuItemDefinition> CreateMenuItems()
		{
			var mw = _main_window;//((MainWindow)App.Current.MainWindow);
			//
			var is_videodraw = mw.MediaListWindow.ViewModel.IsVideoDrawing.Value;
			var is_repeat = _jukebox.IsRepeat.Value;
			var is_random = (_jukebox.SelectMode.Value == JukeboxManager.SelectionMode.Random);
			var is_sequential = (_jukebox.SelectMode.Value == JukeboxManager.SelectionMode.Filename);
			yield return new MenuItemDefinition("再生モード", subitems: new[]
			{
				new MenuItemDefinition("リピート", is_repeat, ()=> _jukebox.IsRepeat.Value=!is_repeat),
				new MenuItemDefinition("-"),
				new MenuItemDefinition("ランダム", is_random, ()=> _jukebox.SelectMode.Value = JukeboxManager.SelectionMode.Random),
				new MenuItemDefinition("ファイル名順", is_sequential, ()=> _jukebox.SelectMode.Value=JukeboxManager.SelectionMode.Filename)
			});
			var vol = _audio_player.Volume;
			var vol_is_max = vol >= 1.0;
			var vol_is_min = vol <= 0.0;
			var vol_text = "ボリューム (" + (vol * 100.0).ToString("F0") + "%)";
			yield return new MenuItemDefinition(vol_text, subitems: new[]
			{
				new MenuItemDefinition("上げる", enabled: !vol_is_max, clicked: ()=>_audio_player.Volume = _audio_player.Volume+0.1),
				new MenuItemDefinition("下げる", enabled: !vol_is_min, clicked: ()=>_audio_player.Volume = _audio_player.Volume-0.1),
			});
			yield return new MenuItemDefinition("-");
			var play_pause_text = (_audio_player.IsPaused) ? "再生" : "一時停止";
			yield return new MenuItemDefinition(play_pause_text, clicked: () => _audio_player.PlayPause());
			yield return new MenuItemDefinition("スキップ", clicked: () => _jukebox.SelectNext(true));
			yield return new MenuItemDefinition("始めから再生", clicked: () => _audio_player.Replay());
			yield return new MenuItemDefinition("再生履歴", subitems: CreateRecentPlayMenuItems());
			yield return new MenuItemDefinition("-");
			yield return new MenuItemDefinition("開く", subitems: CreateRecentFolderMenuItems());
			yield return new MenuItemDefinition("-");
			var window_show_hide_text = mw.IsVisible ? "ウィンドウを隠す" : "ウィンドウを表示する";
			yield return new MenuItemDefinition(window_show_hide_text, clicked: () => mw.WindowShowHideToggle());
			yield return new MenuItemDefinition("ウィンドウを画面右下へ移動", clicked: () => mw.ResetWindowPosition());
			yield return new MenuItemDefinition("-");
			yield return new MenuItemDefinition("アップデート", enabled: !_app_update.IsShowingUpdateMessage, is_visibled: _app_update.IsUpdateReady, clicked: OnUpdate);
			yield return new MenuItemDefinition("オプション", enabled: !option_dialog_opened, clicked: OpenOptionDialog);
			yield return new MenuItemDefinition("終了", clicked: () => mw.Close());
		}

		bool folder_dialog_opened = false;
		bool option_dialog_opened = false;
		MenuItemDefinition[] CreateRecentFolderMenuItems()
		{
			var head = new[]
			{
				new MenuItemDefinition("フォルダ", clicked: OpenFolderDialog, enabled: !folder_dialog_opened),
				new MenuItemDefinition("-"),
			};
			//
			var current = _media_db_view.FocusPath.Value;
			var items = _recents.GetRecentsOpenedFolder()
				.Select(f => new MenuItemDefinition(f,
					is_checked: f.Equals(current, StringComparison.CurrentCultureIgnoreCase),
					clicked: delegate
					{
						Task.Run(() =>
						{
							_media_db_view.FocusPath.Value = f;
							_jukebox.ViewFocus.Value.Dispose();
							_jukebox.ViewFocus.Value = new MediaDBViewFocus(f);
						});
					}))
				.ToArray();
			if (!items.Any())
				items = new[] { new MenuItemDefinition("履歴はありません", enabled: false), };
			//
			var ret = head.Concat(items).ToArray();
			return ret;
		}
		MenuItemDefinition[] CreateRecentPlayMenuItems()
		{
			var currentItem = _jukebox.CurrentMedia.Value;
			var current = (currentItem != null) ? currentItem.FilePath : string.Empty;
			var ret = _recents.GetRecentsPlayItems(20)
				.Select((f, i) => new MenuItemDefinition(Path.GetFileName(f),
					is_checked: f.Equals(current, StringComparison.CurrentCultureIgnoreCase),
					clicked: delegate { _jukebox.SelectPrevious(i); }))
				.ToArray();
			return ret.Any() ? ret : new[] { new MenuItemDefinition("履歴はありません", enabled: false), };
		}
		async void OnUpdate()
		{
			if (await _app_update.ShowUpdateMessageAsync(new WindowInteropHelper(_main_window).EnsureHandle()))
				App.Current.Shutdown();
		}
		void OpenFolderDialog()
		{
			if (folder_dialog_opened) return;
			try
			{
				folder_dialog_opened = true;
				using (var dlg = new FolderBrowserDialogEx())
				{
					dlg.UseNewDialog = true;
					dlg.SelectedPath = _media_db_view.FocusPath.Value;
					var nativeWindow = new NativeWindow();
					nativeWindow.AssignHandle(new WindowInteropHelper(_main_window).EnsureHandle());
					try
					{
						if (dlg.ShowDialog(nativeWindow) == DialogResult.OK)
						{
							Task.Run(() =>
							{
								_media_db_view.FocusPath.Value = dlg.SelectedPath;
								_jukebox.ViewFocus.Value.Dispose();
								_jukebox.ViewFocus.Value = new MediaDBViewFocus(dlg.SelectedPath);
							});
						}
					}
					finally { nativeWindow.ReleaseHandle(); }
				}
			}
			finally { folder_dialog_opened = false; }
		}
		void OpenOptionDialog()
		{
			if (option_dialog_opened) return;

			var nativeWindow = new NativeWindow();
			try
			{
				option_dialog_opened = true;
				_shortcut_key.SuspressKeyEvent = true;
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
				_shortcut_key.SuspressKeyEvent = false;
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

	}
}
