using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using SmartAudioPlayerFx.Data;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Quala;
using Quala.Extensions;

namespace SmartAudioPlayerFx.Managers
{
	[Require(typeof(XmlPreferencesManager))]
	[Require(typeof(AudioPlayerManager))]
	[Require(typeof(MediaDBViewManager))]
	sealed class JukeboxManager : IDisposable
	{
		#region ctor

		public ReactiveProperty<MediaDBViewFocus> ViewFocus { get; private set; }
		public ReactiveProperty<bool> IsServiceStarted { get; private set; }
		public ReactiveProperty<MediaItem> CurrentMedia { get; private set; }
		public PlaingAttributeInfo NextPlaingAttribute { get; set; }	// 次回再生時の動作を決定する属性
		public ReactiveProperty<bool> IsRepeat { get; private set; }
		public ReactiveProperty<SelectionMode> SelectMode { get; private set; }
		public static event Action<MediaItem> PlayError; // 再生エラーが発生した
		readonly CompositeDisposable _disposables;

		public JukeboxManager()
		{
			ViewFocus = new ReactiveProperty<MediaDBViewFocus>(new MediaDBViewFocus(null), ReactivePropertyMode.RaiseLatestValueOnSubscribe);
			IsServiceStarted = new ReactiveProperty<bool>(false);
			CurrentMedia = new ReactiveProperty<MediaItem>(mode: ReactivePropertyMode.RaiseLatestValueOnSubscribe);
			NextPlaingAttribute = null;
			IsRepeat = new ReactiveProperty<bool>(false);
			SelectMode = new ReactiveProperty<SelectionMode>(SelectionMode.Random);
			_disposables = new CompositeDisposable(ViewFocus, IsServiceStarted, CurrentMedia, IsRepeat, SelectMode);

			// Preferences
			ManagerServices.PreferencesManager.PlayerSettings
				.Subscribe(x => LoadPreferences(x))
				.AddTo(_disposables);
			ManagerServices.PreferencesManager.SerializeRequestAsObservable()
				.Subscribe(_ => SavePreferences(ManagerServices.PreferencesManager.PlayerSettings.Value))
				.AddTo(_disposables);

			// 再生が完了したら次の曲を再生
			ManagerServices.AudioPlayerManager.PlayEndedAsObservable()
				.Subscribe(x =>
				{
					var by_error = x.ErrorReason != null;	// エラーが発生した？
					if (by_error && CurrentMedia.Value != null && PlayError != null)
						PlayError(CurrentMedia.Value);
					SelectNext(false, by_error);
				})
				.AddTo(_disposables);

			// メディア情報が変更されたら更新する
			ManagerServices.MediaDBViewManager.Items
				.GetNotifyObservable()
				.Where(x => x.Type == VersionedCollection<MediaItem>.NotifyType.Update)
				.Subscribe(x =>
				{
					// MediaDBViewManagerから直生取得したものは
					// MediaItemのインスタンスが違うため自動更新が効かないので処理する必要がある
					var current = CurrentMedia.Value;
					if (current == null) return;
					if (x.Item.ID != current.ID) return;

					x.Item.CopyTo(current);
					NextPlaingAttribute = PlaingAttributeInfo.Ignore;
					CurrentMedia.Value = x.Item;
				})
				.AddTo(_disposables);

			// MediaDBViewManagerによるスキャンが完了したとき、再生してなかったら自動再生を試みる
			ManagerServices.MediaDBViewManager.ItemCollect_CoreScanFinishedAsObservable()
				.Where(_ => CurrentMedia.Value == null || CurrentMedia.Value.IsNotExist == true)
				.Subscribe(_ => SelectNext(true))
				.AddTo(_disposables);

			// CurrentMediaが更新
			CurrentMedia
				.ObserveOnUIDispatcher()
				.Subscribe(OnSetCurrentMedia)
				.AddTo(_disposables);
		}
		public void Dispose()
		{
			_disposables.Dispose();

			var v = ViewFocus.Value;
			if (v != null)
				v.Dispose();
		}

