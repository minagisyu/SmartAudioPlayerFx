using Quala;
using Reactive.Bindings;
using SimpleInjector;
using SmartAudioPlayerFx.AppUpdate;
using SmartAudioPlayerFx.MediaDB;
using SmartAudioPlayerFx.MediaPlayer;
using SmartAudioPlayerFx.Notification;
using SmartAudioPlayerFx.Preferences;
using SmartAudioPlayerFx.Shortcut;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
		public static Container Services { get; } = new Container();

		static App()
		{
			// WinForms Initialize
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);

			// Register and Pre-Initialize Services
			Services.RegisterSingleton(() => new AppMutexManager("SmartAudioPlayer Fx"));

			Services.RegisterSingleton<StorageManager>();
			Services.RegisterInitializer<StorageManager>(storage =>
			{
				var asm = Assembly.GetEntryAssembly();
				var asmName = asm != null ? asm.GetName().Name : string.Empty;
				storage.AppDataDirectory = new StorageManager.DataPath(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), asmName)));
				storage.AppDirectory = new StorageManager.DataPath(new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), asmName)));
			});

			Services.RegisterSingleton<LogManager>();
			Services.RegisterInitializer<LogManager>(log =>
			{
				var logDir = Services.GetInstance<StorageManager>()
					.AppDataDirectory
					.CreateFilePathInfo("SmartAudioPlayer Fx.log");
				log.WriteLogHeader(Assembly.GetEntryAssembly());
				log.Output.Subscribe(s =>
				{
					Debug.WriteLine(s);
					using (var stream = logDir.AppendText())
					{
						stream.WriteLine(s);
					}
				});
			});
			//= standalone
			Services.RegisterSingleton<XmlPreferencesManager>();
			Services.RegisterSingleton<AudioPlayerManager>();
			Services.RegisterSingleton<NotificationManager>();
			Services.RegisterSingleton<TasktrayIconView>();//[NotificationManager]
			Services.RegisterSingleton<MediaDBManager>();
			//=require Preferences+TaskIcon
			Services.RegisterSingleton<AppUpdateManager>();
			//=require Preferences
			Services.RegisterSingleton<MediaItemFilterManager>();
			//=require Preferences+MediaDB+MediaItemFilter
			Services.RegisterSingleton<MediaDBViewManager>();
			//=require Preferences+MediaDBView
			Services.RegisterSingleton<RecentsManager>();
			//=require Preferences+AudioPlayer+MediaDBView
			Services.RegisterSingleton<JukeboxManager>();
			//=require Preferences+AudioPlayer+Jukebox
			Services.RegisterSingleton<ShortcutKeyManager>();
			//=add_xxx
			Services.RegisterSingleton<ContextMenuManager>();
		}

		// ui
		TasktrayIconView tasktray;

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

			// minimum Initialize
			UIDispatcherScheduler.Initialize();
			Exit += delegate
			{
				Services.Dispose();
			};

			// tasktray
			tasktray = Services.GetInstance<TasktrayIconView>();

			// アップデートチェック
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (HandleUpdateProcess(e.Args))
				return;

#if !DEBUG
			// 多重起動の抑制
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (CheckApplicationInstance())
				return;
#endif
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
				(_, __) => App.Services.GetInstance<XmlPreferencesManager>().Save(),
				Dispatcher);

			sw.Stop();
			App.Services.GetInstance<LogManager>().AddDebugLog($"App.OnStartrup: {sw.ElapsedMilliseconds}ms");
		}

		bool HandleUpdateProcess(string[] args)
		{
			var app_update = Services.GetInstance<AppUpdateManager>();
			app_update.OnPostUpdate(args);
			if (app_update.OnUpdate(args))
			{
				Shutdown(-2);
				return true;
			}
			return false;
		}
		bool CheckApplicationInstance()
		{
			if (Services.GetInstance<AppMutexManager>().ExistApplicationInstance())
			{
				// すでに起動しているインスタンスがある
				App.Current?.ShowMessage("多重起動は出来ません。アプリケーションを終了します。");
				Shutdown(-1);
				return true;
			}
			return false;
		}

	}
}
