using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Codeplex.Reactive;
using Codeplex.Reactive.Extensions;
using SmartAudioPlayer;
using SmartAudioPlayerFx.Managers;

namespace SmartAudioPlayerFx.Views
{
	sealed class MainWindowViewModel
	{
		public IObservable<Unit> Initialized { get; private set; }
		#region Properties

		public ReactiveProperty<Int32Rect> WindowPlacement { get; private set; }
		public ReactiveProperty<double> InactiveOpacity { get; private set; }
		public ReactiveProperty<double> DeactiveOpacity { get; private set; }
		public ReactiveProperty<bool> IsVisible { get; private set; }	// 機能未実装...
		public ReactiveCommand PlayPauseCommand { get; private set; }
		//
		public ReactiveProperty<bool> IsLoading { get; private set; }
		public ReactiveProperty<JukeboxManager.SelectionMode> SelectMode { get; private set; }
		public ReactiveProperty<bool> IsRepeat { get; private set; }
		public ReactiveProperty<bool> IsPaused { get; private set; }
		public ReactiveProperty<MediaItem> CurrentMedia { get; private set; }
		public ReactiveProperty<long> PositionTicks { get; private set; }
		public ReactiveProperty<long> DurationTicks { get; private set; }
		public ReactiveProperty<double> Volume { get; private set; }
		//
		public ReactiveProperty<string> SelectModeTooltip { get; private set; }
		public ReactiveCommand SelectModeToggleCommand { get; private set; }
		//
		public ReactiveProperty<string> RepeatTooltip { get; private set; }
		public ReactiveCommand RepeatToggleCommand { get; private set; }
		//
		public ReactiveProperty<string> StateTooltip { get; private set; }
		public ReactiveCommand StateToggleCommand { get; private set; }
		//
		public ReactiveProperty<string> Title { get; private set; }
		public ReactiveProperty<string> TitleTooltip { get; private set; }
		public ReactiveProperty<bool> TitleTooltipEnable { get; private set; }
		public ReactiveCommand TitleSkipCommand { get; private set; }
		//
		public ReactiveProperty<string> VolumeLevel { get; private set; }
		public ReactiveProperty<string> VolumeTooltip { get; private set; }
		//
		public ReactiveProperty<string> PositionString { get; private set; }
		public ReactiveProperty<string> SeekTooltip { get; private set; }

		#endregion

