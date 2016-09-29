using Quala;
using Quala.Extensions;
using Reactive.Bindings;
using SmartAudioPlayerFx.Managers;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	partial class App : Application
	{
		public static ReferenceManager Models { get; } = new ReferenceManager();

		static App()
		{
			// WinForms Initialize
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			var sw = Stopwatch.StartNew();

#if DEBUG
#else
			// Exception Handling
			AppDomain.CurrentDomain.UnhandledException += (_, x) =>
				App.Current?.ShowExceptionMessage(x.ExceptionObject as Exception);
			this.DispatcherUnhandledException += (_, x) =>
				App.Current?.ShowExceptionMessage(x.Exception);
#endif
			// Logger Setting
			Models.Get<Logging>().LogFilename =
				Models.Get<Storage>().AppDataRoaming.CreateFilePath("SmartAudioPlayer Fx.log");

			// minimum Initialize
			UIDispatcherScheduler.Initialize();
		ManagerServices.Initialize();
			Exit += delegate
			{
				ManagerServices.Dispose();
				Models.Dispose();
			};

			// アップデートチェック
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (HandleUpdateProcess(e.Args))
				return;

			// 多重起動の抑制
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (CheckApplicationInstance())
				return;

			this.MainWindow = new Views.MainWindow();
			this.MainWindow.Show();

			// LogOff -> Close
			App.Current.SessionEnding += delegate
			{
				MainWindow?.Close();
			};

			// Set TrayIcon Menus
			ManagerServices.TaskIconManager.SetMenuItems();

			// 定期保存(すぐに開始する)
			new DispatcherTimer(
				TimeSpan.FromMinutes(5),
				DispatcherPriority.Normal,
				(_, __) => ManagerServices.PreferencesManager.Save(),
				Dispatcher);

			sw.Stop();
			Models.Get<Logging>().AddDebugLog("App.OnStartrup: {0}ms", sw.ElapsedMilliseconds);
		}

		bool HandleUpdateProcess(string[] args)
		{
			ManagerServices.AppUpdateManager.OnPostUpdate(args);
			if (ManagerServices.AppUpdateManager.OnUpdate(args))
			{
				Shutdown(-2);
				return true;
			}
			return false;
		}
		bool CheckApplicationInstance()
		{
#if DEBUG
			return false;
#else
			AppService.AppMutex.Name = "SmartAudioPlayer Fx";
			if (AppService.AppMutex.ExistApplicationInstance())
			{
				// すでに起動しているインスタンスがある
				App.Current?.ShowMessage("多重起動は出来ません。アプリケーションを終了します。");
				Shutdown(-1);
				return true;
			}
			return false;
#endif
		}

	}
}
