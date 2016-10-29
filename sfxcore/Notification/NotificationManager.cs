using Quala;
using Reactive.Bindings;
using System;

namespace SmartAudioPlayerFx.Notification
{
	[SingletonService]
	public sealed class NotificationManager
	{
		public event Action NotifyClicked;
		public void RaiseNotifyClicked() => NotifyClicked?.Invoke();

		public ReactiveProperty<string> NotifyMessage { get; } = new ReactiveProperty<string>();
	}
}
