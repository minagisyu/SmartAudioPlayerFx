using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Quala;
using Quala.Windows.Forms;
using SmartAudioPlayerFx.Update;
using SmartAudioPlayerFx.UI;

namespace SmartAudioPlayerFx.Player
{
	static class TasktrayService
	{
		static NotifyIcon tray;
		public static event Action BaloonTipClicked;

		static TasktrayService()
		{
			LogService.AddDebugLog("TasktrayService", "Call ctor");
			// Tasktray作成
			tray = new NotifyIcon();
			tray.Text = "SmartAudioPlayer Fx";
			tray.Icon = new Icon(System.Windows.Application.GetResourceStream(new Uri("/Resources/SAPFx.ico", UriKind.Relative)).Stream);
			tray.ContextMenu = new ContextMenu();
			tray.ContextMenu.Popup += (s,_) =>
			{
				// メニュー内容を動的に変更
				var menu = s as ContextMenu;
				if(menu == null) return;
				menu.MenuItems.Clear();
				menu.MenuItems.AddRange(UIService.CreateWinFormsMenuItems());
			};
			tray.BalloonTipClicked += delegate
			{
				if (BaloonTipClicked != null)
					BaloonTipClicked();
			};
			App.Current.Exit += delegate { TasktrayService.Dispose(); };
		}

		public static void Dispose()
		{
			LogService.AddDebugLog("TasktrayService", "Call Dispose");
			tray.Dispose();
		}

		public static bool IsVisible
		{
			get { return tray.Visible; }
			set
			{
				LogService.AddDebugLog("TasktrayService", "Set IsVisible: {0}", value);
				tray.Visible = value;
			}
		}

		public static void ShowBaloonTip(TimeSpan timeout, ToolTipIcon icon, string title, string message)
		{
			tray.ShowBalloonTip((int)timeout.TotalMilliseconds, title, message, icon);
		}

	}
}
