using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using Quala;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Windows;

namespace SmartAudioPlayerFx.Player
{
	/// <summary>
	/// 連続再生するPlayerServiceっぽいもの。
	/// 現在再生曲と以前に再生した曲、再生エラーした曲を管理。
	/// </summary>
	// ↓  MediaDBService					[value-source/backend]
	//(↓↑ ItemFilterService					[source-filter])
	// ↓↑ ItemCollectionService				[find-source]
	// ↓↑ ItemCollectionViewFocusService	[select-source]
	// ↓  JukeboxService					[selection/value-cache]
	// ↓↑ PlayerService						[playing]
	// 設定保存に関係するもの↓
	static class JukeboxService
	{
		public static AudioPlayer AudioPlayer { get; private set; }
		public static MediaDBView AllItems { get; private set; }
		public static MediaItemsViewFocus ViewFocus { get; private set; }

		/// <summary>
		/// 再生エラーが発生しました
		/// </summary>
		public static event Action<MediaItem> PlayError;

		// サービス開始しました的な
		public static event Action ServiceStarted;
		public static bool IsServiceStarted { get; private set; }

		static LinkedList<string> recent_play_items;

		#region フォルダオープン履歴(SetFocusPath()で設定される)

		public const int FOLDER_RECENTS_ITEMS_MAX = 20;
		static List<string> opened_folder_recents;
		public static string[] GetFolderRecents()
		{
			return
				(opened_folder_recents != null) ? opened_folder_recents.ToArray() :
				new string[0];
		}
		public static void AddFolderRecents(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;
			if (opened_folder_recents == null)
				opened_folder_recents = new List<string>();
			//
			opened_folder_recents
				.RemoveAll(i => i.Equals(path, StringComparison.CurrentCultureIgnoreCase));
			opened_folder_recents
				.Insert(0, path);
			if (opened_folder_recents.Count >= FOLDER_RECENTS_ITEMS_MAX)
				opened_folder_recents.RemoveRange(
					FOLDER_RECENTS_ITEMS_MAX,
					opened_folder_recents.Count - (FOLDER_RECENTS_ITEMS_MAX + 1));
		}
		public static void ClearFolderRecents()
		{
			if (opened_folder_recents != null)
				opened_folder_recents.Clear();
		}

		#endregion

		#region ctor

		static JukeboxService()
		{
			var db_filename = PreferenceService.CreateFullPath("data", "media.db");
			AudioPlayer = new AudioPlayer();
			AllItems = new MediaDBView(db_filename);
			ViewFocus = new MediaItemsViewFocus(AllItems);
			recent_play_items = new LinkedList<string>();

			// 初期設定
			SelectMode = SelectionMode.Random;
			IsRepeat = false;

			// フォルダ履歴
			AllItems.FocusPathChanged += delegate
			{
				AddFolderRecents(AllItems.FocusPath);
			};

			// 再生が完了したら次の曲を再生
			AudioPlayer.PlayEnded += e =>
			{
				var by_error = e.ErrorReason != null;	// エラーが発生した？
				if (by_error && CurrentMedia != null && PlayError != null)
					PlayError(CurrentMedia);
				SelectNext(false, by_error);
			};

			// メディア情報が変更されたら更新する
			AllItems.ItemChanged += e =>
			{
				// MediaDBServiceから直生取得したものは
				// MediaItemのインスタンスが違うからItemsCollectionServiceによる自動更新が効かないので処理する必要がある
				if (e.ChangedType != MediaDBView.MediaItemChangedType.Update) return;
				var current = CurrentMedia;
				if (current == null) return;

				var item = e.Items.FirstOrDefault(i => i.ID == current.ID);
				if (item == null) return;

				item.CopyTo(current);
				// 呼び出さないとViewが更新を認知できない
				if (CurrentMediaChanged != null)
					CurrentMediaChanged(new CurrentMediaChangedEventArgs() { NewMedia = item, });
			};

			// MediaDBServiceによるスキャンが完了したとき、再生してなかったら自動再生を試みる
			AllItems.ItemCollect_CoreScanFinished += delegate
			{
				if (CurrentMedia == null)
					UIService.UIThreadInvoke(() => SelectNext(true));
			};
		}

