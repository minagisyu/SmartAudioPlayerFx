using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Quala;

namespace SmartAudioPlayerFx.Player
{
	/// <summary>
	/// 音楽を再生するためのプレーヤー
	/// 内部でWPFのMediaPlayerクラスを利用するが、再生エラー時にレジストリ追加してリトライするなどの機能を実装。
	/// </summary>
	sealed class AudioPlayer
	{
		MediaPlayer player;
		Action on_failed;	// player.Open()後のplayer.MediaFailed
		Action on_opened;	// player.Open()後のplayer.MediaOpened

		public AudioPlayer()
		{
			player = new MediaPlayer();
			player.MediaFailed += delegate { if (on_failed != null) { on_failed(); } };
			player.MediaOpened += delegate { if (on_opened != null) { on_opened(); } };
			player.MediaEnded += delegate { OnPlayEnded(null); };
		}

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
			LogService.AddDebugLog("AudioPlayer", "Call Close");
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
			LogService.AddDebugLog("AudioPlayer", "Call Open: path={0}, isPause={1}, startPosition={2}", path ?? "(null)", isPause, startPosition.HasValue ? startPosition.Value.ToString() : "(null)");
			Close();

			if (path == null) return;	// 停止状態へ
			if (!File.Exists(path)) { OnPlayEnded("ファイルがありません"); return; }	// 再生終了扱い
			//
			Duration = (TimeSpan?)null;
			IsPaused = isPause;
			CurrentOpenedPath = path;

			// error/event handling
			on_failed = () => OnFailed_Handling(path, isPause, startPosition, play_started);
			on_opened = () => OnOpened_Handling(isPause, startPosition, play_started);

