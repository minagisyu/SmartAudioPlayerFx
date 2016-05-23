using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Quala;
using SmartAudioPlayerFx.UI;

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
		public static MediaItemsCollection AllItems { get; private set; }
		public static MediaItemsCollectionViewFocus ViewFocus { get; private set; }

		/// <summary>
		/// 再生エラーが発生しました
		/// </summary>
		public static event Action<MediaItem> PlayError;

		static LinkedList<string> recent_play_items;

		#region ctor

		static JukeboxService()
		{
			AudioPlayer = new AudioPlayer();
			AllItems = new MediaItemsCollection();
			ViewFocus = new MediaItemsCollectionViewFocus(AllItems);

			// 再生履歴を100件まで保持
			var items = MediaDBService.RecentPlayItemsPath(100);
			recent_play_items = new LinkedList<string>(items);

			// 再生が完了したら次の曲を再生
			AudioPlayer.PlayEnded += OnPlayEnded;
			// メディア情報が変更されたら更新する
			MediaDBService.MediaItemChanged += OnMediaItemChanged;
			// MediaDBServiceによるスキャンが完了したとき、再生してなかったら自動再生を試みる
			JukeboxService.AllItems.ItemCollect_CoreScanFinished += OnCoreScanFinished;
		}

		static void OnPlayEnded(AudioPlayer.PlayEndedEventArgs e)
		{
			if (e.ErrorReason != null && CurrentMedia != null && PlayError != null)
				PlayError(CurrentMedia);
			SelectNext(false);
		}

		static void OnMediaItemChanged(MediaDBService.MediaItemChangedEventArgs e)
		{
			// MediaDBServiceから直生取得したものは
			// MediaItemのインスタンスが違うからItemsCollectionServiceによる自動更新が効かないので処理する必要がある
			if (e.IsFullInfo && CurrentMedia != null)
			{
				var item = e.Items.FirstOrDefault(i => i.ID == CurrentMedia.ID);
				if (item != null)
				{
					item.CopyTo(CurrentMedia);
					// 呼び出さないとViewが更新を認知できない
					if (CurrentMediaChanged != null)
						CurrentMediaChanged(new CurrentMediaChangedEventArgs() { NewMedia = item, });
				}
			}
		}

		static void OnCoreScanFinished()
		{
			if (CurrentMedia == null)
				UIService.UIThreadInvoke(() => SelectNext(true));
		}

		#endregion
		#region Preferences

		public static void SavePreferencesAdd(XElement element)
		{
			{	// this
				element.SetAttributeValue("SelectMode", SelectMode);
				element.SetAttributeValue("IsRepeat", IsRepeat);
				element.SetAttributeValue("IsPaused", AudioPlayer.IsPaused);
				element.SetAttributeValue("Volume", AudioPlayer.Volume.ToString());
				element.SetAttributeValue("Position", AudioPlayer.Position.ToString()); // ToString()しないと戻せない
				element.SetAttributeValue("CurrentMedia", (CurrentMedia != null) ? CurrentMedia.FilePath : null);
			}
			{	// ItemFilterService
				var elm1 = element.Element("AcceptExtensions");
				if (elm1 == null) { elm1 = new XElement("AcceptExtensions"); element.Add(elm1); }
				var elm2 = elm1.Elements("Item").ToArray();
				JukeboxService.AllItems.MediaItemFilteringManager.AcceptExtensions.Run(i =>
				{
					// 同じExtension属性の値を持つ最後のElementを選択、なければ作る
					var target = elm2.Where(m =>
					{
						return m.Attributes("Extension")
							.Any(n => n.Value.Equals(i.Extension, StringComparison.CurrentCultureIgnoreCase));
					}).LastOrDefault();
					if (target == null) { target = new XElement("Item"); elm1.Add(target); }
					// 設定
					target.SetAttributeValue("IsEnable", i.IsEnable);
					target.SetAttributeValue("Extension", i.Extension);
				});
				//
				elm1 = element.Element("IgnoreWords");
				if (elm1 == null) { elm1 = new XElement("IgnoreWords"); element.Add(elm1); }
				elm2 = elm1.Elements("Item").ToArray();
				JukeboxService.AllItems.MediaItemFilteringManager.IgnoreWords.Run(i =>
				{
					// 同じWord属性の値を持つ最後のElementを選択、なければ作る
					var target = elm2.Where(m =>
					{
						return m.Attributes("Word")
							.Any(n => n.Value.Equals(i.Word, StringComparison.CurrentCultureIgnoreCase));
					}).LastOrDefault();
					if (target == null) { target = new XElement("Item"); elm1.Add(target); }
					// 設定
					target.SetAttributeValue("IsEnable", i.IsEnable);
					target.SetAttributeValue("Word", i.Word);
				});
			}
			{	// ItemCollectionService
				element.SetAttributeValue("FocusPath", JukeboxService.AllItems.FocusPath);
				var elm1 = element.Element("FolderRecents");
				if (elm1 == null) { elm1 = new XElement("FolderRecents"); element.Add(elm1); }
				var elm2 = elm1.Elements("Item").ToArray();
				JukeboxService.AllItems.GetFolderRecents().Run(i =>
				{
					// 同じValue属性の値を持つ最後のElementを選択、なければ作る
					var target = elm2.Where(m =>
					{
						return m.Attributes("Value")
							.Any(n => n.Value.Equals(i, StringComparison.CurrentCultureIgnoreCase));
					}).LastOrDefault();
					if (target == null) { target = new XElement("Item"); elm1.Add(target); }
					// 設定
					target.SetAttributeValue("Value", i);
				});
			}
			{	// ItemCollectionViewFocusService
				element.SetAttributeValue("ViewFocusPath", ViewFocus.ViewFocusPath);
			}
		}
		public static void LoadPreferencesApply(XElement element)
		{
			{	// ItemFilterService
				var extensions = element.GetArrayValues("AcceptExtensions",
					el => new MediaItemFilteringManager.AcceptExtension(
						el.GetOrDefaultValue("IsEnable", false),
						el.GetOrDefaultValue("Extension", (string)null)));
				JukeboxService.AllItems.MediaItemFilteringManager.SetAcceptExtensions(extensions);
				var words = element.GetArrayValues("IgnoreWords",
					el => new MediaItemFilteringManager.IgnoreWord(
						el.GetOrDefaultValue("IsEnable", false),
						el.GetOrDefaultValue("Word", (string)null)));
				JukeboxService.AllItems.MediaItemFilteringManager.SetIgnoreWords(words);
			}
			{	// ItemCollectionService
				element
					.GetArrayValues("FolderRecents", el => el.GetOrDefaultValue("Value", string.Empty))
					.Where(i => !string.IsNullOrWhiteSpace(i))
					.Reverse()
					.Run(i => JukeboxService.AllItems.AddFolderRecents(i));
				var focus_path = element.GetOrDefaultValue("FocusPath", (string)null);
				JukeboxService.AllItems.SetFocusPath(focus_path, true);
			}
			{	// ItemCollectionViewFocusService
				var view_focus_path = element.GetOrDefaultValue("ViewFocusPath", JukeboxService.AllItems.FocusPath);
				ViewFocus.SetViewFocusPath(view_focus_path);
			}
			{	// this
				SelectMode = element.GetOrDefaultValue("SelectMode", SelectionMode.Random);
				IsRepeat = element.GetOrDefaultValue("IsRepeat", false);
				var current = element.GetOrDefaultValue("CurrentMedia", (string)null);
				if (current != null)
				{
					AudioPlayer.SetVolume(element.GetOrDefaultValue("Volume", 0.5));
					var paused = element.GetOrDefaultValue("IsPaused", false);
					var position = element.GetOrDefaultValue("Position", (TimeSpan?)null);
					var item = MediaDBService.GetOrCreate(current);
					if (item != null)
						SetCurrentMedia(item, false, false, paused, position);
					else
						SelectNext(true);
				}
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
					// 再生カウント更新 / 曲情報更新
					Task.Factory.StartNew(() =>
					{
						lock (item)
						{
							var args = new List<Expression<Func<MediaItem, object>>>();
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
							if (args.Count != 0)
							{
								MediaDBService.Update(item, args.ToArray());
							}
						}
						MediaDBService.GetOrCreate(item.FilePath);
					});
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
		public static void SelectNext(bool skipMode)
		{
			// ViewFocusPathがないと選べない
			if (string.IsNullOrWhiteSpace(ViewFocus.ViewFocusPath))
			{
				SetCurrentMedia(null);
				return;
			}
			// リピート
			if (skipMode == false && IsRepeat && CurrentMedia != null)
			{
				AudioPlayer.Replay();
				// 再生カウント更新
				var item = CurrentMedia;
				Task.Factory.StartNew(() =>
				{
					lock (item)
					{
						item.PlayCount++;
						item.LastPlay = DateTime.UtcNow.Ticks;
						MediaDBService.Update(item,
							_ => _.PlayCount,
							_ => _.LastPlay);
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
					LogService.AddDebugLog("JukeboxService", "SelectNext: no items.");
					SetCurrentMedia(CurrentMedia);
					return;
				}
				// 1曲しかないなら先頭を再生
				if (list.Count == 1)
				{
					LogService.AddDebugLog("JukeboxService", "SelectNext: one items.");
					SetCurrentMedia(list.Values.First());
					return;
				}
				// スキップならスキップカウント更新
				if (skipMode && CurrentMedia != null)
				{
					Task.Factory.StartNew(() =>
					{
						CurrentMedia.SkipCount++;
						MediaDBService.Update(CurrentMedia, _ => _.SkipCount);
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
			var prev = (CurrentMedia != null && !index.HasValue) ? MediaDBService.PreviousPlayItem(CurrentMedia) : null;
			if (prev == null)
			{
				// indexが100未満ならGetCachedRecentPlayItemsPath()を使う
				var idx = index ?? 0;
				prev = ((idx > 100) ? MediaDBService.RecentPlayItemsPath(idx + 1) : GetCachedRecentPlayItemsPath(idx + 1))
					.Skip(idx)
					.FirstOrDefault();
			}
			if (prev != null)
			{
				// LastPlayを更新させない
				var item = MediaDBService.GetOrCreate(prev);
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
