using Microsoft.Win32;
using Quala;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmartAudioPlayerFx.MediaPlayer
{
	// 音楽を再生するためのプレーヤー
	// 内部でWPFのMediaPlayerクラスを利用するが、再生エラー時にレジストリ追加してリトライするなどの機能を実装。
	// 将来的にはFFmpegの再生クラスに移行するのでWindows依存でok
	[SingletonService]
	sealed class AudioPlayerManager : IDisposable
	{
		public static bool IsEnableSoundFadeEffect { get; set; } = true;

		#region ctor

		System.Windows.Media.MediaPlayer player;
		Action<Exception> on_failed = null;	// player.Open()後のplayer.MediaFailed
		Action on_opened = null;			// player.Open()後のplayer.MediaOpened

		public AudioPlayerManager()
		{
			if (Application.Current != null && Application.Current.Dispatcher != Dispatcher.CurrentDispatcher)
				throw new InvalidOperationException("call on UIThread!!");

			player = new System.Windows.Media.MediaPlayer();
			player.MediaEnded += delegate { OnPlayEnded(null); };
			player.MediaFailed += (_, x) => { on_failed?.Invoke(x.ErrorException); };
			player.MediaOpened += delegate { on_opened(); };
		}
		public void Dispose()
		{
			if (player != null)
			{
				Close();
				player = null;
			}
			on_failed = null;
			on_opened = null;
			IsPausedChanged = null;
			Opened = null;
			PlayEnded = null;
			PositionSetted = null;
			VolumeChanged = null;
		}

		#endregion
		#region Open/Close, Opened/PlayEnded, CurentOpenedPath,

		/// <summary>
		/// PlayFrom()を使用してファイルが正常にオープンされ、再生が開始されたら呼び出されます。
		/// </summary>
		public event Action Opened;

		/// <summary>
		/// PlayFrom()で現在開かれているファイル。
		/// Closeしたり、再生に失敗したときはnull。
		/// </summary>
		public string CurrentOpenedPath { get; private set; }

		/// <summary>
		/// ファイルの再生を中止して、ファイルを閉じます。
		/// </summary>
		public void Close()
		{
			App.Services.GetInstance<LogManager>().AddDebugLog("Call Close");
			if (string.IsNullOrWhiteSpace(CurrentOpenedPath) == false)
			{
				VolumeFadeoutWithPause();
				// closeでVolumeが元に戻っちゃうので閉じてから再設定
				var vol = player.Volume;
				player.Close();
//				DoDispatcherEvents();
				player.Volume = vol;
			}
			on_failed = null;
			on_opened = null;
			Duration = null;
			IsPaused = false;
			CurrentOpenedPath = null;
		}

		/// <summary>
		/// 指定ファイルの再生を開始します。
		/// 開始時の状態を指定することもできます。
		/// オープン完了するまでは時間差があるため、DurationやIsPausedの値はplay_started or Openedイベント後まで信用できません。
		/// 再生エラーの場合はOnPlayEndedイベントが理由付きで呼び出されます。
		/// </summary>
		/// <param name="path">null == Close()相当</param>
		/// <param name="isPause"></param>
		/// <param name="startPosition">null == TimeSpan.Zero相当</param>
		/// <param name="play_started">Openedイベントの前に呼ばれます</param>
		public void PlayFrom(string path, bool isPause, TimeSpan? startPosition, Action play_started)
		{
			App.Services.GetInstance<LogManager>().AddDebugLog($"Call Open: path={path ?? "(null)"}, isPause={isPause}, startPosition={startPosition.Value.ToString() ?? "(null)"}");
			Close();

			if (path == null) return;	// 停止状態へ
			if (!File.Exists(path)) { OnPlayEnded("ファイルがありません"); return; }	// 再生終了扱い
			//
			Duration = (TimeSpan?)null;
			IsPaused = isPause;
			CurrentOpenedPath = path;

			// error/event handling
			on_failed = (ex) => OnFailed_Handling(path, isPause, startPosition, play_started, ex);
			on_opened = () => OnOpened_Handling(isPause, startPosition, play_started);

			App.Services.GetInstance<LogManager>().AddDebugLog(" - opening file...");
			player.Open(new Uri(path));
		}
		void OnFailed_Handling(string path, bool isPause, TimeSpan? startPosition, Action play_started, Exception ex)
		{
			var ext = Path.GetExtension(path);
			if (string.IsNullOrEmpty(ext)) { ext = "."; }   // 拡張子無しはピリオドのみ
			App.Services.GetInstance<LogManager>().AddDebugLog($" **open failed: extension[{ext}]");
			if (ex != null)
				App.Services.GetInstance<LogManager>().AddErrorLog(" **open failed exception: ", ex);
			//
			if (IsExistsWmpRegistryEntry(ext))
			{
				App.Services.GetInstance<LogManager>().AddDebugLog(" **WMP extension registry exists.");
				// レジストリ設定済みで再生失敗
				OnPlayEnded("再生に失敗しました");
			}
			else
			{
				// レジストリ設定をして再オープン
				SetWmpRegistryEntry(ext);
				App.Services.GetInstance<LogManager>().AddDebugLog(" **WMP extension registry added, retry.");
				PlayFrom(path, isPause, startPosition, play_started);
			}
		}
		void OnOpened_Handling(bool isPause, TimeSpan? startPosition, Action play_started)
		{
			on_failed = null; // 開けた合図
			App.Services.GetInstance<LogManager>().AddDebugLog(" **open success!");
			if (player.HasAudio == false)
			{
				App.Services.GetInstance<LogManager>().AddDebugLog(" -- no have audio... skip.");
				// 音声がないならスキップ
				OnPlayEnded("音声がないため再生がスキップされました");
				return;
			}

			player.Position = startPosition ?? TimeSpan.Zero;
			Duration = (player.NaturalDuration.HasTimeSpan) ? player.NaturalDuration.TimeSpan : (TimeSpan?)null;
			IsPaused = isPause;
			App.Services.GetInstance<LogManager>().AddDebugLog($" **media duration: {Duration.Value.ToString() ?? "(null)"}");
//			DoDispatcherEvents();
//			Thread.Sleep(1);

			if (isPause) { player.Pause(); }
			else if (startPosition.HasValue) { VolumeFadeinWithPlay(); }
			else { player.Play(); }
//			DoDispatcherEvents();
//			Thread.Sleep(1);

			play_started?.Invoke();
			Opened?.Invoke();
		}

		#endregion
		#region Duration/Position/Volume/IsPaused,

		/// <summary>
		/// メディアの長さ。
		/// 取得できないこともある。
		/// 値はPlayFrom()直後ではなく、Openedイベント直前に設定される。
		/// </summary>
		public TimeSpan? Duration { get; private set; }

		/// <summary>
		/// Positionを(手動で)セットした
		/// </summary>
		public event Action PositionSetted;

		/// <summary>
		/// メディアの現在の位置を取得
		/// </summary>
		public TimeSpan Position
		{
			get
			{
				return player.Position;
			}
			set
			{
				App.Services.GetInstance<LogManager>().AddDebugLog($"Set Position: {value}");
				player.Position = value;
				PositionSetted?.Invoke();
			}
		}

		/// <summary>
		/// ボリュームが変更された
		/// </summary>
		public event Action VolumeChanged;

		/// <summary>
		/// 現在のボリュームを0.0-1.0の間で取得。
		/// </summary>
		public double Volume
		{
			get
			{
				return player.Volume;
			}
			set
			{
				App.Services.GetInstance<LogManager>().AddDebugLog($"Set Volume: {value}");
				if (player.Volume == value) return;
				player.Volume = value;
				VolumeChanged?.Invoke();
			}
		}

		/// <summary>
		/// IsPausedが変化した
		/// </summary>
		public event Action IsPausedChanged;

		bool __is_paused_value;
		public bool IsPaused
		{
			get { return __is_paused_value; }
			private set
			{
				if (__is_paused_value == value) return;
				__is_paused_value = value;
				IsPausedChanged?.Invoke();
			}
		}

		/// <summary>
		/// 現在開いているメディアにビデオがあるか
		/// </summary>
		public bool HasVideo
		{
			get
			{
				return player.HasVideo;
			}
		}

		/// <summary>
		/// ビデオを描画するためのDrawingBrushを取得します。
		/// ビデオがない場合はnullが返ります。
		/// </summary>
		/// <returns></returns>
		public DrawingBrush GetVideoBrush()
		{
			return player.HasVideo ?
				new DrawingBrush(new VideoDrawing()
				{
					Player = player,
					Rect = new Rect(0, 0, player.NaturalVideoWidth, player.NaturalVideoHeight),
				}) :
				null;
		}

		#endregion
		#region PlayEnded, PlayPause,

		/// <summary>
		/// 再生が終了したときに発生するイベント。
		/// 再生エラー時にも呼び出されます。
		/// </summary>
		public event Action<PlayEndedEventArgs> PlayEnded;

		void OnPlayEnded(string error_reason)
		{
			App.Services.GetInstance<LogManager>().AddDebugLog($" **play ended, ErrorReason={error_reason}");
			PlayEnded?.Invoke(new PlayEndedEventArgs() { ErrorReason = error_reason, });
			Close();
		}

		/// <summary>
		/// 再生/一時停止切り替え。
		/// ボリュームのフェード効果がつきます。
		/// </summary>
		public void PlayPause()
		{
			App.Services.GetInstance<LogManager>().AddDebugLog("Call PlayPause");

			// player.Open()直後、on_opened()実行前までに呼ばれると停止しないので操作を無視する
			if (on_failed != null)
			{
				App.Services.GetInstance<LogManager>().AddDebugLog(" - suspess.(on_failed != null)");
				return;
			}

			if (IsPaused)
				VolumeFadeinWithPlay();
			else
				VolumeFadeoutWithPause();
		}

		/// <summary>
		/// 頭から再生。
		/// Position==0相当だが、フェード効果がつきます。
		/// </summary>
		public void Replay()
		{
			App.Services.GetInstance<LogManager>().AddDebugLog("Call Replay");

			var paused = IsPaused;
			if (!paused)
				VolumeFadeoutWithPause();
			Position = TimeSpan.Zero;
			player.Play();
			IsPaused = false;
		}

		#endregion
		#region Helper

		// WinFormsのDoEventsライクな...
		[Obsolete]
		static void DoDispatcherEvents()
		{
		//	Application.Current.UIThreadInvoke(() => System.Windows.Forms.Application.DoEvents());
		}

		// 停止状態からフェードイン再生
		// UIThreadから呼ぶこと
		void VolumeFadeinWithPlay()
		{
			App.Services.GetInstance<LogManager>().AddDebugLog("Call VolumeFadeinWithPlay");
			var vol = player.Volume;
			App.Services.GetInstance<LogManager>().AddDebugLog($" - current Volume(1): {vol}");
			player.Volume = 0;
			//			DoDispatcherEvents();
			App.Services.GetInstance<LogManager>().AddDebugLog($" - current Volume(2): {player.Volume}");
			player.Play();
			IsPaused = false;
//			DoDispatcherEvents();
			Thread.Sleep(10);
			ValueAnimation_Wait(
				0, vol, TimeSpan.FromMilliseconds(300),
				v =>
				{
					player.Volume = v;
//					DoDispatcherEvents();
				},
				() =>
				{
//					DoDispatcherEvents();
					player.Volume = vol;
					//					DoDispatcherEvents();
					//					Thread.Sleep(1);
					App.Services.GetInstance<LogManager>().AddDebugLog($"VolumeFadeinWithPlay complate: vol({vol})==player.Volume({player.Volume})");
				});
		}

		// フェードアウト一時停止
		// UIThreadから呼ぶこと
		void VolumeFadeoutWithPause()
		{
			App.Services.GetInstance<LogManager>().AddDebugLog("Call VolumeFadeoutWithPause");
			var vol = player.Volume;
			App.Services.GetInstance<LogManager>().AddDebugLog($" - current Volume: {vol}");
			ValueAnimation_Wait(
				vol, 0, TimeSpan.FromMilliseconds(300),
				v =>
				{
					player.Volume = v;
//					DoDispatcherEvents();
				},
				() =>
				{
//					DoDispatcherEvents();
					player.Pause();
					IsPaused = true;
//					DoDispatcherEvents();
//					Thread.Sleep(10);
					player.Volume = vol;
					//					DoDispatcherEvents();
					App.Services.GetInstance<LogManager>().AddDebugLog($"VolumeFadeoutWithPause complate: vol({vol})==player.Volume({player.Volume})");
				});
		}

		// アニメーション用ヘルパ
		void ValueAnimation_Wait(double from, double to, TimeSpan duration, Action<double> tick, Action complate)
		{
			try
			{
				var sleepTime = duration.TotalMilliseconds / 10.0;
				App.Services.GetInstance<LogManager>().AddDebugLog($"ValueAnimation_Wait: IsEnableSoundFadeEffect={IsEnableSoundFadeEffect}, sleepTime={sleepTime}");
				if (IsEnableSoundFadeEffect)
				{
					var span = to - from;
					var step = span / 10.0;
					var current = from;
					for (var n = 0; n < 10; n++)
					{
						current = Math.Min(Math.Max(0.0, (current + step)), 1.0);
						if (tick != null)
						{
							try { tick(current); }
							catch { }
						}
						Thread.Sleep(Math.Max((int)sleepTime, 50));
					}
				}
			}
			finally
			{
				complate?.Invoke();
			}
		}

		// WMPのレジストリエントリがあるか確認する
		// 拡張子はピリオドつきで、拡張子がないものはピリオドのみで
		bool IsExistsWmpRegistryEntry(string ext)
		{
			using (var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\MediaPlayer\Player\Extensions\" + ext))
			{
				return (regKey != null);
			}
		}

		// WMPのレジストリエントリを追加する
		void SetWmpRegistryEntry(string ext)
		{
			using (var regKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\MediaPlayer\Player\Extensions\" + ext))
			{
				// 既存の設定を上書きしないように、存在しない場合のみ追加する
				if (regKey.GetValue("Permissions") == null)
				{
					regKey.SetValue("Permissions", 1, RegistryValueKind.DWord);
					App.Services.GetInstance<LogManager>().AddInfoLog($@"レジストリキーを設定しました: {regKey.Name}\Permissions = 1");
				}
				if (regKey.GetValue("Runtime") == null)
				{
					regKey.SetValue("Runtime", 7, RegistryValueKind.DWord);
					App.Services.GetInstance<LogManager>().AddInfoLog($@"レジストリキーを設定しました: {regKey.Name}\Runtime = 7");
				}
			}
		}

		public class PlayEndedEventArgs : EventArgs
		{
			// エラーの原因 (nullならエラーなし)
			public string ErrorReason;
		}

		#endregion

	}

	static class AudioPlayerManagerExtensions
	{
		public static IObservable<Unit> OpenedAsObservable(this AudioPlayerManager manager)
			=> Observable.FromEvent(v => manager.Opened += v, v => manager.Opened -= v);

		public static IObservable<Unit> PositionSettedAsObservable(this AudioPlayerManager manager)
			=> Observable.FromEvent(v => manager.PositionSetted += v, v => manager.PositionSetted -= v);

		public static IObservable<Unit> VolumeChangedAsObservable(this AudioPlayerManager manager)
			=> Observable.FromEvent(v => manager.VolumeChanged += v, v => manager.VolumeChanged -= v);

		public static IObservable<Unit> IsPausedChangedAsObservable(this AudioPlayerManager manager)
			=> Observable.FromEvent(v => manager.IsPausedChanged += v, v => manager.IsPausedChanged -= v);

		public static IObservable<AudioPlayerManager.PlayEndedEventArgs> PlayEndedAsObservable(this AudioPlayerManager manager)
			=> Observable.FromEvent<AudioPlayerManager.PlayEndedEventArgs>(v => manager.PlayEnded += v, v => manager.PlayEnded -= v);
	}

}
