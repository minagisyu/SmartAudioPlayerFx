using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Quala.WPF.Extensions
{
	public static class EventAsObservableExtensions
	{
		// System.AppDomain
		public static IObservable<EventPattern<EventArgs>> ProcessExitAsObservable(this System.AppDomain domain)
		{
			return Observable.FromEventPattern<EventHandler, EventArgs>(v => domain.ProcessExit += v, v => domain.ProcessExit -= v);
		}
		public static IObservable<EventPattern<UnhandledExceptionEventArgs>> UnhandledExceptionAsObservable(this System.AppDomain domain)
		{
			return Observable.FromEventPattern<UnhandledExceptionEventHandler, UnhandledExceptionEventArgs>(v => domain.UnhandledException += v, v => domain.UnhandledException -= v);
		}

		// System.Windows.Application
		public static IObservable<EventPattern<DispatcherUnhandledExceptionEventArgs>> DispatcherUnhandledExceptionAsObservable(this System.Windows.Application app)
		{
			return Observable.FromEventPattern<DispatcherUnhandledExceptionEventHandler, DispatcherUnhandledExceptionEventArgs>(v => app.DispatcherUnhandledException += v, v => app.DispatcherUnhandledException -= v);
		}
		public static IObservable<EventPattern<ExitEventArgs>> ExitAsObservable(this System.Windows.Application app)
		{
			return Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(v => app.Exit += v, v => app.Exit -= v);
		}

	}

}
