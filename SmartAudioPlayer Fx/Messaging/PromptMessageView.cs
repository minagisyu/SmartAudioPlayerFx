using Quala;
using Quala.Win32.Dialog;
using System;
using System.Reactive.Linq;

namespace SmartAudioPlayerFx.Messaging
{
	sealed class PromptMessageView
	{
		public PromptMessageView(PromptMessage message)
		{
			message.ShowDialog
				.Select(_ => new { message.Message, message.Description, })
				.Where(m => string.IsNullOrWhiteSpace(m.Message) == false)
				.Subscribe(m =>
				{
					if (string.IsNullOrWhiteSpace(m.Description) == false)
					{
						// WPFのMessageBoxはビジュアルスタイルが効かないから使わない
						System.Windows.Forms.MessageBox.Show(m.Message, "SmartAudioPlayer Fx");
					}
					else
					{
						using (var dlg = new MessageDialog())
						{
							dlg.Title = "SmartAudioPlayer Fx";
							dlg.HeaderMessage = m.Message;
							dlg.DescriptionMessage = m.Description;
							dlg.ShowDialog();
						}
					}
				});

		}

		public PromptMessageView Configure()
		{
			// DialogMessageの購読をするために呼び出す
			// 将来的にはタイトルの設定でもすればいいのかな？
			return this;
		}

		[Obsolete]
		public void ShowExceptionMessage(Exception ex)
		{
			// todo: 専用のダイアログ使う？
			App.Services.GetInstance<LogManager>().AddCriticalErrorLog("UnhandledException", ex);
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
	}
}
