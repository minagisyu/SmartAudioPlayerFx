using System;
using System.Drawing;
using System.Windows.Forms;
using Quala;
using WPF = System.Windows;

namespace SmartAudioPlayerFx
{
	static class TasktrayService
	{
		static NotifyIcon tray;
		static object latestClickedTag = null;

		/// <summary>
		/// バルーンチップがクリックされたときに発生。
		/// ShowBaloonTip()のclickedTagが引数として渡されます。
		/// </summary>
		public static event Action<object> BaloonTipClicked;

		static TasktrayService()
		{
			LogService.AddDebugLog("Call ctor");
			// Tasktray作成
			tray = new NotifyIcon();
			tray.Text = "SmartAudioPlayer Fx";
			tray.Icon = new Icon(WPF.Application.GetResourceStream(new Uri("/Resources/SAPFx.ico", UriKind.Relative)).Stream);
			tray.ContextMenu = new ContextMenu();
			tray.ContextMenu.Popup += (s, _) =>
			{
				// メニュー内容を動的に変更
				var menu = s as ContextMenu;
				if (menu == null) return;
				menu.MenuItems.Clear();
				menu.MenuItems.AddRange(UIService.CreateWinFormsMenuItems());
			};
			tray.BalloonTipClicked += delegate
			{
				if (BaloonTipClicked != null)
					BaloonTipClicked(latestClickedTag);
			};
			App.Current.Exit += delegate { TasktrayService.Dispose(); };
		}

		public static void Dispose()
		{
			LogService.AddDebugLog("Call Dispose");
			tray.Dispose();
		}

		public static bool IsVisible
		{
			get { return tray.Visible; }
			set
			{
				LogService.AddDebugLog("Set IsVisible: {0}", value);
				tray.Visible = value;
			}
		}

		/// <summary>
		/// バルーンチップを表示します
		/// </summary>
		/// <param name="timeout"></param>
		/// <param name="icon"></param>
		/// <param name="title"></param>
		/// <param name="message"></param>
		/// <param name="clickedTag">BaloonTipClickedイベントに渡されるオブジェクト。識別にどうぞ。</param>
		public static void ShowBaloonTip(TimeSpan timeout, ToolTipIcon icon, string title, string message, object clickedTag)
		{
			latestClickedTag = clickedTag;
			tray.ShowBalloonTip((int)timeout.TotalMilliseconds, title, message, icon);
		}

	}
}