		void LoadPreferences(XElement element)
		{
			var ps = ManagerServices.PreferencesManagerJson.PlayerSettings;
			ViewFocus.Value = ViewFocusFromString(
				ps.GetValue("ViewMode", "Default"),
				ps.GetValue("ViewFocusPath", (string)null));

			SelectMode.Value = ps.GetValue("SelectMode", SelectionMode.Random);
			IsRepeat.Value = ps.GetValue("IsRepeat", false);
			ManagerServices.AudioPlayerManager.Volume = ps.GetValue("Volume", 0.5);
			NextPlaingAttribute = new PlaingAttributeInfo(
				false,
				false,
				ps.GetValue("IsPaused", false),
				ps.GetValue("Position", (TimeSpan?)null));
			// memo: 実行タイミングのずれで再生/一時停止を繰り返してしまうので、
			//       暫定処理
			System.Windows.Forms.Application.DoEvents();
			CurrentMedia.Value = /*ManagerServices.MediaDBViewManager.GetOrCreate(
				element.GetAttributeValueEx("CurrentMedia", (string)null))*/null;

			MediaDBViewFocus_LatestAddOnly.RecentIntervalDays =
				Math.Max(1, ps.GetValue("RecentIntervalDays", 60));
			//
			//
			ViewFocus.Value = ViewFocusFromString(
				element.GetAttributeValueEx("ViewMode", "Default"),
				element.GetAttributeValueEx("ViewFocusPath", (string)null));

			SelectMode.Value = element.GetAttributeValueEx("SelectMode", SelectionMode.Random);
			IsRepeat.Value = element.GetAttributeValueEx("IsRepeat", false);
			ManagerServices.AudioPlayerManager.Volume = element.GetAttributeValueEx("Volume", 0.5);
			NextPlaingAttribute = new PlaingAttributeInfo(
				false,
				false,
				element.GetAttributeValueEx("IsPaused", false),
				element.GetAttributeValueEx("Position", (TimeSpan?)null));
			// memo: 実行タイミングのずれで再生/一時停止を繰り返してしまうので、
			//       暫定処理
			System.Windows.Forms.Application.DoEvents();
			CurrentMedia.Value = /*ManagerServices.MediaDBViewManager.GetOrCreate(
				element.GetAttributeValueEx("CurrentMedia", (string)null))*/null;

			MediaDBViewFocus_LatestAddOnly.RecentIntervalDays =
				Math.Max(1, element.GetAttributeValueEx("RecentIntervalDays", 60));

		}
		void SavePreferences(XElement element)
		{
			ManagerServices.PreferencesManagerJson.PlayerSettings
				.SetValue("SelectMode", SelectMode.Value)
				.SetValue("IsRepeat", IsRepeat.Value)
				.SetValue("IsPaused", ManagerServices.AudioPlayerManager.IsPaused)
				.SetValue("Volume", ManagerServices.AudioPlayerManager.Volume.ToString("F3"))
				.SetValue("Position", ManagerServices.AudioPlayerManager.Position.ToString()) // ToString()しないと戻せない
				.SetValue("CurrentMedia", (CurrentMedia.Value != null) ? CurrentMedia.Value.FilePath : null)
				.SetValue("ViewMode", ViewFocusToString(ViewFocus.Value))
				.SetValue("ViewFocusPath", ViewFocus.Value.FocusPath)
				.SetValue("RecentIntervalDays", MediaDBViewFocus_LatestAddOnly.RecentIntervalDays)
				;
			//
			//
			element
				// this
				.SetAttributeValueEx("SelectMode", SelectMode.Value)
				.SetAttributeValueEx("IsRepeat", IsRepeat.Value)
				.SetAttributeValueEx("IsPaused", ManagerServices.AudioPlayerManager.IsPaused)
				.SetAttributeValueEx("Volume", ManagerServices.AudioPlayerManager.Volume.ToString("F3"))
				.SetAttributeValueEx("Position", ManagerServices.AudioPlayerManager.Position.ToString()) // ToString()しないと戻せない
				.SetAttributeValueEx("CurrentMedia", (CurrentMedia.Value != null) ? CurrentMedia.Value.FilePath : null)
				.SetAttributeValueEx("ViewMode", ViewFocusToString(ViewFocus.Value))
				.SetAttributeValueEx("ViewFocusPath", ViewFocus.Value.FocusPath)
				//
				.SetAttributeValueEx("RecentIntervalDays", MediaDBViewFocus_LatestAddOnly.RecentIntervalDays);
		}

