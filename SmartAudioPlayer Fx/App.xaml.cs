using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SmartAudioPlayerFx.Managers;
using SmartAudioPlayerFx.Views;
using WinForms = System.Windows.Forms;
using Reactive.Bindings;
using SmartAudioPlayerFx.Data;
using Quala;

namespace SmartAudioPlayerFx
{
	partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			var sw = Stopwatch.StartNew();

			// WinForms Initialize
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);
			WinForms.Application.DoEvents();
#if DEBUG
#else
			// Exception Handling
			AppDomain.CurrentDomain.UnhandledException += (_, x) =>
				App.Current?.ShowExceptionMessage(x.ExceptionObject as Exception);
			this.DispatcherUnhandledException += (_, x) =>
				App.Current?.ShowExceptionMessage(x.Exception);
#endif
			// Logger Setting
			AppService.Log.LogFilename = AppService.Storage.AppDataRoaming
					.CreateFilePath("SmartAudioPlayer Fx.log");

			// minimum Initialize
			UIDispatcherScheduler.Initialize();
			var dbFilename = AppService.Storage.AppDataRoaming
				.CreateFilePath("data", "media.db");
			ManagerServices.Initialize(dbFilename);
			Exit += delegate
			{
				ManagerServices.Dispose();
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
				if (MainWindow == null) return;
				MainWindow.Close();
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
			AppService.Log.AddDebugLog("App.OnStartrup: {0}ms", sw.ElapsedMilliseconds);
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
