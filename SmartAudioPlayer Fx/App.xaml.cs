using System;
using System.Threading;
using System.Windows;
using Quala;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.UI;
using SmartAudioPlayerFx.Update;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	partial class App : Application
	{
		#region ctor

		static App()
		{
			// WinForms Initialize
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);

			// Logging
			AppDomain.CurrentDomain.ProcessExit += delegate
			{
				var path = PreferenceService.CreateFullPath("SmartAudioPlayer Fx.log");
				LogService.Save(path);
			};
			AppDomain.CurrentDomain.UnhandledException += (_, e) =>
			{
				var ex = e.ExceptionObject as Exception;
				if (ex != null)
					ShowExceptionMessage("AppDomain", ex);
			};
		}

		public App()
		{
			// Logging
			this.DispatcherUnhandledException += (_, e) =>
			{
				var ex = e.Exception as Exception;
				if (ex != null)
					ShowExceptionMessage("Dispatcher", ex);
			};
		}

		static void ShowExceptionMessage(string source, Exception ex)
		{
			LogService.AddCriticalErrorLog(source, "UnhandledException", ex);
			var message = string.Format(
				"未処理の例外エラーが発生しました ({1}){0}"+
				"----------------------------------------{0}"+
				"{2}",
				Environment.NewLine,
				source,
				ex);
			WinForms.MessageBox.Show(message, "SmartAudioPlayer Fx");
		}

		#endregion

		protected override void OnStartup(StartupEventArgs e)
		{
			// アップデートチェック＆関連処理
			// ** TODO: あとで動作チェックする **
			UpdateService.OnPostUpdate(e.Args);
			if (UpdateService.OnUpdate(e.Args))
			{
				Shutdown();
				return;
			}

			// 多重起動の抑制
			if (IsExistsApplicationInstance())
			{
				WinForms.MessageBox.Show("多重起動は出来ません。アプリケーションを終了します。", "SmartAudioPlayer Fx");
				Shutdown(-1);
				return;
			}

			// 初期化
			base.OnStartup(e);
			TasktrayService.IsVisible = true;
			UpdateService.Start();
			UIService.Start();
		}

		bool IsExistsApplicationInstance()
		{
			// 多重起動の抑制
			var mutex = new Mutex(false, "SmartAudioPlayer Fx");
			if (mutex.WaitOne(0, false) == false)
			{
				// すでに起動しているインスタンスがある
				LogService.AddInfoLog("Application", "多重起動を確認しました。");
				mutex.Close();
				return true;
			}

			// 新規起動だった
			Exit += delegate { mutex.ReleaseMutex(); };
			return false;
		}
	}
}
