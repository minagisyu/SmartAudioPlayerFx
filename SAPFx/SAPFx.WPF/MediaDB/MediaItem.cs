using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic;

namespace SmartAudioPlayerFx.MediaDB
{
	[DebuggerDisplay("{Title}")]
	public sealed class MediaItem
	{
		// Memo: SAPFx 3.2.0.1以前のQuala.dllの実装ミスにより
		//       DBカラムが追加されると例外落ちして読み込めなくなるのでカラムの追加は不可能
		//       互換性を維持するためには、別のテーブルにデータを置く必要がある
		const char _SPLITCHAR_ = '\b';		// 検索ヒント用セパレート文字
		public string _play_error_reason;

		public long ID { get; set; }			// ID(自動生成/Primary key)
		public string FilePath { get; set; }	// ファイルパス(indexed, unique, collate nocase)
		public string Title { get; set; }       // タイトル
		public string Artist { get; set; }		// アーティスト
		public string Album { get; set; }		// アルバム
		public string Comment { get; set; }		// コメント
		public string SearchHint { get; set; }	// 検索ヒント
		public long CreatedDate { get; set; }	// ファイル作成日(UTC ticks)
		public long LastWrite { get; set; }		// ファイル最終書き込み日(UTC ticks)
		public long LastUpdate { get; set; }	// DB最終更新日(UTC ticks)
		public long LastPlay { get; set; }		// 最終再生日(UTC ticks)
		public long PlayCount { get; set; }		// 再生数
		public long SelectCount { get; set; }	// 選択数
		public long SkipCount { get; set; }		// スキップ数
		public bool IsFavorite { get; set; }	// お気に入り？
		public bool IsNotExist { get; set; }	// ファイルが存在しない

		public MediaItem() { }
		public MediaItem(string path)
		{
			var fi = new FileInfo(path);
			ID = 0;
			FilePath = fi.FullName;
			Title = fi.Name;
			Artist = string.Empty;
			Album = string.Empty;
			Comment = string.Empty;
			UpdateSearchHint();
			CreatedDate = ((fi.Exists) ? fi.CreationTimeUtc : DateTime.MinValue).Ticks;
			LastWrite = ((fi.Exists) ? fi.LastWriteTimeUtc : DateTime.MinValue).Ticks;
			LastUpdate = DateTime.UtcNow.Ticks;
			LastPlay = DateTime.MinValue.Ticks;
			PlayCount = 0;
			SkipCount = 0;
			IsFavorite = false;
			IsNotExist = !fi.Exists;
		}

		public void UpdateSearchHint()
		{
			// filepath, title, artist, album, commentを半角小文字カナに変換して\bで連結
			var filepath = StrConv_LowerHankakuKana(FilePath);
			var title = StrConv_LowerHankakuKana(Title);
			var artist = StrConv_LowerHankakuKana(Artist);
			var album = StrConv_LowerHankakuKana(Album);
			var comment = StrConv_LowerHankakuKana(Comment);
			SearchHint = $"{filepath}{_SPLITCHAR_}{title}{_SPLITCHAR_}{artist}{_SPLITCHAR_}{album}{_SPLITCHAR_}{comment}";
		}

		public void CopyTo(MediaItem item)
		{
			item.ID = ID;
			item.FilePath = FilePath;
			item.Title = Title;
			item.Artist = Artist;
			item.Album = Album;
			item.Comment = Comment;
			item.SearchHint = SearchHint;
			item.CreatedDate = CreatedDate;
			item.LastWrite = LastWrite;
			item.LastUpdate = LastUpdate;
			item.LastPlay = LastPlay;
			item.PlayCount = PlayCount;
			item.SelectCount = SelectCount;
			item.SkipCount = SkipCount;
			item.IsFavorite = IsFavorite;
			item.IsNotExist = IsNotExist;
			item._play_error_reason = _play_error_reason;
		}

