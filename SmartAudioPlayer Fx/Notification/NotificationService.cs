using Reactive.Bindings;
using System;

namespace SmartAudioPlayerFx.Notification
{
	sealed class NotificationService
	{
		public event Action NotifyClicked;
		public void RaiseNotifyClicked() => NotifyClicked?.Invoke();

		public ReactiveProperty<string> NotifyMessage { get; } = new ReactiveProperty<string>();
	}
}
