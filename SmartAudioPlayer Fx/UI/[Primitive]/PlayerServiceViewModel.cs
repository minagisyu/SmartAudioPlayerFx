using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Quala;
using Quala.Windows.Mvvm;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.UI
{
	// Viewに必要なプロパティをServiceから
	sealed class PlayerServiceViewModel : ViewModel
	{
		public PlayerServiceViewModel()
		{
			State__ctor();
			CurrentMedia__ctor();
			Position__ctor();
			IsVideoDrawing = this.RegisterViewModelProperty(true);
		}

		#region Preferences

		public void SavePreferences()
		{
			var elm = PreferenceService.Load("data", "player.xml") ?? new XElement("Player");
			if (elm.Name != "Player") elm.Name = "Player";
			//
			elm.SetAttributeValue("IsVideoDrawing", IsVideoDrawing.Value);
			//
			JukeboxService.SavePreferencesAdd(elm);
			ShortcutKeyService.SavePreferencesAdd(elm);
			//
			PreferenceService.Save(elm, "data", "player.xml");
		}

		public void LoadPreferences()
		{
			var elm = PreferenceService.Load("data", "player.xml");
			if (elm == null || elm.Name.LocalName != "Player") elm = null;
			IsVideoDrawing.Value = elm.GetOrDefaultValue("IsVideoDrawing", true);
			JukeboxService.LoadPreferencesApply(elm);
			ShortcutKeyService.LoadPreferencesApply(elm);
		}

		#endregion

		#region Mode, Repeat, State

		void State__ctor()
		{
			// Mode
			JukeboxService.SelectModeChanged += delegate
			{
				OnPropertyChanged("SelectMode");
				OnPropertyChanged("SelectModeImageSource");
				OnPropertyChanged("SelectModeImageTooltip");
			};
			// Repeat
			JukeboxService.IsRepeatChanged += delegate
			{
				OnPropertyChanged("IsRepeat");
				OnPropertyChanged("RepeatImageOpacity");
				OnPropertyChanged("RepeatImageTooltip");
			};
			// State
			JukeboxService.AudioPlayer.IsPausedChanged += delegate
			{
				OnPropertyChanged("IsPaused");
				OnPropertyChanged("StateImageSource");
				OnPropertyChanged("StateImageTooltip");
			};
			JukeboxService.CurrentMediaChanged += delegate
			{
				OnPropertyChanged("IsPaused");
				OnPropertyChanged("StateImageSource");
				OnPropertyChanged("StateImageTooltip");
			};
		}

		// Mode
		public string SelectModeImageSource
		{
			get
			{
				return
					JukeboxService.SelectMode == JukeboxService.SelectionMode.Filename ? "/Resources/モード：シーケンシャル.png" :
					JukeboxService.SelectMode == JukeboxService.SelectionMode.Random ? "/Resources/モード：ランダム.png" :
					null;
			}
		}
		public string SelectModeImageTooltip
		{
			get
			{
				return
					JukeboxService.SelectMode == JukeboxService.SelectionMode.Filename ? "モード：ファイル名順" :
					JukeboxService.SelectMode == JukeboxService.SelectionMode.Random ? "モード：ランダム" :
					null;
			}
		}

		// Repeat
		public string RepeatImageSource { get { return "/Resources/モード：リピート.png"; } }
		public double RepeatImageOpacity { get { return (JukeboxService.IsRepeat) ? 1.0 : 0.4; } }
		public string RepeatImageTooltip { get { return (JukeboxService.IsRepeat) ? "リピート：有効" : "リピート：無効"; } }

		// State
		public string StateImageSource { get { return (JukeboxService.AudioPlayer.IsPaused) ? "/Resources/ステート：一時停止.png" : "/Resources/ステート：再生.png"; } }
		public string StateImageTooltip { get { return (!JukeboxService.AudioPlayer.IsPaused) ? "状態：再生中" : "状態：一時停止中"; } }

		#endregion
		#region CurrentMedia, Volume

		void CurrentMedia__ctor()
		{
			// CurrentMedia
			JukeboxService.CurrentMediaChanged += delegate
			{
				OnPropertyChanged("Title");
				OnPropertyChanged("TitleTooltip");
			};
			// Volume
			JukeboxService.AudioPlayer.VolumeChanged += delegate
			{
				OnPropertyChanged("Volume");
				OnPropertyChanged("VolumeTooltip");
			};
		}

		// CurrentMedia
		public string Title
		{
			get
			{
				var current = JukeboxService.CurrentMedia;
				return (current != null) ? current.Title : "[再生可能なメディアがありません]";
			}
		}
		public string TitleTooltip
		{
			get
			{
				var current = JukeboxService.CurrentMedia;
				if (current == null) return null;

				var sb = new StringBuilder(current.Title);
				if (!string.IsNullOrEmpty(current.Artist))
				{
					sb.AppendLine();
					sb.Append(current.Artist);
				}
				if (!string.IsNullOrEmpty(current.Album))
				{
					sb.AppendLine();
					sb.Append(current.Album);
				}
				if (!string.IsNullOrEmpty(current.Comment))
				{
					sb.AppendLine();
					sb.AppendLine();
					sb.Append(current.Comment);
				}
				return sb.ToString();
			}
		}

		// Volume
		public double Volume
		{
			get { return JukeboxService.AudioPlayer.Volume; }
			set { JukeboxService.AudioPlayer.SetVolume(value); }
		}
		public string VolumeTooltip
		{
			get { return string.Format("ボリューム：{0:F0}%", Volume*100); }
		}

		#endregion
		#region Position

		void Position__ctor()
		{
			// Position
			JukeboxService.AudioPlayer.Opened += delegate
			{
				OnPropertyChanged("PositionString");
				OnPropertyChanged("PositionTicks");
				OnPropertyChanged("DurationTicks");
				OnPropertyChanged("SeekTooltip");
			};
			Observable
				.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
				.Subscribe(_ =>
				{
					OnPropertyChanged("PositionString");
					OnPropertyChanged("PositionTicks");
					OnPropertyChanged("SeekTooltip");
				});
		}

		public static string PositionString
		{
			get
			{
				if (JukeboxService.AudioPlayer.Duration.HasValue)
				{
					var pos = JukeboxService.AudioPlayer.Position;
					return
						((pos.Hours > 0) ? pos.Hours.ToString("d2") + ":" : "") +
						pos.Minutes.ToString("d2") + ":" +
						pos.Seconds.ToString("d2");
				}
				else
				{
					return "00:00";
				}
			}
		}
		public long PositionTicks
		{
			get { return JukeboxService.AudioPlayer.Position.Ticks; }
			set { JukeboxService.AudioPlayer.SetPosition(TimeSpan.FromTicks(value)); }
		}
		public long DurationTicks
		{
			get
			{
				return (JukeboxService.AudioPlayer.Duration.HasValue) ?
					JukeboxService.AudioPlayer.Duration.Value.Ticks : 0;
			}
		}
		public string SeekTooltip
		{
			get
			{
				var d_string = "00:00";
				if (JukeboxService.AudioPlayer.Duration.HasValue)
				{
					var d = JukeboxService.AudioPlayer.Duration.Value;
					d_string = ((d.Hours > 0) ? d.Hours.ToString("d2") + ":" : "") +
						d.Minutes.ToString("d2") + ":" +
						d.Seconds.ToString("d2");
				}
				return PositionString + " / " + d_string;
			}
		}

		#endregion

		public ViewModelProperty<bool> IsVideoDrawing { get; private set; }

	}
}