		#region 検索用文字変換(& cctor)

		// 文字比較用フラグ
		static readonly VbStrConv StrConvFlag;

		static MediaItem()
		{
			//=[ StrConvFlag ]=====
			// 全部使える？ >> Lowercase(All) + Narrow(Asia) + Katakana(JP)
			var flag = VbStrConv.Lowercase | VbStrConv.Narrow | VbStrConv.Katakana;
			try { Strings.StrConv("test", flag, 0); goto Finish; }
			catch (ArgumentException) { }
			// 日本語ロケールサポートしてねーし…。 >> Lowercase(All) + Narrow(Asia)
			flag = VbStrConv.Lowercase | VbStrConv.Narrow;
			try { Strings.StrConv("test", flag, 0); goto Finish; }
			catch (ArgumentException) { }
			// アジアロケールもサポートなしっすか…。 >> Lowercase(All)
			flag = VbStrConv.Lowercase;
			Finish:
			StrConvFlag = flag;
		}

		/// <summary>
		/// 文字を[小文字][半角][カタカナ]に変換。
		/// 半角にはアジアロケール、カタカナには日本ロケールが必要。
		/// 非対応の場合は[小][半]->[小]の順で機能が減少。
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string StrConv_LowerHankakuKana(string value)
		{
			return Strings.StrConv(value, StrConvFlag, 0);
		}

		#endregion
	}

	// MediaItemのファイルパス関連操作の高速化を目的としたキャッシュクラス
	public static class MediaItemExtension
	{
		public static void ClearAllCache()
		{
			lock (filepath_dir_cache)
			{
				filepath_dir_cache.Clear();
			}
			lock (filedir_hash_cache)
			{
				filedir_hash_cache.Clear();
			}
			lock (dirpath_hash_cache)
			{
				dirpath_hash_cache.Clear();
			}
		}

		// FilePathDir
		static Dictionary<MediaItem, string> filepath_dir_cache = new Dictionary<MediaItem, string>();
		public static string GetFilePathDir(this MediaItem item, bool cache_recreate = false)
		{
			if (item == null) return null;

			string ret;
			lock (filepath_dir_cache)
			{
				if (cache_recreate)
				{
					filepath_dir_cache.Remove(item);
				}
				if (filepath_dir_cache.TryGetValue(item, out ret) == false)
				{
					ret = Path.GetDirectoryName(item.FilePath);
					filepath_dir_cache.Add(item, ret);
				}
			}
			return ret;
		}

		// FileDirHashes
		static Dictionary<string, HashSet<int>> filedir_hash_cache = new Dictionary<string, HashSet<int>>();
		static Dictionary<string, int> dirpath_hash_cache = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);

		// item.FilePathにpathから始まるパスが含まれているかチェック
		public static bool ContainsDirPath(this MediaItem item, string path)
		{
			return ContainsDirPath(item.GetFilePathDir(), path);
		}
		public static bool ContainsDirPath(string item_path, string path)
		{
			HashSet<int> item_hash;
			lock (filedir_hash_cache)
			{
				if (filedir_hash_cache.TryGetValue(item_path, out item_hash) == false)
				{
					var tmp = item_path;
					item_hash = new HashSet<int>();
					do
					{
						item_hash.Add(StringComparer.CurrentCultureIgnoreCase.GetHashCode(tmp));
						tmp = Path.GetDirectoryName(tmp);
					}
					while (string.IsNullOrEmpty(tmp) == false);
					filedir_hash_cache.Add(item_path, item_hash);
				}
			}
			int path_hash;
			lock (dirpath_hash_cache)
			{
				if (dirpath_hash_cache.TryGetValue(path, out path_hash) == false)
				{
					path_hash = StringComparer.CurrentCultureIgnoreCase.GetHashCode(path);
					dirpath_hash_cache.Add(path, path_hash);
				}
			}
			return item_hash.Contains(path_hash);
		}
	}
}
