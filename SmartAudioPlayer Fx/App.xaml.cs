using Quala;
using Reactive.Bindings;
using SmartAudioPlayerFx.Notification;
using SmartAudioPlayerFx.Preferences;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	// Application-Domain.
	// Controller? Presenter?
    // Controller
	partial class App : Application
	{
        // model
        public static ReferenceManager Models { get; private set; }
        public static ReferenceManager ModelServices { get; private set; }
        public static ReferenceManager UIServices { get; private set; }
        public static LogManager Log { get; private set; }
		public static Storage Storage { get; private set; }

		// ui
		TasktrayIconView tasktray;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			var sw = Stopwatch.StartNew();

			// WinForms Initialize
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);

			// Logger Setting
			Models = new ReferenceManager();
            ModelServices = new ReferenceManager();
            UIServices = new ReferenceManager();
			Log =  Models.Get<LogManager>();
			Log.Output.Subscribe(s =>
			{
				Debug.WriteLine(s);
				using (var stream = Models.Get<Storage>().AppDataRoaming.CreateFilePathInfo("SmartAudioPlayer Fx.log").AppendText())
				{
					stream.WriteLine(s);
				}
			});
			Storage = Models.Get<Storage>();

#if DEBUG
#else
			// Exception Handling
			AppDomain.CurrentDomain.UnhandledException += (_, x) =>
				App.Current?.ShowExceptionMessage(x.ExceptionObject as Exception);
			this.DispatcherUnhandledException += (_, x) =>
				App.Current?.ShowExceptionMessage(x.Exception);
#endif

			// minimum Initialize
			UIDispatcherScheduler.Initialize();
			ManagerServices.Initialize();
			Exit += delegate
			{
				ManagerServices.Dispose();
				Models.Dispose();
			};

			// tasktray
			tasktray = new TasktrayIconView();

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
			tasktray.SetMenuItems();

			// 定期保存(すぐに開始する)
			new DispatcherTimer(
				TimeSpan.FromMinutes(5),
				DispatcherPriority.Normal,
				(_, __) => App.Models.Get<XmlPreferencesManager>().Save(),
				Dispatcher);

			sw.Stop();
			Models.Get<LogManager>().AddDebugLog($"App.OnStartrup: {sw.ElapsedMilliseconds}ms");
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