		public static void Start()
		{
			// 再生履歴を100件まで保持
			var items = AllItems.RecentPlayItemsPath(100);
			lock (recent_play_items)
			{
				items.Run(i => recent_play_items.AddLast(i));
			}

			// LoadPreferencesApplyで保持しておいたCurrentMedia(reserve)などををこのタイミングで設定
			AllItems.SetFocusPath(focuspath_reserve, true,
				() =>
				{
					ViewFocus.SetViewFocusPath(viewfocuspath_reserve ?? AllItems.FocusPath);
					var item = AllItems.GetOrCreate(currentmedia_reserve);
					if (item != null)
					{
						if (item.ID != 0)
						{
							var finfo = new FileInfo(item.FilePath);
							var tag = MediaTagService.Get(item.FilePath);
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
								AllItems.RaiseDBUpdate(item,
									_ => _.FilePath,
									_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
									_ => _.CreatedDate, _ => _.LastUpdate, _ => _.LastWrite, _ => _.IsNotExist);
							}
						}
						UIService.UIThreadInvoke(() => SetCurrentMedia(item, false, false, playfrom_parameter_reserve.Item1, playfrom_parameter_reserve.Item2));
					}
					else
					{
						UIService.UIThreadInvoke(() => SelectNext(true));
					}
				}, null);
			IsServiceStarted = true;
			if (ServiceStarted != null)
				ServiceStarted();
		}

		public static void Dispose()
		{
			AudioPlayer.Close();
			AllItems.SetFocusPath(null, false);	// 検索の停止処理
			AllItems.Dispose();
		}

		#endregion
		#region Preferences

		// 起動時の設定読み込みで重くならないように一時変数に読み込んでおく。Start()後は直接設定。
		static string currentmedia_reserve;
		static Tuple<bool, TimeSpan?> playfrom_parameter_reserve;
		static string focuspath_reserve;
		static string viewfocuspath_reserve;

