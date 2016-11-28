using SmartAudioPlayer.InterfaceHub;
using SmartAudioPlayerFx.Shortcut;
using SmartAudioPlayerFx.Views;
using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Forms;
using System.Windows.Threading;

namespace SmartAudioPlayerFx.Messaging
{
	sealed class NotificationView : IDisposable
	{
		NotifyIcon tray = new NotifyIcon();
		ContextMenuManager _context_menu;

		public NotificationView(NotificationMessage notification, ContextMenuManager context_menu)
		{
			_context_menu = context_menu;

			if (App.Current != null && App.Current.Dispatcher != Dispatcher.CurrentDispatcher)
				throw new InvalidOperationException("call on UIThread!!");

			// Tasktray作成
			tray.Text = "SmartAudioPlayer Fx";
			tray.Icon = new Icon(App.GetResourceStream(new Uri("/Resources/SAPFx.ico", UriKind.Relative)).Stream);
			tray.BalloonTipClicked += (_, __) => BaloonTipClicked?.Invoke();
			tray.Visible = false;

			// NotificationService購読
			BaloonTipClicked += () => notification.RaiseNotifyClicked();
			notification.ShowNotification
				.Select(_ => notification.Message)
				.Where(o => string.IsNullOrWhiteSpace(o) == false)
				.Subscribe(o => tray.ShowBalloonTip((int)TimeSpan.FromSeconds(10).TotalMilliseconds, "SmartAudioPlayer Fx", o, ToolTipIcon.Info));

			tray.Visible = true;
		}

		public event Action BaloonTipClicked;

		public void Dispose()
		{
			tray.Dispose();
		}

		public NotificationView Configure()
		{
			return this;
		}

		public void SetMenuItems(MainWindow mw)
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
				menu.MenuItems.AddRange(_context_menu.CreateWinFormsMenuItems(mw));
			};
		}

	}
}
