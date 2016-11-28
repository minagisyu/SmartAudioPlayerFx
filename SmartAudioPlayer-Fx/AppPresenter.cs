using SmartAudioPlayerFx.Messaging;
using SmartAudioPlayerFx.Views;
using System;

namespace SmartAudioPlayerFx
{
	public sealed class AppPresenter
	{
		readonly PromptMessageView _prompt;
		readonly NotificationView _notify;
		readonly Lazy<MainWindow> _mainWindow;

		public AppPresenter()
		{
			_prompt = App.Services.GetInstance<PromptMessageView>().Configure();
			_notify = App.Services.GetInstance<NotificationView>().Configure();
			_mainWindow = new Lazy<MainWindow>(() =>
			{
				var wnd = new MainWindow();
				_notify.SetMenuItems(wnd);
				return wnd;
			});
		}

		public void MainWindow_Show()
		{
			if (!_mainWindow.IsValueCreated) return;
			_mainWindow.Value.Show();
		}

		public void MainWindow_Close()
		{
			if (!_mainWindow.IsValueCreated) return;
			_mainWindow.Value.Close();
		}
	}
}