		// MEMO: AllItemsはDefault扱い
		MediaDBViewFocus ViewFocusFromString(string viewFocusString, string focusPath)
		{
			switch (viewFocusString.ToLower())
			{
				case "favorite":
					return new MediaDBViewFocus_FavoriteOnly(focusPath);
				case "latestadd":
					return new MediaDBViewFocus_LatestAddOnly(focusPath);
				case "nonplayed":
					return new MediaDBViewFocus_NonPlayedOnly(focusPath);
			}
			return new MediaDBViewFocus(focusPath);
		}
		string ViewFocusToString(MediaDBViewFocus viewFocus)
		{
			return
				(viewFocus is MediaDBViewFocus_FavoriteOnly) ? "Favorite" :
				(viewFocus is MediaDBViewFocus_LatestAddOnly) ? "LatestAdd" :
				(viewFocus is MediaDBViewFocus_NonPlayedOnly) ? "NonPlayed" :
				"Default";
		}

		#endregion

		public void Start()
		{
			IsServiceStarted.Value = true;
			if (CurrentMedia.Value == null)
			{
				SelectNext(true);
			}
			else
			{
				CurrentMedia.Value = CurrentMedia.Value;
			}
		}

		void OnSetCurrentMedia(MediaItem item)
		{
			if (!IsServiceStarted.Value) return;

			var attr = NextPlaingAttribute ?? PlaingAttributeInfo.Default;
			NextPlaingAttribute = null;
			if (attr.IgnorePlay) return;

			// MEMO: 再生エラー項目の除外はしない
			if (item == null)
			{
				ManagerServices.AudioPlayerManager.Close();
			}
			else
			{
				// 再生を試みる
				ManagerServices.AudioPlayerManager.PlayFrom(item.FilePath, attr.PlayOnPaused, attr.PlayOnPosition, () =>
				{
					// 再生カウント更新
					Task.Run(() =>
					{
						lock (item)
						{
							var args = new List<Expression<Func<MediaItem, object>>>();
							item.LastUpdate = DateTime.UtcNow.Ticks;
							args.Add(_ => _.LastUpdate);
							if (attr.UpdateLastPlay)
							{
								item.LastPlay = DateTime.UtcNow.Ticks;
								args.Add(_ => _.LastPlay);
							}
							if (attr.UpdatePlayCount)
							{
								item.PlayCount++;
								args.Add(_ => _.PlayCount);
							}
							ManagerServices.MediaDBViewManager.RaiseDBUpdateAsync(item, args.ToArray());
						}
						// 曲情報更新
						if (item != null && item.ID != 0)
							RefreshMediaItemInfo(item, true);

						// 再生リストを更新
						if (attr.UpdateLastPlay)
							ManagerServices.RecentsManager.AddRecentsPlayItem(item);
					});
				});
			}
		}

		IObservable<MediaItem> RefreshMediaItemInfo(MediaItem item, bool update_db)
		{
			if (item == null) throw new ArgumentNullException();
			return Observable.Start(() =>
			{
				var finfo = new FileInfo(item.FilePath);
				var tag = MediaTagUtil.Get(finfo);
				if (tag.IsTagLoaded)
				{
					item.FilePath = finfo.FullName;
					item.Title = tag.Title;
					item.Artist = tag.Artist;
					item.Album = tag.Album;
					item.Comment = tag.Comment;
					item.UpdateSearchHint();
					item.CreatedDate = finfo.Exists ? finfo.CreationTimeUtc.Ticks : DateTime.MinValue.Ticks;
					item.LastUpdate = DateTime.UtcNow.Ticks;
					item.LastWrite = finfo.Exists ? finfo.LastWriteTimeUtc.Ticks : DateTime.MinValue.Ticks;
					item.IsNotExist = !finfo.Exists;
					item.GetFilePathDir(true);
				}
				if (update_db)
				{
					ManagerServices.MediaDBViewManager.RaiseDBUpdateAsync(item,
						_ => _.FilePath,
						_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
						_ => _.CreatedDate, _ => _.LastUpdate, _ => _.LastWrite, _ => _.IsNotExist);
				}
				return item;
			});
		}

