using System;
using System.Threading;
using System.Windows;
using Quala;
using SmartAudioPlayerFx.Update;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	partial class App : Application
	{
		static Mutex mutex;

		/// <summary>
		/// すでに起動しているインスタンスがあるか調べます。
		/// インスタンスがあればtrueが返ります。
		/// </summary>
		static bool IsExistsApplicationInstance
		{
			get
			{
				// mutex != nulなら新規起動確認後のはず
				if (mutex != null) return false;

				mutex = new Mutex(false, "SmartAudioPlayer Fx");
				if (mutex.WaitOne(0, false))
				{
					// 新規起動
					App.Current.Exit += delegate { mutex.ReleaseMutex(); };
					return false;
				}
				else
				{
					// すでに起動しているインスタンスがある
					LogService.AddInfoLog("多重起動を確認しました。");
					mutex.Close();
					mutex = null;
					return true;
				}
			}
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			#region WinForms Initialize

			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);
			WinForms.Application.DoEvents();

			#endregion
			#region Logging

			AppDomain.CurrentDomain.ProcessExit += (_, __) =>
			{
				var path = PreferenceService.CreateFullPath("SmartAudioPlayer Fx.log");
				LogService.Save(path);
			};
			AppDomain.CurrentDomain.UnhandledException += (_, ev) =>
			{
				var ex = ev.ExceptionObject as Exception;
				if (ex != null)
					UIService.ShowExceptionMessage("AppDomain", ex);
			};
			this.DispatcherUnhandledException += (_, ev) =>
			{
				var ex = ev.Exception as Exception;
				if (ex != null)
					UIService.ShowExceptionMessage("Dispatcher", ex);
			};

			#endregion
			#region アップデートチェック＆多重起動の抑制

			UpdateService.OnPostUpdate(e.Args);
			if (UpdateService.OnUpdate(e.Args))
			{
				Shutdown(-2);
				return;
			}

			if(IsExistsApplicationInstance)
			{
				UIService.ShowMessage("多重起動は出来ません。アプリケーションを終了します。");
				Shutdown(-1);
				return;
			}

			#endregion

			UIService.Start();
		}

		void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
		{
			// ログオフ -> Close
			if (e.Cancel == false &&
				UIService.PlayerWindow != null)
			{
				UIService.PlayerWindow.Close();
			}
		}

	}
}
