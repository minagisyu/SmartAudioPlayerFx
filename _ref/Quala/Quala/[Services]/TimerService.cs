using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;

namespace Quala
{
	/// <summary>
	/// タイマー
	/// </summary>
	[Obsolete("Use Observable.Timer() (Rx Extension/System.Reactive.dll)")]
	public static class TimerService
	{
		static TimerServiceCoreImpl harfSecTimer = new TimerServiceCoreImpl(TimeSpan.FromSeconds(0.5));
		static TimerServiceCoreImpl SecondsTimer = new TimerServiceCoreImpl(TimeSpan.FromSeconds(1));
		static TimerServiceCoreImpl MinutesTimer = new TimerServiceCoreImpl(TimeSpan.FromMinutes(1));
		static TimerServiceCoreImpl HoursTimer = new TimerServiceCoreImpl(TimeSpan.FromHours(1));

		// memo: intervalをTimeSpanにして近い時間間隔のタイマーに自動で追加する
		// run_now: 呼び出し直後に実行するか否か
		// stop: タイマーを停止するにはcontext.Stop()を呼び出す
		public static Context AddHarfSecTimer(int interval, bool run_now, Action<Context> action)
		{
			return harfSecTimer.Add(interval, run_now, action);
		}
		public static Context AddSecondsTimer(int interval, bool run_now, Action<Context> action)
		{
			return SecondsTimer.Add(interval, run_now, action);
		}
		public static Context AddMinutesTimer(int interval, bool run_now, Action<Context> action)
		{
			return MinutesTimer.Add(interval, run_now, action);
		}
		public static Context AddHoursTimer(int interval, bool run_now, Action<Context> action)
		{
			return HoursTimer.Add(interval, run_now, action);
		}

		public sealed class Context
		{
			static long global_id = 0;
			long id;
			int interval;
			int down_count;
			Action<Context> handler;
			internal bool IsDisposed;

			internal Context(int interval, bool run_now, Action<Context> handler)
			{
				this.id = global_id++;
				this.interval = down_count = interval;
				this.handler = handler;
				IsDisposed = false;
				if (run_now)
					handler(this);
				LogService.AddDebugLog("TimerService", "timer started, id: {0}", id);
			}

			internal void Tick()
			{
				down_count--;
				if (down_count <= 0)
				{
					down_count = interval;
					handler(this);
				}
			}

			public void Stop()
			{
				this.IsDisposed = true;
			}
		}

		// タイマー共通実装部
		sealed class TimerServiceCoreImpl
		{
			DispatcherTimer timer;
			List<Context> workers = new List<Context>();

			internal TimerServiceCoreImpl(TimeSpan interval)
			{
				timer = new DispatcherTimer();
				timer.Interval = interval;
				timer.Tick += new EventHandler(timer_Tick);
			}

			void timer_Tick(object sender, EventArgs e)
			{
				lock (workers)
				{
					workers.ForEach(data => data.Tick());
					workers.RemoveAll(data => data.IsDisposed);
					if (workers.Count == 0)
					{
						timer.Stop();
					}
				}
			}

			public Context Add(int interval, bool run_now, Action<Context> action)
			{
				lock (workers)
				{
					var ctx = new Context(interval, run_now, action);
					workers.Add(ctx);
					if (workers.Count == 1)
					{
						timer.Start();
					}
					return ctx;
				}
			}
		}

	}
}