		/// <summary>
		/// 現在のモードを考慮して次の曲を選択して再生
		/// </summary>
		/// <param name="skipMode"></param>
		/// <param name="by_error">現在の曲が再生エラーによって終了した場合true(リピード動作に影響)</param>
		public void SelectNext(bool skipMode, bool by_error = false)
		{
			// ViewFocusPathがないと選べない
			if (string.IsNullOrWhiteSpace(ViewFocus.Value.FocusPath))
			{
				CurrentMedia.Value = null;
				return;
			}
			// リピート
			if (skipMode == false && IsRepeat.Value && CurrentMedia.Value != null && by_error == false)
			{
				ManagerServices.AudioPlayerManager.Replay();
				// 再生カウント更新
				var item = CurrentMedia.Value;
				Task.Factory.StartNew(() =>
				{
					lock (item)
					{
						item.PlayCount++;
						item.LastPlay = DateTime.UtcNow.Ticks;
						item.LastUpdate = DateTime.UtcNow.Ticks;
						ManagerServices.MediaDBViewManager.RaiseDBUpdateAsync(item, _ => _.PlayCount, _ => _.LastPlay, _ => _.LastUpdate);
					}
				});
				return;
			}

			var rootItems = ViewFocus.Value.Items;
			if (rootItems == null) return;
			var items = rootItems.Get().Item1;

			// 現在のビューから選択
			// 曲が無いと選べないのでリプレイ
			if (items.Length == 0)
			{
				if (by_error)
				{
					// 再生＆選択不可能なのでnull
					AppService.Log.AddDebugLog("SelectNext: no items. (by error)");
					CurrentMedia.Value = null;
				}
				else
				{
					AppService.Log.AddDebugLog("SelectNext: no items.");
					CurrentMedia.Value = CurrentMedia.Value;
				}
				return;
			}
			// 1曲しかないなら先頭を再生
			if (items.Length == 1)
			{
				// エラーが原因で呼び出されたので、この曲は再生できない
				// nullにして終わる
				if (by_error)
				{
					AppService.Log.AddDebugLog("SelectNext: one items, by_error==true, set CurrenyMedia = null.");
					CurrentMedia.Value = null;
					return;
				}

				AppService.Log.AddDebugLog("SelectNext: one items.");
				CurrentMedia.Value = items[0];
				return;
			}
			// スキップならスキップカウント更新
			if (skipMode && CurrentMedia.Value != null)
			{
				CurrentMedia.Value.SkipCount++;
				CurrentMedia.Value.LastUpdate = DateTime.UtcNow.Ticks;
				ManagerServices.MediaDBViewManager.RaiseDBUpdateAsync(CurrentMedia.Value, _ => _.SkipCount, _ => _.LastUpdate);
			}

			// 再生エラーが無いアイテム群。
			var itemsQuery = items.Where(i => i._play_error_reason == null);

			// ファイル名順選択
			if (SelectMode.Value == SelectionMode.Filename && CurrentMedia.Value != null)
			{
				// ファイルパスでソート
				itemsQuery = itemsQuery
					.GroupBy(i => i.GetFilePathDir())
					.OrderBy(g => g.Key)
					.SelectMany(g => g.OrderBy(i => i.FilePath));
				// 現在のメディアの次までスキップ
				if (CurrentMedia.Value != null)
				{
					itemsQuery = itemsQuery
						.SkipWhile(i => !i.FilePath.Equals(CurrentMedia.Value.FilePath, StringComparison.CurrentCultureIgnoreCase))
						.Skip(1);
				}
				// メディアを取得 or リスト先頭を取得して選択
				var select_top = false;	// リストを周回して一番上を選んだ？
			ReSelect:
				var item = itemsQuery.FirstOrDefault();
				if (item == null)
				{
					if (select_top)
					{
						// 再生できなさそう？
						CurrentMedia.Value = null;
						return;
					}
					item = items
						.Where(i => i._play_error_reason == null)
						.GroupBy(i => i.GetFilePathDir())
						.OrderBy(g => g.Key)
						.SelectMany(g => g.OrderBy(i => i.FilePath))
						.FirstOrDefault();
					select_top = true;
				}
				if (File.Exists(item.FilePath) == false)
				{
					itemsQuery.Skip(1);
					goto ReSelect;
				}
				CurrentMedia.Value = item;
				return;
			}
			else // ランダム選択
			{
				// 現在のメディア以外を選択
				if (CurrentMedia.Value != null)
				{
					itemsQuery = itemsQuery
						.Where(i => !i.FilePath.Equals(CurrentMedia.Value.FilePath, StringComparison.CurrentCultureIgnoreCase));
				}

				// 既存の曲を選択しないための措置
				// デフォルトで全体の10%程度(min:1, max:100)はしばらく再生しない
				var latestplayednum = Math.Min(Math.Max(1, (items.Length / 10)), 100);
				var latestplayed = ManagerServices.RecentsManager.GetRecentsPlayItems(latestplayednum);
				var r = new Random();
			ReSelect:
				var item = itemsQuery
					.Repeat()
					.Skip(r.Next(rootItems.Count + 1))
					.FirstOrDefault();

				// 最近再生の曲は再生しない
				if (item != null && latestplayed.Contains(item.FilePath, StringComparer.CurrentCultureIgnoreCase))
					goto ReSelect;

				CurrentMedia.Value = item;
				return;
			}
		}

