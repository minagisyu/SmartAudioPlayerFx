using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SmartAudioPlayer
{
	/// <summary>
	/// 音楽を再生するためのプレーヤー
	/// 内部でWPFのMediaPlayerクラスを利用するが、再生エラー時にレジストリ追加してリトライするなどの機能を実装。
	/// </summary>
	[Standalone, UIThread]
	public sealed class AudioPlayerManager : IDisposable
	{
		public static bool IsEnableSoundFadeEffect { get; set; }

		static AudioPlayerManager()
		{
			IsEnableSoundFadeEffect = true;
		}

		#region ctor

		readonly MediaPlayer player;
		Action<Exception> on_failed = null;	// player.Open()後のplayer.MediaFailed
		Action on_opened = null;			// player.Open()後のplayer.MediaOpened

		public AudioPlayerManager()
		{
			if (Application.Current != null && Application.Current.Dispatcher != Dispatcher.CurrentDispatcher)
				throw new InvalidOperationException("call on UIThread!!");

			player = new MediaPlayer();
			player.MediaEnded += delegate { OnPlayEnded(null); };
			player.MediaFailed += (_, x) => { if (on_failed != null) { on_failed(x.ErrorException); } };
			player.MediaOpened += delegate { on_opened(); };
		}
		public void Dispose()
		{
			Close();

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
			Logger.AddDebugLog("Call Close");
			if (string.IsNullOrWhiteSpace(CurrentOpenedPath) == false)
			{
				VolumeFadeoutWithPause();
				// closeでVolumeが元に戻っちゃうので閉じてから再設定
				var vol = player.Volume;
				player.Close();
				DoDispatcherEvents();
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
			Logger.AddDebugLog("Call Open: path={0}, isPause={1}, startPosition={2}", path ?? "(null)", isPause, startPosition.HasValue ? startPosition.Value.ToString() : "(null)");
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

			Logger.AddDebugLog(" - opening file...");
			player.Open(new Uri(path));
		}
		void OnFailed_Handling(string path, bool isPause, TimeSpan? startPosition, Action play_started, Exception ex)
		{
			var ext = Path.GetExtension(path);
			if (string.IsNullOrEmpty(ext)) { ext = "."; }	// 拡張子無しはピリオドのみ
			Logger.AddDebugLog(" **open failed: extension[{0}]", ext);
			if (ex != null)
				Logger.AddErrorLog(" **open failed exception: ", ex);
			//
			if (IsExistsWmpRegistryEntry(ext))
			{
				Logger.AddDebugLog(" **WMP extension registry exists.");
				// レジストリ設定済みで再生失敗
				OnPlayEnded("再生に失敗しました");
			}
			else
			{
				// レジストリ設定をして再オープン
				SetWmpRegistryEntry(ext);
				Logger.AddDebugLog(" **WMP extension registry added, retry.");
				PlayFrom(path, isPause, startPosition, play_started);
			}
		}
		void OnOpened_Handling(bool isPause, TimeSpan? startPosition, Action play_started)
		{
			on_failed = null; // 開けた合図
			Logger.AddDebugLog(" **open success!");
			if (player.HasAudio == false)
			{
				Logger.AddDebugLog(" -- no have audio... skip.");
				// 音声がないならスキップ
				OnPlayEnded("音声がないため再生がスキップされました");
				return;
			}

			player.Position = startPosition ?? TimeSpan.Zero;
			Duration = (player.NaturalDuration.HasTimeSpan) ? player.NaturalDuration.TimeSpan : (TimeSpan?)null;
			IsPaused = isPause;
			Logger.AddDebugLog(" **media duration: {0}", Duration.HasValue ? Duration.Value.ToString() : "(null)");
			DoDispatcherEvents();
			Thread.Sleep(1);

			if (isPause) { player.Pause(); }
			else if (startPosition.HasValue) { VolumeFadeinWithPlay(); }
			else { player.Play(); }
			DoDispatcherEvents();
			Thread.Sleep(1);

			if (play_started != null)
				play_started();
			if (Opened != null)
				Opened();
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
				Logger.AddDebugLog("Set Position: {0}", value);
				player.Position = value;
				if (PositionSetted != null)
					PositionSetted();
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
				Logger.AddDebugLog("Set Volume: {0}", value);
				if (player.Volume == value) return;
				player.Volume = value;
				if (VolumeChanged != null)
					VolumeChanged();
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
				if (IsPausedChanged != null)
					IsPausedChanged();
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
			Logger.AddDebugLog(" **play ended, ErrorReason={0}", error_reason);
			if (PlayEnded != null)
				PlayEnded(new PlayEndedEventArgs() { ErrorReason = error_reason, });
			else
				Close();
		}

		/// <summary>
		/// 再生/一時停止切り替え。
		/// ボリュームのフェード効果がつきます。
		/// </summary>
		public void PlayPause()
		{
			Logger.AddDebugLog("Call PlayPause");

			// player.Open()直後、on_opened()実行前までに呼ばれると停止しないので操作を無視する
			if (on_failed != null)
			{
				Logger.AddDebugLog(" - suspess.(on_failed != null)");
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
			Logger.AddDebugLog("Call Replay");

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
		static void DoDispatcherEvents()
		{
			new Action(() => System.Windows.Forms.Application.DoEvents())
				.UIThreadInvoke();
		}

		// 停止状態からフェードイン再生
		// UIThreadから呼ぶこと
		void VolumeFadeinWithPlay()
		{
			Logger.AddDebugLog("Call VolumeFadeinWithPlay");
			var vol = player.Volume;
			Logger.AddDebugLog(" - current Volume(1): {0}", vol);
			player.Volume = 0;
			DoDispatcherEvents();
			Logger.AddDebugLog(" - current Volume(2): {0}", player.Volume);
			player.Play();
			IsPaused = false;
			DoDispatcherEvents();
			Thread.Sleep(10);
			ValueAnimation_Wait(
				0, vol, TimeSpan.FromMilliseconds(300),
				v =>
				{
					player.Volume = v;
					DoDispatcherEvents();
				},
				() =>
				{
					DoDispatcherEvents();
					player.Volume = vol;
					DoDispatcherEvents();
					Thread.Sleep(1);
					Logger.AddDebugLog("VolumeFadeinWithPlay complate: vol({0})==player.Volume({1})", vol, player.Volume);
				});
		}

		// フェードアウト一時停止
		// UIThreadから呼ぶこと
		void VolumeFadeoutWithPause()
		{
			Logger.AddDebugLog("Call VolumeFadeoutWithPause");
			var vol = player.Volume;
			Logger.AddDebugLog(" - current Volume: {0}", vol);
			ValueAnimation_Wait(
				vol, 0, TimeSpan.FromMilliseconds(300),
				v =>
				{
					player.Volume = v;
					DoDispatcherEvents();
				},
				() =>
				{
					DoDispatcherEvents();
					player.Pause();
					IsPaused = true;
					DoDispatcherEvents();
					Thread.Sleep(10);
					player.Volume = vol;
					DoDispatcherEvents();
					Logger.AddDebugLog("VolumeFadeoutWithPause complate: vol({0})==player.Volume({1})", vol, player.Volume);
				});
		}

		// アニメーション用ヘルパ
		void ValueAnimation_Wait(double from, double to, TimeSpan duration, Action<double> tick, Action complate)
		{
			try
			{
				var sleepTime = duration.TotalMilliseconds / 10.0;
				Logger.AddDebugLog("ValueAnimation_Wait: IsEnableSoundFadeEffect={0}, sleepTime={1}",
					IsEnableSoundFadeEffect, sleepTime);
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
				if (complate != null)
					complate();
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
					Logger.AddInfoLog(@"レジストリキーを設定しました: {0}\Permissions = 1", regKey.Name);
				}
				if (regKey.GetValue("Runtime") == null)
				{
					regKey.SetValue("Runtime", 7, RegistryValueKind.DWord);
					Logger.AddInfoLog(@"レジストリキーを設定しました: {0}\Runtime = 7", regKey.Name);
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

	public static class AudioPlayerManagerExtensions
	{
		public static IObservable<Unit> OpenedAsObservable(this AudioPlayerManager manager)
		{
			return Observable.FromEvent(v => manager.Opened += v, v => manager.Opened -= v);
		}
		public static IObservable<Unit> PositionSettedAsObservable(this AudioPlayerManager manager)
		{
			return Observable.FromEvent(v => manager.PositionSetted += v, v => manager.PositionSetted -= v);
		}
		public static IObservable<Unit> VolumeChangedAsObservable(this AudioPlayerManager manager)
		{
			return Observable.FromEvent(v => manager.VolumeChanged += v, v => manager.VolumeChanged -= v);
		}
		public static IObservable<Unit> IsPausedChangedAsObservable(this AudioPlayerManager manager)
		{
			return Observable.FromEvent(v => manager.IsPausedChanged += v, v => manager.IsPausedChanged -= v);
		}
		public static IObservable<AudioPlayerManager.PlayEndedEventArgs> PlayEndedAsObservable(this AudioPlayerManager manager)
		{
			return Observable.FromEvent<AudioPlayerManager.PlayEndedEventArgs>(v => manager.PlayEnded += v, v => manager.PlayEnded -= v);
		}
	}

}
