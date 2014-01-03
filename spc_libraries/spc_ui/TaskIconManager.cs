using System;
using System.Drawing;
using System.Reactive.Linq;
using System.Windows.Forms;
using System.Windows.Threading;
using wpf = System.Windows;

namespace SmartAudioPlayer
{
	[Standalone]
	public sealed class TaskIconManager : IDisposable
	{
		#region ctor

		NotifyIcon tray;
		object latestClickedTag = null;

		public TaskIconManager(string trayText, Icon trayIcon)
		{
			if (wpf.Application.Current != null && wpf.Application.Current.Dispatcher != Dispatcher.CurrentDispatcher)
				throw new InvalidOperationException("call on UIThread!!");

			// Tasktray作成
			tray = new NotifyIcon();
			tray.Text = trayText;
			tray.Icon = trayIcon;
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

		public void SetMenuItems(Func<MenuItem[]> menuItemsFactory)
		{
			if (tray == null) return;
			if (tray.ContextMenu != null) return;
			tray.ContextMenu = new ContextMenu();
			tray.ContextMenu.Popup += (s, _) =>
			{
				// メニュー内容を動的に変更
				var menu = s as ContextMenu;
				if (menu == null) return;

				var items = menuItemsFactory();
				menu.MenuItems.Clear();
				menu.MenuItems.AddRange(items);
			};
		}
	}

	public static class TaskIconManagerExtensions
	{
		public static IObservable<object> BaloonTipClickedAsObservable(this TaskIconManager manager)
		{
			return Observable.FromEvent<object>(v => manager.BaloonTipClicked += v, v => manager.BaloonTipClicked -= v);
		}
	}
}
