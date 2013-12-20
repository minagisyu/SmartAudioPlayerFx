using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using Quala;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.UI;
using SmartAudioPlayerFx.UI.Views;
using WinForms = System.Windows.Forms;
using SmartAudioPlayerFx.Update;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace SmartAudioPlayerFx
{
	partial class App : Application
	{
		#region ctor

		static App()
		{
			AppDomain.CurrentDomain.ProcessExit += delegate
			{
				var name = PreferenceService.CreateFullPath("SmartAudioPlayer Fx.log");
				LogService.Save(name);
			};
			AppDomain.CurrentDomain.UnhandledException += (_, e)=>
			{
				LogService.AddErrorLog("AppDomain", "UnhandledException", e.ExceptionObject as Exception);
				WinForms.MessageBox.Show(string.Format(
					"未処理の例外エラーが発生しました (AppDomain){0}"+
					"----------------------------------------{0}"+
					"{1}",
					Environment.NewLine,
					e.ExceptionObject),
					"SmartAudioPlayer Fx");
			};
		}

		public App()
		{
			this.DispatcherUnhandledException += (_, e)=>
			{
				LogService.AddErrorLog("Dispatcher", "UnhandledException", e.Exception);
				WinForms.MessageBox.Show(string.Format(
					"未処理の例外エラーが発生しました (Dispatcher){0}"+
					"----------------------------------------{0}"+
					"{1}",
					Environment.NewLine,
					e.Exception),
					"SmartAudioPlayer Fx");
			};
		}

		#endregion

		bool IsExistsApplicationInstance()
		{
			// 多重起動の抑制
			bool created;
			var assembly = Assembly.GetExecutingAssembly();
			var mutex = new Mutex(true, "Global\\" + assembly.Location.Replace('\\', '_').Replace('/', '_'), out created);
			if (created)
				Exit += delegate { mutex.Close(); };
			return !created;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);

			// アップデートチェック＆関連処理
			UpdateService.OnPostUpdate(e.Args);
			if (UpdateService.OnUpdate(e.Args))
			{
				Shutdown();
				return;
			}

			// 多重起動の抑制
			if (Debugger.IsAttached == false && IsExistsApplicationInstance())
			{
				LogService.AddInfoLog("Application", "多重起動を確認しました。現在のプロセスを終了します。");
				WinForms.MessageBox.Show("多重起動は出来ません。アプリケーションを終了します。", "SmartAudioPlayer Fx");
				Shutdown(-1);
				return;
			}

			// 初期化
			base.OnStartup(e);
			MediaDBService.PrepareService();
			ShortcutKeyService.PrepareService();
			TasktrayService.IsVisible = true;
			UpdateService.LoadPreferences();
			JukeboxService.PrepareService();
			UIService.PrepareService();

		}
	}
}
