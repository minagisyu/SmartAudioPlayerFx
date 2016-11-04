using Quala;
using Reactive.Bindings;
using System;

namespace SmartAudioPlayerFx.Messaging
{
	// 通知メッセージの表示を意図した仲介クラス
	// メッセージの表示、ユーザーレスポンスを期待する
	// Domain層で利用され、View層はこれを参照して実装する
	[SingletonService]
	public sealed class NotificationMessage
	{
		public event Action NotifyClicked;
		public void RaiseNotifyClicked() => NotifyClicked?.Invoke();

		public string Message { get; set; }

		public ReactiveCommand ShowNotification { get; } = new ReactiveCommand();
	}
}