		/// <summary>
		/// 「以前再生した曲」を再生
		/// index = 0 = 今
		/// index = 1 = 1つ前
		/// </summary>
		/// <param name="index"></param>
		public void SelectPrevious(int? index = null)
		{
			// CurrentMediaがあって、index指定がないときは再生履歴1つ前を再生
			var prev = (CurrentMedia.Value != null && !index.HasValue) ?
				ManagerServices.MediaDBManager.PreviousPlayItem(CurrentMedia.Value) : null;
			if (prev == null)
			{
				// indexが100未満ならGetCachedRecentPlayItemsPath()を使う
				var idx = index ?? 0;
				prev = ((idx > 100) ?
					ManagerServices.MediaDBManager.RecentPlayItemsPath(idx + 1) :
					ManagerServices.RecentsManager.GetRecentsPlayItems(idx + 1))
					.Skip(idx)
					.FirstOrDefault();
			}
			if (prev != null)
			{
				// LastPlayを更新させない
				var item = ManagerServices.MediaDBViewManager.GetOrCreate(prev);
				NextPlaingAttribute = new PlaingAttributeInfo(false);
				CurrentMedia.Value = item;
			}
		}

		#region Definition

		public enum SelectionMode
		{
			Random,
			Filename,
		}

		public class PlaingAttributeInfo
		{
			/// <summary>
			/// 最終再生時刻を更新する
			/// </summary>
			public bool UpdateLastPlay;
			/// <summary>
			/// 再生数を更新(インクリメント)する
			/// </summary>
			public bool UpdatePlayCount;
			/// <summary>
			/// 再生してすぐにポーズ状態にする
			/// </summary>
			public bool PlayOnPaused;
			/// <summary>
			/// 再生してすぐにシークする
			/// </summary>
			public TimeSpan? PlayOnPosition;
			/// <summary>
			/// 再生を無視する(CurrentMediaプロパティ設定用)
			/// </summary>
			internal bool IgnorePlay;

			/// <summary>
			/// デフォルト値(UpdateXXX=true/PlayOnXXX=false or null)
			/// </summary>
			public static readonly PlaingAttributeInfo Default = new PlaingAttributeInfo();
			/// <summary>
			/// 無視用
			/// </summary>
			internal static readonly PlaingAttributeInfo Ignore = new PlaingAttributeInfo() { IgnorePlay = true, };

			public PlaingAttributeInfo(
				bool updateLastPlay = true,
				bool updatePlayCount = true,
				bool playOnPaused = false,
				TimeSpan? playOnPosition = null)
			{
				this.PlayOnPaused = playOnPaused;
				this.PlayOnPosition = playOnPosition;
				this.UpdateLastPlay = updateLastPlay;
				this.UpdatePlayCount = updatePlayCount;
				this.IgnorePlay = false;
			}
		}

		#endregion
	}
}