		public MainWindowViewModel()
		{
			WindowPlacement = new ReactiveProperty<Int32Rect>(new Int32Rect(0, 0, 0, 0));
			InactiveOpacity = new ReactiveProperty<double>(0.8);
			DeactiveOpacity = new ReactiveProperty<double>(0.65);
			IsVisible = new ReactiveProperty<bool>(true);
			PlayPauseCommand = new ReactiveCommand();

			// Common Property
			IsLoading = new ReactiveProperty<bool>(false);
			SelectMode = new ReactiveProperty<JukeboxManager.SelectionMode>(JukeboxManager.SelectionMode.Random);
			IsRepeat = new ReactiveProperty<bool>(false);
			IsPaused = new ReactiveProperty<bool>();
			CurrentMedia = new ReactiveProperty<MediaItem>();
			PositionTicks = new ReactiveProperty<long>();
			DurationTicks = new ReactiveProperty<long>();
			Volume = new ReactiveProperty<double>();

			// Sub Property
			SelectModeTooltip = new ReactiveProperty<string>();
			SelectModeToggleCommand = new ReactiveCommand();
			RepeatTooltip = new ReactiveProperty<string>();
			RepeatToggleCommand = new ReactiveCommand();
			StateTooltip = new ReactiveProperty<string>();
			StateToggleCommand = new ReactiveCommand();
			Title = new ReactiveProperty<string>();
			TitleTooltip = new ReactiveProperty<string>();
			TitleTooltipEnable = new ReactiveProperty<bool>();
			TitleSkipCommand = new ReactiveCommand();
			VolumeLevel = new ReactiveProperty<string>();
			VolumeTooltip = new ReactiveProperty<string>();
			PositionString = new ReactiveProperty<string>();
			SeekTooltip = new ReactiveProperty<string>();

			Initialized = Observable.Start(() =>
			{
				// Preferences
				ManagerServices.PreferencesManager.WindowSettings
					.Subscribe(x => OnLoadWindowPrefernces(x));
				ManagerServices.PreferencesManager.SerializeRequestAsObservable()
					.Subscribe(_ => OnSavePreferences());

				// Setup Events
				PlayPauseCommand
					.Subscribe(_ => ManagerServices.AudioPlayerManager.PlayPause());

				// Common Property
				ManagerServices.JukeboxManager.IsServiceStarted
					.Subscribe(x => IsLoading.Value = !x);
				ManagerServices.JukeboxManager.SelectMode
					.Subscribe(x => SelectMode.Value = x);
				ManagerServices.JukeboxManager.IsRepeat
					.Subscribe(x => IsRepeat.Value = x);
				Observable.Merge(
					ManagerServices.AudioPlayerManager.OpenedAsObservable(),
					ManagerServices.AudioPlayerManager.IsPausedChangedAsObservable(),
					Observable.Return(Unit.Default))	// イベント来るまで動かないので初期設定用にReturnを返してやる
					.Subscribe(_ => IsPaused.Value = ManagerServices.AudioPlayerManager.IsPaused);
				ManagerServices.JukeboxManager.CurrentMedia
					.Subscribe(x => CurrentMedia.Value = x);
				Observable.Merge(
					ManagerServices.AudioPlayerManager.OpenedAsObservable(),
					ManagerServices.AudioPlayerManager.PositionSettedAsObservable(),
					Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1)).Select(_ => Unit.Default))
					.ObserveOnUIDispatcher()
					.Subscribe(_ => PositionTicks.Value = ManagerServices.AudioPlayerManager.Position.Ticks);
				ManagerServices.AudioPlayerManager.OpenedAsObservable()
					.Subscribe(_ => DurationTicks.Value = ManagerServices.AudioPlayerManager.Duration.HasValue ? ManagerServices.AudioPlayerManager.Duration.Value.Ticks : 0);
				App.Current.Dispatcher.Invoke(() =>
				{
					Observable.Merge(
						ManagerServices.AudioPlayerManager.VolumeChangedAsObservable(),
						Observable.Return(Unit.Default))
						.Subscribe(_ => Volume.Value = ManagerServices.AudioPlayerManager.Volume);
				});
				Volume
					 .ObserveOnUIDispatcher()
					 .Subscribe(x => ManagerServices.AudioPlayerManager.Volume = x);

				// SubProperty
				SelectMode
					.Subscribe(x => SelectModeTooltip.Value = SelectModeToTooltipString(x));
				SelectModeToggleCommand
					.Subscribe(_ =>
					{
						var newMode = (ManagerServices.JukeboxManager.SelectMode.Value == JukeboxManager.SelectionMode.Filename) ?
							JukeboxManager.SelectionMode.Random :
							JukeboxManager.SelectionMode.Filename;
						ManagerServices.JukeboxManager.SelectMode.Value = newMode;
					});
				IsRepeat
					.Subscribe(x => RepeatTooltip.Value = RepeatModeToTooltipString(x));
				RepeatToggleCommand
					.Subscribe(_ =>
					{
						var newRepeat = !ManagerServices.JukeboxManager.IsRepeat.Value;
						ManagerServices.JukeboxManager.IsRepeat.Value = newRepeat;
					});
				IsPaused
					.Subscribe(x => StateTooltip.Value = PlayStateToTooltipString(x));
				StateToggleCommand
					.Subscribe(_ => ManagerServices.AudioPlayerManager.PlayPause());
				Observable.Merge(
					MediaListItemViewModel._isTitleFromFilePathChangedAsObservable(),
					Observable.Return(Unit.Default))
					.Select(_ => MediaListItemViewModel.IsTitleFromFilePath)
					.CombineLatest(CurrentMedia, IsLoading,
						(x, y, z) => new { IsTitleFromFilePath = x, CurrentMedia = y, IsLoading = z, })
					.Subscribe(x => Title.Value = CurrentMediaToTitleString(x.CurrentMedia, x.IsLoading, x.IsTitleFromFilePath));
				CurrentMedia
					.CombineLatest(IsLoading, (x, y) => new { CurrentMedia = x, IsLoading = y, })
					.Subscribe(x => TitleTooltip.Value = CurrentMediaToTitleTooltipString(x.CurrentMedia, x.IsLoading));
				TitleTooltip
					.Subscribe(x => TitleTooltipEnable.Value = !string.IsNullOrWhiteSpace(x));
				TitleSkipCommand
					.Subscribe(_ => Task.Run(() => ManagerServices.JukeboxManager.SelectNext(true)));
				Volume
					.Subscribe(x =>
					{
						VolumeLevel.Value = VolumeToVolumeLevelString(x);
						VolumeTooltip.Value = VolumeToTooltipString(x);
					});
				PositionTicks
					.CombineLatest(DurationTicks, (x, y) => new { PositionTicks = x, DurationTicks = y, })
					.Subscribe(x => PositionString.Value = PositionToString(x.PositionTicks, x.DurationTicks));
				PositionString
					.CombineLatest(DurationTicks, (x, y) => new { PosString = x, DurTicks = y })
					.Subscribe(x => SeekTooltip.Value = PositionToTooltipString(x.PosString, x.DurTicks));
			});
		}

		void OnSavePreferences()
		{
			ManagerServices.PreferencesManager.WindowSettings.Value
				.SetAttributeValueEx("WindowPlacement", WindowPlacement.Value)
				.SetAttributeValueEx("InactiveOpacity", (int)(InactiveOpacity.Value * 100.0))
				.SetAttributeValueEx("DeactiveOpacity", (int)(DeactiveOpacity.Value * 100.0))
				.SetAttributeValueEx("IsVisible", IsVisible.Value);
		}
		void OnLoadWindowPrefernces(XElement windowSettings)
		{
			var wp = windowSettings.GetAttributeValueEx("WindowPlacement", Int32Rect.Empty);
			if (wp.IsEmpty)
			{
				// 以前の設定データであるDynamicWindowBoundsの取得を試みる
				windowSettings.SubElement("DynamicWindowBounds", false, el =>
				{
					wp = new Int32Rect(
						el.GetAttributeValueEx("RealLeft", 0),
						el.GetAttributeValueEx("RealTop", 0),
						el.GetAttributeValueEx("RealWidth", 0),
						el.GetAttributeValueEx("RealHeight", 0));
				});
			}
			if (wp.IsEmpty)
			{
				// デフォルト値の設定
				wp = MainWindow.GetDefaultWindowPosition();
			}

			WindowPlacement.Value = wp;
			InactiveOpacity.Value = windowSettings.GetAttributeValueEx("InactiveOpacity", 80) / 100.0;
			DeactiveOpacity.Value = windowSettings.GetAttributeValueEx("DeactiveOpacity", 65) / 100.0;
			IsVisible.Value = windowSettings.GetAttributeValueEx("IsVisible", true);
		}
		public void SavePreferences()
		{
			ManagerServices.PreferencesManager.Save();
		}

		public async Task JukeboxStart()
		{
			await ManagerServices.JukeboxManager.Start();
		}
		public void SetPlayerPosition(TimeSpan value)
		{
			ManagerServices.AudioPlayerManager.Position = value;
		}

		#region Value to String Convertion

		string SelectModeToTooltipString(JukeboxManager.SelectionMode mode)
		{
			return
				mode == JukeboxManager.SelectionMode.Filename ? "モード：ファイル名順" :
				mode == JukeboxManager.SelectionMode.Random ? "モード：ランダム" :
				string.Empty;
		}
		string RepeatModeToTooltipString(bool isRepeat)
		{
			return isRepeat ? "リピート：有効" : "リピート：無効";
		}
		string PlayStateToTooltipString(bool isPaused)
		{
			return isPaused ? "状態：一時停止中" : "状態：再生中";
		}

		string CurrentMediaToTitleString(MediaItem currentMedia, bool isLoading, bool isTitleFromFilePath)
		{
			return
				isLoading ? "[読み込み中です...]" :
				currentMedia == null ? "[再生可能なメディアがありません]" :
				isTitleFromFilePath ? Path.GetFileName(currentMedia.FilePath) :
				currentMedia.Title;
		}
		string CurrentMediaToTitleTooltipString(MediaItem currentMedia, bool isLoading)
		{
			if (isLoading || currentMedia == null) return null;

			var sb = new StringBuilder(currentMedia.Title);
			if (!string.IsNullOrEmpty(currentMedia.Artist)) { sb.AppendLine(); sb.Append(currentMedia.Artist); }
			if (!string.IsNullOrEmpty(currentMedia.Album)) { sb.AppendLine(); sb.Append(currentMedia.Album); }
			if (!string.IsNullOrEmpty(currentMedia.Comment)) { sb.AppendLine(); sb.AppendLine(); sb.Append(currentMedia.Comment); }
			return sb.ToString();
		}

		string VolumeToVolumeLevelString(double volume)
		{
			return
				(volume == 0.0) ? "Mute" :
				(volume > 0.7) ? "Hi" :
				(volume > 0.3) ? "Mid" :
				"Low";
		}
		string VolumeToTooltipString(double volume)
		{
			return string.Format("ボリューム：{0:F0}%", volume * 100.0);
		}

		string PositionToString(long positionTicks, long durationTicks)
		{
			if (durationTicks == 0)
			{
				return "00:00";
			}
			else
			{
				var pos = TimeSpan.FromTicks(positionTicks);
				return
					((pos.Hours > 0) ? pos.Hours.ToString("d2") + ":" : string.Empty) +
					pos.Minutes.ToString("d2") + ":" +
					pos.Seconds.ToString("d2");
			}
		}
		string PositionToTooltipString(string positionString, long durationTicks)
		{
			var dur = TimeSpan.FromTicks(durationTicks);
			var dur_string =
				((dur.Hours > 0) ? dur.Hours.ToString("d2") + ":" : string.Empty) +
				dur.Minutes.ToString("d2") + ":" +
				dur.Seconds.ToString("d2");
			return positionString + "/" + dur_string;
		}

		#endregion
	}
}
