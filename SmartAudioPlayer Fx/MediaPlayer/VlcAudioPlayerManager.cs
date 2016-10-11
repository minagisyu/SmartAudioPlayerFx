using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Media;
using Quala;
using Vlc.DotNet.Core;
using System.Reflection;

namespace SmartAudioPlayerFx.MediaPlayer
{
	public class PlayEndedEventArgs : EventArgs
	{
		// エラーの原因 (nullならエラーなし)
		public string ErrorReason;
	}

	/// <summary>
	/// 音楽を再生するためのプレーヤー
	/// 内部でWPFのMediaPlayerクラスを利用するが、再生エラー時にレジストリ追加してリトライするなどの機能を実装。
	/// </summary>
	//	[Standalone]
	public sealed class VlcAudioPlayerManager : IDisposable
	{
		public static bool IsEnableSoundFadeEffect { get; set; } = true;

		#region ctor

		VlcMediaPlayer player;
		Action on_failed = null;	// player.Open()後のplayer.MediaFailed

		public VlcAudioPlayerManager()
		{
			DirectoryInfo vlcLibDirectory = null;
			var currentAssembly = Assembly.GetExecutingAssembly();
			var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
			if (currentDirectory != null)
			{
				vlcLibDirectory =
					(AssemblyName.GetAssemblyName(currentAssembly.Location).ProcessorArchitecture == ProcessorArchitecture.X86) ?
					new DirectoryInfo(Path.Combine(currentDirectory, @"x86\")) :
					new DirectoryInfo(Path.Combine(currentDirectory, @"x64\"));
			}
			player = new VlcMediaPlayer(vlcLibDirectory);
			player.EndReached += delegate { OnPlayEnded(null); };
            player.EncounteredError += delegate { on_failed?.Invoke(); };
		}
		public void Dispose()
		{
			Close();

			on_failed = null;
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
			App.Models.Get<Logging>().AddDebugLog("Call Close");
			if (string.IsNullOrWhiteSpace(CurrentOpenedPath) == false)
			{
				VolumeFadeoutWithPause();
				// closeでVolumeが元に戻っちゃうので閉じてから再設定
				var vol = player.Audio.Volume;
                player.Stop();
				player.Audio.Volume = vol;
			}
			on_failed = null;
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
			App.Models.Get<Logging>().AddDebugLog("Call Open: path={0}, isPause={1}, startPosition={2}", path ?? "(null)", isPause, startPosition.HasValue ? startPosition.Value.ToString() : "(null)");
			Close();

			if (path == null) return;	// 停止状態へ
			if (!File.Exists(path)) { OnPlayEnded("ファイルがありません"); return; }	// 再生終了扱い
			//
			Duration = (TimeSpan?)null;
			IsPaused = isPause;
			CurrentOpenedPath = path;

			// error/event handling
			on_failed = () => OnFailed_Handling(path, isPause, startPosition, play_started);

			App.Models.Get<Logging>().AddDebugLog(" - opening file...");
			var media = player.SetMedia(new FileInfo(path));
			media.ParsedChanged += delegate
			{
				OnOpened_Handling(isPause, startPosition, play_started);
			};
			media.ParseAsync();
		}
		void OnFailed_Handling(string path, bool isPause, TimeSpan? startPosition, Action play_started)
		{
			var ext = Path.GetExtension(path);
			if (string.IsNullOrEmpty(ext)) { ext = "."; }   // 拡張子無しはピリオドのみ
			App.Models.Get<Logging>().AddDebugLog(" **open failed: extension[{0}]", ext);
			//
			OnPlayEnded("再生に失敗しました");
		}
		void OnOpened_Handling(bool isPause, TimeSpan? startPosition, Action play_started)
		{
			on_failed = null; // 開けた合図
			App.Models.Get<Logging>().AddDebugLog(" **open success!");
			if (player.Audio.Tracks.Count > 0)
			{
				App.Models.Get<Logging>().AddDebugLog(" -- no have audio... skip.");
				// 音声がないならスキップ
				OnPlayEnded("音声がないため再生がスキップされました");
				return;
			}

		//	player.Position = startPosition ?? TimeSpan.Zero;
		//	Duration = (player.NaturalDuration.HasTimeSpan) ? player.NaturalDuration.TimeSpan : (TimeSpan?)null;
			IsPaused = isPause;
			App.Models.Get<Logging>().AddDebugLog(" **media duration: {0}", Duration.HasValue ? Duration.Value.ToString() : "(null)");

			if (isPause) { player.Pause(); }
			else if (startPosition.HasValue) { VolumeFadeinWithPlay(); }
			else { player.Play(); }

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
				return TimeSpan.Zero;// player.Position;
			}
			set
			{
				App.Models.Get<Logging>().AddDebugLog("Set Position: {0}", value);
			//	player.Position = value;
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
				return 0;// player.Volume;
			}
			set
			{
				App.Models.Get<Logging>().AddDebugLog("Set Volume: {0}", value);
			//	if (player.Volume == value) return;
			//	player.Volume = value;
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
				return false;// player.HasVideo;
			}
		}

		/// <summary>
		/// ビデオを描画するためのDrawingBrushを取得します。
		/// ビデオがない場合はnullが返ります。
		/// </summary>
		/// <returns></returns>
		public DrawingBrush GetVideoBrush()
		{
			return /*player.HasVideo ?
				new DrawingBrush(new VideoDrawing()
				{
					Player = player,
					Rect = new Rect(0, 0, player.NaturalVideoWidth, player.NaturalVideoHeight),
				}) :
				*/null;
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
			App.Models.Get<Logging>().AddDebugLog(" **play ended, ErrorReason={0}", error_reason);
			PlayEnded?.Invoke(new PlayEndedEventArgs() { ErrorReason = error_reason, });
			Close();
		}

		/// <summary>
		/// 再生/一時停止切り替え。
		/// ボリュームのフェード効果がつきます。
		/// </summary>
		public void PlayPause()
		{
			App.Models.Get<Logging>().AddDebugLog("Call PlayPause");

			// player.Open()直後、on_opened()実行前までに呼ばれると停止しないので操作を無視する
			if (on_failed != null)
			{
				App.Models.Get<Logging>().AddDebugLog(" - suspess.(on_failed != null)");
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
			App.Models.Get<Logging>().AddDebugLog("Call Replay");

			var paused = IsPaused;
			if (!paused)
				VolumeFadeoutWithPause();
			Position = TimeSpan.Zero;
		//	player.Play();
			IsPaused = false;
		}

		#endregion
		#region Helper


		// 停止状態からフェードイン再生
		// UIThreadから呼ぶこと
		void VolumeFadeinWithPlay()
		{
			App.Models.Get<Logging>().AddDebugLog("Call VolumeFadeinWithPlay");
		//	var vol = player.Volume;
		//	AppService.Log.AddDebugLog(" - current Volume(1): {0}", vol);
		//	player.Volume = 0;
//			DoDispatcherEvents();
		//	AppService.Log.AddDebugLog(" - current Volume(2): {0}", player.Volume);
		//	player.Play();
			IsPaused = false;
//			DoDispatcherEvents();
			Thread.Sleep(10);
			/*	ValueAnimation_Wait(
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
						AppService.Log.AddDebugLog("VolumeFadeinWithPlay complate: vol({0})==player.Volume({1})", vol, player.Volume);
					});
			*/
		}

		// フェードアウト一時停止
		// UIThreadから呼ぶこと
		void VolumeFadeoutWithPause()
		{
			App.Models.Get<Logging>().AddDebugLog("Call VolumeFadeoutWithPause");
		//	var vol = player.Volume;
		//	AppService.Log.AddDebugLog(" - current Volume: {0}", vol);
			/*	ValueAnimation_Wait(
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
						AppService.Log.AddDebugLog("VolumeFadeoutWithPause complate: vol({0})==player.Volume({1})", vol, player.Volume);
					});
			*/
		}

		// アニメーション用ヘルパ
		void ValueAnimation_Wait(double from, double to, TimeSpan duration, Action<double> tick, Action complate)
		{
			try
			{
				var sleepTime = duration.TotalMilliseconds / 10.0;
				App.Models.Get<Logging>().AddDebugLog("ValueAnimation_Wait: IsEnableSoundFadeEffect={0}, sleepTime={1}",
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
				complate?.Invoke();
			}
		}

		#endregion

	}

	public static class VlcAudioPlayerManagerExtensions
	{
		public static IObservable<Unit> OpenedAsObservable(this VlcAudioPlayerManager manager)
		{
			return Observable.FromEvent(v => manager.Opened += v, v => manager.Opened -= v);
		}
		public static IObservable<Unit> PositionSettedAsObservable(this VlcAudioPlayerManager manager)
		{
			return Observable.FromEvent(v => manager.PositionSetted += v, v => manager.PositionSetted -= v);
		}
		public static IObservable<Unit> VolumeChangedAsObservable(this VlcAudioPlayerManager manager)
		{
			return Observable.FromEvent(v => manager.VolumeChanged += v, v => manager.VolumeChanged -= v);
		}
		public static IObservable<Unit> IsPausedChangedAsObservable(this VlcAudioPlayerManager manager)
		{
			return Observable.FromEvent(v => manager.IsPausedChanged += v, v => manager.IsPausedChanged -= v);
		}
		public static IObservable<PlayEndedEventArgs> PlayEndedAsObservable(this VlcAudioPlayerManager manager)
		{
			return Observable.FromEvent<PlayEndedEventArgs>(v => manager.PlayEnded += v, v => manager.PlayEnded -= v);
		}
	}

}