		public static void SavePreferencesAdd(XElement element)
		{
			element
				// this
				.SetAttributeValueEx(() => SelectMode)
				.SetAttributeValueEx(() => IsRepeat)
				.SetAttributeValueEx(() => AudioPlayer.IsPaused)
				.SetAttributeValueEx("Volume", AudioPlayer.Volume.ToString())
				.SetAttributeValueEx("Position", AudioPlayer.Position.ToString()) // ToString()しないと戻せない
				.SetAttributeValueEx("CurrentMedia", (CurrentMedia != null) ? CurrentMedia.FilePath : null)
				// ItemFilterService
				.SubElement("AcceptExtensions", true, elm =>
				{
					elm.RemoveAll();
					JukeboxService.AllItems.MediaItemFilter.AcceptExtensions.Run(i =>
					{
						elm
							// 同じExtension属性の値を持つ最後のElementを選択、なければ作る
							.GetOrCreateElement("Item", m => m.Attributes("Extension")
								.Any(n => string.Equals(n.Value, i.Extension, StringComparison.CurrentCultureIgnoreCase)))
							.SetAttributeValueEx(() => i.IsEnable)
							.SetAttributeValueEx(() => i.Extension);
					});
				})
				.SubElement("IgnoreWords",true,  elm =>
				{
					elm.RemoveAll();
					JukeboxService.AllItems.MediaItemFilter.IgnoreWords.Run(i =>
					{
						elm
							// 同じWord属性の値を持つ最後のElementを選択、なければ作る
							.GetOrCreateElement("Item",
								m => m.Attributes("Word")
									.Any(n => string.Equals(n.Value, i.Word, StringComparison.CurrentCultureIgnoreCase)))
							.SetAttributeValueEx("IsEnable", i.IsEnable)
							.SetAttributeValueEx("Word", i.Word);
					});
				})
				// ItemCollectionService
				.SetAttributeValueEx(() => JukeboxService.AllItems.FocusPath)
				.SubElement("FolderRecents", true, elm =>
				{
					elm.RemoveAll();
					JukeboxService.GetFolderRecents().Run(i =>
					{
						elm
							// 同じValue属性の値を持つ最後のElementを選択、なければ作る
							.GetOrCreateElement("Item",
								m => m.Attributes("Value")
									.Any(n => string.Equals(n.Value, i, StringComparison.CurrentCultureIgnoreCase)))
							.SetAttributeValueEx("Value", i);
					});
				})
				// ItemCollectionViewFocusService
				.SetAttributeValueEx(() => ViewFocus.ViewFocusPath);
		}
		public static void LoadPreferencesApply(XElement element)
		{
			// ItemFilterService
			var extensions = element.GetArrayValues("AcceptExtensions",
				el => new MediaItemFilter.AcceptExtension(
					el.GetAttributeValueEx("IsEnable", false),
					el.GetAttributeValueEx("Extension", (string)null)))
				.ToArray();
			JukeboxService.AllItems.MediaItemFilter.SetAcceptExtensions(extensions);
			var words = element.GetArrayValues("IgnoreWords",
				el => new MediaItemFilter.IgnoreWord(
					el.GetAttributeValueEx("IsEnable", false),
					el.GetAttributeValueEx("Word", (string)null)))
				.ToArray();
			JukeboxService.AllItems.MediaItemFilter.SetIgnoreWords(words);
			// ItemCollectionService
			element
				.GetArrayValues("FolderRecents", el => el.GetAttributeValueEx("Value", string.Empty))
				.Where(i => !string.IsNullOrWhiteSpace(i))
				.Reverse()
				.Run(i => AddFolderRecents(i));
			focuspath_reserve = element.GetAttributeValueEx("FocusPath", (string)null);
			// ItemCollectionViewFocusService
			viewfocuspath_reserve = element.GetAttributeValueEx("ViewFocusPath", (string)null);
			// this
			element
				.GetAttributeValueEx((object)null, _ => SelectMode)
				.GetAttributeValueEx((object)null, _ => IsRepeat);
			var volume = element.GetAttributeValueEx("Volume", 0.5);
			currentmedia_reserve = element.GetAttributeValueEx("CurrentMedia", (string)null);
			if (currentmedia_reserve != null)
			{
				AudioPlayer.SetVolume(volume);
				var paused = element.GetAttributeValueEx("IsPaused", false);
				var position = element.GetAttributeValueEx("Position", (TimeSpan?)null);
				playfrom_parameter_reserve = Tuple.Create(paused, position);
			}
			//
			if (IsServiceStarted)
			{
				AllItems.SetFocusPath(focuspath_reserve, true, () =>
				{
					ViewFocus.SetViewFocusPath(viewfocuspath_reserve ?? AllItems.FocusPath);
					var item = AllItems.GetOrCreate(currentmedia_reserve);
					if (item != null)
					{
						if (item.ID != 0)
						{
							var finfo = new FileInfo(item.FilePath);
							var tag = MediaTagService.Get(item.FilePath);
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
								AllItems.RaiseDBUpdate(item,
									_ => _.FilePath,
									_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
									_ => _.CreatedDate, _ => _.LastUpdate, _ => _.LastWrite, _ => _.IsNotExist);
							}
						}
						SetCurrentMedia(item, false, false, playfrom_parameter_reserve.Item1, playfrom_parameter_reserve.Item2);
					}
					else
					{
						SelectNext(true);
					}
				});
			}
		}

		#endregion

		/// <summary>
		/// CurrentMediaが変更された
		/// </summary>
		public static event Action<CurrentMediaChangedEventArgs> CurrentMediaChanged;
		/// <summary>
		/// 現在再生曲
		/// </summary>
		public static MediaItem CurrentMedia { get; private set; }
		/// <summary>
		/// 現在再生曲を設定
		/// </summary>
		/// <param name="item"></param>
		public static void SetCurrentMedia(
			MediaItem item,
			bool update_lastplay = true,
			bool update_playcount = true,
			bool play_on_paused = false,
			TimeSpan? play_on_position = null)
		{
			// MEMO: 再生エラー項目の除外はしない
			var oldMedia = CurrentMedia;
			CurrentMedia = item;
			if (item == null)
			{
				AudioPlayer.Close();
			}
			else
			{
				// 再生を試みる
				AudioPlayer.PlayFrom(item.FilePath, play_on_paused, play_on_position, () =>
				{
					// 再生カウント更新
					Observable.Start(() =>
					{
						lock (item)
						{
							var args = new List<Expression<Func<MediaItem, object>>>();
							item.LastUpdate = DateTime.UtcNow.Ticks;
							args.Add(_ => _.LastUpdate);
							if (update_lastplay)
							{
								item.LastPlay = DateTime.UtcNow.Ticks;
								args.Add(_ => _.LastPlay);
							}
							if (update_playcount)
							{
								item.PlayCount++;
								args.Add(_ => _.PlayCount);
							}
							AllItems.RaiseDBUpdate(item, args.ToArray());
						}

						// 曲情報更新
						if (item != null && item.ID != 0)
						{
							var finfo = new FileInfo(item.FilePath);
							var tag = MediaTagService.Get(item.FilePath);
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
								AllItems.RaiseDBUpdate(item,
									_ => _.FilePath,
									_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
									_ => _.CreatedDate, _ => _.LastUpdate, _ => _.LastWrite, _ => _.IsNotExist);
							}
						
						}

						if (update_lastplay)
						{
							// 再生リストを更新
							lock (recent_play_items)
							{
								var current = item.FilePath;
								var top = recent_play_items.First;
								if (top != null)
								{
									// 一緒なら何もしない (重複追加をしないように)
									if (string.Equals(current, top.Value, StringComparison.CurrentCultureIgnoreCase))
										return;
								}
								// リストからcurrentを全て削除、先頭にcurrentを追加、100件以上ならそれ以下になるまで削除
								recent_play_items
									.Where(i => i.Equals(current, StringComparison.CurrentCultureIgnoreCase))
									.ToArray()
									.Run(i => recent_play_items.Remove(i));
								recent_play_items.AddFirst(current);
								while (recent_play_items.Count > 100)
								{
									recent_play_items.RemoveLast();
								}
							}
						}
					});
				});
			}
			if (CurrentMediaChanged != null)
				CurrentMediaChanged(new CurrentMediaChangedEventArgs() { NewMedia = item, OldMedia = oldMedia, });
		}

		/// <summary>
		/// リピート変更
		/// </summary>
		public static event Action IsRepeatChanged;
		/// <summary>
		/// リピートする？
		/// </summary>
		public static bool IsRepeat { get; private set; }
		/// <summary>
		/// リピート状態を設定
		/// </summary>
		/// <param name="value"></param>
		public static void SetIsRepeat(bool value)
		{
			IsRepeat = value;
			if (IsRepeatChanged != null)
				IsRepeatChanged();
		}

		/// <summary>
		/// 選択モードが変更された
		/// </summary>
		public static event Action SelectModeChanged;
		/// <summary>
		/// 選択モード
		/// </summary>
		public static SelectionMode SelectMode { get; private set; }
		/// <summary>
		/// 選択モードの設定
		/// </summary>
		/// <param name="mode"></param>
		public static void SetSelectMode(SelectionMode mode)
		{
			SelectMode = mode;
			if (SelectModeChanged != null)
				SelectModeChanged();
		}

		/// <summary>
		/// 現在のモードを考慮して次の曲を選択して再生
		/// </summary>
		/// <param name="skipMode"></param>
		/// <param name="by_error">現在の曲が再生エラーによって終了した場合true(リピード動作に影響)</param>
		public static void SelectNext(bool skipMode, bool by_error = false)
		{
			// ViewFocusPathがないと選べない
			if (string.IsNullOrWhiteSpace(ViewFocus.ViewFocusPath))
			{
				SetCurrentMedia(null);
				return;
			}
			// リピート
			if (skipMode == false && IsRepeat && CurrentMedia != null && by_error == false)
			{
				AudioPlayer.Replay();
				// 再生カウント更新
				var item = CurrentMedia;
				Observable.Start(() =>
				{
					lock (item)
					{
						item.PlayCount++;
						item.LastPlay = DateTime.UtcNow.Ticks;
						item.LastUpdate = DateTime.UtcNow.Ticks;
						AllItems.RaiseDBUpdate(item, _ => _.PlayCount, _ => _.LastPlay, _ => _.LastUpdate);
					}
				});
				return;
			}

			// 現在のビューから選択
			// リスト変更とランダム選択などがぶつかると例外がでるのでロックする
			var list = ViewFocus.ViewItems;
			if (list == null) return;
			lock (list)
			{
				// 曲が無いと選べないのでリプレイ
				if (list.Count == 0)
				{
					if (by_error == false)
					{
						LogService.AddDebugLog("SelectNext: no items.");
						SetCurrentMedia(CurrentMedia);
					}
					else
					{
						// 再生＆選択不可能なのでnull
						LogService.AddDebugLog("SelectNext: no items. (by error)");
						SetCurrentMedia(null);
					}
					return;
				}
				// 1曲しかないなら先頭を再生
				if (list.Count == 1)
				{
					LogService.AddDebugLog("SelectNext: one items.");
					SetCurrentMedia(list.Values.First());
					return;
				}
				// スキップならスキップカウント更新
				if (skipMode && CurrentMedia != null)
				{
					Observable.Start(() =>
					{
						CurrentMedia.SkipCount++;
						CurrentMedia.LastUpdate = DateTime.UtcNow.Ticks;
						AllItems.RaiseDBUpdate(CurrentMedia, _ => _.SkipCount, _ => _.LastUpdate);
					});
				}

				// 再生エラーが無いアイテム群。
				var items = list.Values
					.Where(i => i._play_error_reason == null);

				// ファイル名順選択
				if (SelectMode == SelectionMode.Filename && CurrentMedia != null)
				{
					// ファイルパスでソート
					items = items
						.GroupBy(i => Path.GetDirectoryName(i.FilePath))
						.OrderBy(g => g.Key)
						.SelectMany(g => g.OrderBy(i => i.FilePath));
					// 現在のメディアの次までスキップ
					if (CurrentMedia != null)
					{
						items = items
							.SkipWhile(i => !i.FilePath.Equals(CurrentMedia.FilePath, StringComparison.CurrentCultureIgnoreCase))
							.Skip(1);
					}
					// メディアを取得 or リスト先頭を取得して選択
					var select_top = false;	// リストを周回して一番上を選んだ？
				ReSelect:
					var item = items.FirstOrDefault();
					if (item == null)
					{
						if (select_top)
						{
							// 再生できなさそう？
							SetCurrentMedia(null);
							return;
						}
						item = list.Values.First();
						select_top = true;
					}
					if (File.Exists(item.FilePath) == false)
					{
						items.Skip(1);
						goto ReSelect;
					}
					SetCurrentMedia(item);
					return;
				}
				// ランダム選択
				{
					// 現在のメディア以外を選択
					if (CurrentMedia != null)
					{
						items = items
							.Where(i => !i.FilePath.Equals(CurrentMedia.FilePath, StringComparison.CurrentCultureIgnoreCase));
					}

					// 既存の曲を選択しないための措置
					// デフォルトで全体の10%程度(min:1, max:100)はしばらく再生しない
					var latestplayednum = (list.Count / 10).Limit(1, 100);
					var latestplayed = GetCachedRecentPlayItemsPath(latestplayednum);
					var r = new Random();

				ReSelect:
					var item = items
						.Repeat()
						.Skip(r.Next(list.Count + 1))
						.FirstOrDefault();

					// 最近再生の曲は再生しない
					if (item != null && latestplayed.Contains(item.FilePath, StringComparer.CurrentCultureIgnoreCase))
						goto ReSelect;

					SetCurrentMedia(item);
					return;
				}
			}
		}

		/// <summary>
		/// 「以前再生した曲」を再生
		/// index = 0 = 今
		/// index = 1 = 1つ前
		/// </summary>
		/// <param name="index"></param>
		public static void SelectPrevious(int? index = null)
		{
			// CurrentMediaがあって、index指定がないときは再生履歴1つ前を再生
			var prev = (CurrentMedia != null && !index.HasValue) ? AllItems.PreviousPlayItem(CurrentMedia) : null;
			if (prev == null)
			{
				// indexが100未満ならGetCachedRecentPlayItemsPath()を使う
				var idx = index ?? 0;
				prev = ((idx > 100) ? AllItems.RecentPlayItemsPath(idx + 1) : GetCachedRecentPlayItemsPath(idx + 1))
					.Skip(idx)
					.FirstOrDefault();
			}
			if (prev != null)
			{
				// LastPlayを更新させない
				var item = AllItems.GetOrCreate(prev);
				Observable.Start(() =>
				{
					if (item == null || item.ID == 0) return;
					var finfo = new FileInfo(item.FilePath);
					var tag = MediaTagService.Get(item.FilePath);
					if (tag.IsTagLoaded == false) return;
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
					AllItems.RaiseDBUpdate(item,
						_ => _.FilePath,
						_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
						_ => _.CreatedDate, _ => _.LastUpdate, _ => _.LastWrite, _ => _.IsNotExist);
				});
				SetCurrentMedia(item, false);
			}
		}

		public enum SelectionMode { Random, Filename, }

		/// <summary>
		/// JukeboxServiceによってキャッシュされた再生履歴(最新100件)を取得します。
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public static string[] GetCachedRecentPlayItemsPath(int limit)
		{
			lock (recent_play_items)
			{
				return recent_play_items.Take(limit).ToArray();
			}
		}

		public sealed class CurrentMediaChangedEventArgs : EventArgs
		{
			public MediaItem OldMedia;
			public MediaItem NewMedia;
		}

	}
}