			LogService.AddDebugLog("AudioPlayer", " - opening file...");
			player.Open(new Uri(path));
		}
		void OnFailed_Handling(string path, bool isPause, TimeSpan? startPosition, Action play_started)
		{
			var ext = Path.GetExtension(path);
			if (string.IsNullOrEmpty(ext)) { ext = "."; }	// 拡張子無しはピリオドのみ
			LogService.AddDebugLog("AudioPlayer", " **open failed: extension[{0}]", ext);
			//
			if (IsExistsWmpRegistryEntry(ext))
			{
				LogService.AddDebugLog("AudioPlayer", " **WMP extension registry exists.");
				// レジストリ設定済みで再生失敗
				OnPlayEnded("再生に失敗しました");
			}
			else
			{
				// レジストリ設定をして再オープン
				SetWmpRegistryEntry(ext);
				LogService.AddDebugLog("AudioPlayer", " **WMP extension registry added, retry.");
				PlayFrom(path, isPause, startPosition, play_started);
			}
		}
		void OnOpened_Handling(bool isPause, TimeSpan? startPosition, Action play_started)
		{
			on_failed = null; // 開けた合図
			LogService.AddDebugLog("AudioPlayer", " **open success!");
			if (player.HasAudio == false)
			{
				LogService.AddDebugLog("AudioPlayer", " -- no have audio... skip.");
				// 音声がないならスキップ
				OnPlayEnded("音声がないため再生がスキップされました");
				return;
			}

			player.Position = startPosition ?? TimeSpan.Zero;
			Duration = (player.NaturalDuration.HasTimeSpan) ? player.NaturalDuration.TimeSpan : (TimeSpan?)null;
			IsPaused = isPause;
			LogService.AddDebugLog("AudioPlayer", " **media duration: {0}", Duration.HasValue ? Duration.Value.ToString() : "(null)");

			if (isPause) { player.Pause(); }
			else if (startPosition.HasValue) { VolumeFadeinWithPlay(); }
			else { player.Play(); }

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
		/// メディアの現在の位置を取得
		/// </summary>
		public TimeSpan Position { get { return player.Position; } }
		/// <summary>
		/// メディアの現在の位置を設定
		/// </summary>
		/// <param name="value"></param>
		public void SetPosition(TimeSpan value)
		{
			LogService.AddDebugLog("AudioPlayer", "Set Position: {0}", value);
			player.Position = value;
		}

		/// <summary>
		/// ボリュームが変更された
		/// </summary>
		public event Action VolumeChanged;
		/// <summary>
		/// 現在のボリュームを0.0-1.0の間で取得。
		/// </summary>
		public double Volume { get { return player.Volume; } }
		/// <summary>
		/// ボリュームを設定 (0.0-1.0)
		/// </summary>
		/// <param name="value"></param>
		public void SetVolume(double value)
		{
			LogService.AddDebugLog("AudioPlayer", "Set Volume: {0}", value);
			player.Volume = value;
			if (VolumeChanged != null)
				VolumeChanged();
		}

		/// <summary>
		/// IsPausedが変化した
		/// </summary>
		public event Action IsPausedChanged;
		/// <summary>
		/// 今ポーズ中？
		/// </summary>
		public bool IsPaused { get; private set; }
		/// <summary>
		/// ポーズ状態を設定
		/// </summary>
		/// <param name="value"></param>
		void SetIsPaused(bool value)
		{
			IsPaused = value;
			if (IsPausedChanged != null)
				IsPausedChanged();
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
			LogService.AddDebugLog("AudioPlayer", " **play ended, ErrorReason={0}", error_reason);
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
			LogService.AddDebugLog("AudioPlayer", "Call PlayPause");
			// player.Open()直後、on_opened()実行前までに呼ばれると停止しないので操作を無視する
			if (on_failed != null)
			{
				LogService.AddDebugLog("AudioPlayer", " - suspess.(on_failed != null)");
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
			LogService.AddDebugLog("AudioPlayer", "Call Replay");
			var paused = IsPaused;
			if (!paused)
				VolumeFadeoutWithPause();
			SetPosition(TimeSpan.Zero);
			player.Play();
			SetIsPaused(false);
		}

		#endregion
		#region Helper

		// WinFormsのDoEventsライクな...
		static void DoDispatcherEvents()
		{
			Dispatcher.PushFrame(new DispatcherFrame() { Continue = false, });
		}

		// 停止状態からフェードイン再生
		// UIThreadから呼ぶこと
		void VolumeFadeinWithPlay()
		{
			LogService.AddDebugLog("AudioPlayer", "Call VolumeFadeinWithPlay");
			var vol = player.Volume;
			player.Volume = 0;
			player.Play();
			SetIsPaused(false);
		//	Thread.Sleep(10);	// Playが完了する前にVolumeが設定されるのを阻止
			DoDispatcherEvents();
			ValueAnimation_Wait(0, vol, TimeSpan.FromMilliseconds(100),
				(v) =>
				{
					player.Volume = v;
					DoDispatcherEvents();
				},
				() =>
				{
					// ディスパッチャが動いていないせいで反映しない場合があるみたいなので、念を押してBeginInvoke使って遅延設定。
					player.Volume = vol;
					DoDispatcherEvents();
					player.Volume = vol;
					DoDispatcherEvents();
				}
			);
		}

		// フェードアウト一時停止
		// UIThreadから呼ぶこと
		void VolumeFadeoutWithPause()
		{
			LogService.AddDebugLog("AudioPlayer", "Call VolumeFadeoutWithPause");
			var vol = player.Volume;
			ValueAnimation_Wait(vol, 0, TimeSpan.FromMilliseconds(100),
				(v) =>
				{
					player.Volume = v;
					DoDispatcherEvents();
				},
				() =>
				{
					player.Pause();
					SetIsPaused(true);
					DoDispatcherEvents();
				//	Thread.Sleep(10);	// Pauseが完了する前にVolumeが設定されるのを阻止
					// ディスパッチャが動いていないせいで反映しない場合があるみたいなので、念を押してBeginInvoke使って遅延設定。
					player.Volume = vol;
					DoDispatcherEvents();
					player.Volume = vol;
					DoDispatcherEvents();
				});
		}

		// アニメーション用ヘルパ
		static void ValueAnimation_Wait(double from, double to, TimeSpan duration, Action<double> tick, Action complate)
		{
			try
			{
				var span = to - from;
				var step = span / duration.TotalMilliseconds;
				var current = from;
				for (var n = 0.0; n < duration.TotalMilliseconds; n++)
				{
					current += step;
					if (tick != null)
						tick(current);
					Thread.Sleep(1);
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
		static bool IsExistsWmpRegistryEntry(string ext)
		{
			using (var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\MediaPlayer\Player\Extensions\"+ext))
			{
				return (regKey != null);
			}
		}

		// WMPのレジストリエントリを追加する
		static void SetWmpRegistryEntry(string ext)
		{
			using (var regKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\MediaPlayer\Player\Extensions\" + ext))
			{
				// 既存の設定を上書きしないように、存在しない場合のみ追加する
				if (regKey.GetValue("Permissions") == null)
				{
					regKey.SetValue("Permissions", 1, RegistryValueKind.DWord);
					LogService.AddInfoLog("AudioPlayer", @"レジストリキーを設定しました: {0}\Permissions = 1", regKey.Name);
				}
				if (regKey.GetValue("Runtime") == null)
				{
					regKey.SetValue("Runtime", 7, RegistryValueKind.DWord);
					LogService.AddInfoLog("AudioPlayer", @"レジストリキーを設定しました: {0}\Runtime = 7", regKey.Name);
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
}

