using Quala;
using Reactive.Bindings;
using SimpleInjector;
using SmartAudioPlayerFx.AppUpdate;
using SmartAudioPlayerFx.MediaDB;
using SmartAudioPlayerFx.MediaPlayer;
using SmartAudioPlayerFx.Messaging;
using SmartAudioPlayerFx.Preferences;
using SmartAudioPlayerFx.Shortcut;
using SmartAudioPlayerFx.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	partial class App : Application
	{
		// model
		public static Container Services { get; } = new Container();

		protected override void OnStartup(StartupEventArgs e)
		{
			var sw = Stopwatch.StartNew();

			// Component Initialize
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);
			UIDispatcherScheduler.Initialize();
			RegisterServices(Services);
			Exit += (_, __) => Services.Dispose();

			// Exception Handling
			HandleUnhandledException();

			// 表示系の購読、Domain<->View間のバインド？ (DialogMessage / NotificationMessage / ShortcutAction)
			Services.GetInstance<DialogMessageView>().Configure();
			Services.GetInstance<TasktrayIconView>().Configure();

			// アップデートチェック
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (HandleUpdateProcess(e.Args))
				return;

			// 多重起動の抑制
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (CheckApplicationInstance())
				return;

			// MainWindow
			this.MainWindow = new Views.MainWindow();
			this.MainWindow.Show();
			App.Current.SessionEnding += (_, __) => MainWindow?.Close(); // LogOff -> Close
			Services.GetInstance<TasktrayIconView>().SetMenuItems((MainWindow)this.MainWindow); // Set TrayIcon Menus

			// Preference
			ConfigureAutoSave();

			base.OnStartup(e);
			sw.Stop();
			App.Services.GetInstance<LogManager>().AddDebugLog($"App.OnStartrup: {sw.ElapsedMilliseconds}ms");
		}

		void ConfigureAutoSave()
		{
			// 定期保存(すぐに開始する)
			var timer = new DispatcherTimer(TimeSpan.FromMinutes(5), DispatcherPriority.Normal,
				(_, __) => App.Services.GetInstance<XmlPreferencesManager>().Save(),
				Dispatcher);
			Exit += (_, __) => timer.Stop();
		}

		void HandleUnhandledException()
		{
			/*	AppDomain.CurrentDomain.UnhandledException += (_, x) =>
					App.Current?.ShowExceptionMessage(x.ExceptionObject as Exception);
				this.DispatcherUnhandledException += (_, x) =>
					App.Current?.ShowExceptionMessage(x.Exception);
			*/
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
			//	App.Current?.ShowMessage("多重起動は出来ません。アプリケーションを終了します。");
				Shutdown(-1);
				return true;
			}
			return false;
		}

		void RegisterServices(Container container)
		{
			// Register and Pre-Initialize Services
			container.RegisterSingleton(() => new AppMutexManager("SmartAudioPlayer Fx"));

			container.RegisterSingleton<StorageManager>();
			container.RegisterInitializer<StorageManager>(storage =>
			{
				var asm = Assembly.GetEntryAssembly();
				var asmName = asm != null ? asm.GetName().Name : string.Empty;
				storage.AppDataDirectory = new StorageManager.DataPath(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), asmName)));
				storage.AppDirectory = new StorageManager.DataPath(new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), asmName)));
			});

			container.RegisterSingleton<LogManager>();
			container.RegisterInitializer<LogManager>(log =>
			{
				var logDir = container.GetInstance<StorageManager>()
					.AppDataDirectory
					.CreateFilePathInfo("SmartAudioPlayer Fx.log");
				log.WriteLogHeader(Assembly.GetEntryAssembly());
				log.Output.Subscribe(s =>
				{
					lock (logDir)
					{
						Debug.WriteLine(s);
						using (var stream = logDir.AppendText())
						{
							stream.WriteLine(s);
						}
					}
				});
			});
			//= standalone
			container.RegisterSingleton<XmlPreferencesManager>();
			container.RegisterSingleton<AudioPlayerManager>();
			container.RegisterSingleton<NotificationMessage>();
			container.RegisterSingleton<TasktrayIconView>();//[NotificationManager]
			container.RegisterSingleton<MediaDBManager>();
			//=require Preferences+TaskIcon
			container.RegisterSingleton<AppUpdateManager>();
			//=require Preferences
			container.RegisterSingleton<MediaItemFilterManager>();
			//=require Preferences+MediaDB+MediaItemFilter
			container.RegisterSingleton<MediaDBViewManager>();
			//=require Preferences+MediaDBView
			container.RegisterSingleton<RecentsManager>();
			//=require Preferences+AudioPlayer+MediaDBView
			container.RegisterSingleton<JukeboxManager>();
			//=require Preferences+AudioPlayer+Jukebox
			container.RegisterSingleton<ShortcutKeyManager>();
			//=add_xxx
			container.RegisterSingleton<ContextMenuManager>();

			// verify (for DEBUG or TEST only)
		//	container.Verify();
		}

	}
}
