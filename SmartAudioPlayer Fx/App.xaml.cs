namespace SmartAudioPlayerFx
{
	using System;
	using System.Diagnostics;
	using System.Reactive.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using __Primitives__;
	using Codeplex.Reactive;
	using Codeplex.Reactive.Extensions;
	using SmartAudioPlayerFx.Managers;
	using SmartAudioPlayerFx.Views;
	using WinForms = System.Windows.Forms;
	using SmartAudioPlayer;

	partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);
			WinForms.Application.DoEvents();
			UIDispatcherScheduler.Initialize();
#if !DEBUG
			// Exception Handling
			AppDomain.CurrentDomain.UnhandledException += (_, x) =>
				ShowExceptionMessage(x.ExceptionObject as Exception);
			this.DispatcherUnhandledException += (_, x) =>
				ShowExceptionMessage(x.Exception);
#endif
			var logFilename = PreferencesManager.CreateFullPath("SmartAudioPlayer Fx.log");
			Logger.SetLogFileName(logFilename);

			// アップデートチェック
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (HandleUpdateProcess(e.Args))
				return;

			// 多重起動の抑制
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (CheckApplicationInstance())
				return;

			// Services Initialize
			var dbFilename = PreferencesManager.CreateFullPath("data", "media.db");
			ManagerServices.Initialize(dbFilename);
			Exit += delegate { ManagerServices.Dispose(); };

			// WindowShow, SetTrayIconMenus
			var window = new Views.MainWindow();
			this.MainWindow = window;
			window.Show();
			App.Current.SessionEnding += delegate { window.Close(); };	// LogOff
			ManagerServices.TaskIconManager.SetMenuItems(window);

			// 定期保存
			Observable.Timer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5))
				.ObserveOnUIDispatcher()
				.Subscribe(_ => ManagerServices.PreferencesManager.Save());
		}

		bool HandleUpdateProcess(string[] args)
		{
			AppUpdateManager.OnPostUpdate(args);
			if (AppUpdateManager.OnUpdate(args))
			{
				Shutdown(-2);
				return true;
			}
			return false;
		}
		bool CheckApplicationInstance()
		{
#if !DEBUG
			var mutex = new Mutex(false, "SmartAudioPlayer Fx");
			if (mutex.WaitOne(0, false))
			{
				// 新規起動
				this.Exit += delegate
				{
					// ReleaseMutex()呼ばないとMutexが残る...
					mutex.ReleaseMutex();
					mutex.Dispose();
				};
				return false;
			}
			else
			{
				// すでに起動しているインスタンスがある
				Logger.AddInfoLog("多重起動を確認しました。");
				WinForms.MessageBox.Show(
					"多重起動は出来ません。アプリケーションを終了します。",
					"SmartAudioPlayer Fx");
				mutex.Dispose();
				Shutdown(-1);
				return true;
			}
#else
			return false;
#endif
		}

		static void ShowExceptionMessage(Exception ex)
		{
			// todo: 専用のダイアログ使う？
			Logger.AddCriticalErrorLog("UnhandledException", ex);
			var message = string.Format(
				"未処理の例外エラーが発生しました{0}" +
				"----------------------------------------{0}" +
				"{1}",
				Environment.NewLine,
				ex);
			using (var dlg = new MessageDialog())
			{
				dlg.Title = "SmartAudioPlayer Fx";
				dlg.HeaderMessage = "未処理の例外エラーが発生しました";
				dlg.DescriptionMessage = ex.ToString();
				dlg.ShowDialog();
			}
		}

	}
}
