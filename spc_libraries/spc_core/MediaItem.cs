using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic;

namespace SmartAudioPlayer
{
	[DebuggerDisplay("{Title}")]
	public sealed class MediaItem
	{
		// Memo: SAPFx 3.2.0.1以前のQuala.dllの実装ミスにより
		//       DBカラムが追加されると例外落ちして読み込めなくなるのでカラムの追加は不可能
		//       互換性を維持するためには、別のテーブルにデータを置く必要がある
		public string _play_error_reason;

		public long ID;				// ID(自動生成/Primary key)
		public string FilePath;		// ファイルパス(indexed, unique, collate nocase)
		public string Title;		// タイトル
		public string Artist;		// アーティスト
		public string Album;		// アルバム
		public string Comment;		// コメント
		public string SearchHint;	// 検索ヒント
		public long CreatedDate;	// ファイル作成日(UTC ticks)
		public long LastWrite;		// ファイル最終書き込み日(UTC ticks)
		public long LastUpdate;		// DB最終更新日(UTC ticks)
		public long LastPlay;		// 最終再生日(UTC ticks)
		public long PlayCount;		// 再生数
		public long SelectCount;	// 選択数
		public long SkipCount;		// スキップ数
		public bool IsFavorite;		// お気に入り？
		public bool IsNotExist;		// ファイルが存在しない

		public MediaItem() { }
		public MediaItem(string path)
		{
			var fi = new FileInfo(path);
			this.ID = 0;
			this.FilePath = fi.FullName;
			this.Title = fi.Name;
			this.Artist = string.Empty;
			this.Album = string.Empty;
			this.Comment = string.Empty;
			this.UpdateSearchHint();
			this.CreatedDate = ((fi.Exists) ? fi.CreationTimeUtc : DateTime.MinValue).Ticks;
			this.LastWrite = ((fi.Exists) ? fi.LastWriteTimeUtc : DateTime.MinValue).Ticks;
			this.LastUpdate = DateTime.UtcNow.Ticks;
			this.LastPlay = DateTime.MinValue.Ticks;
			this.PlayCount = 0;
			this.SkipCount = 0;
			this.IsFavorite = false;
			this.IsNotExist = !fi.Exists;
		}
	}

	public static class MediaItemExtension
	{
		public const char SearchHintSplitChar = '\b';
		public static void UpdateSearchHint(this MediaItem item)
		{
			// filepath, title, artist, album, commentを半角小文字カナに変換して\bで連結
			var filepath = StrConv_LowerHankakuKana(item.FilePath);
			var title = StrConv_LowerHankakuKana(item.Title);
			var artist = StrConv_LowerHankakuKana(item.Artist);
			var album = StrConv_LowerHankakuKana(item.Album);
			var comment = StrConv_LowerHankakuKana(item.Comment);
			item.SearchHint = string.Format(
				"{0}" + SearchHintSplitChar +
				"{1}" + SearchHintSplitChar +
				"{2}" + SearchHintSplitChar +
				"{3}" + SearchHintSplitChar +
				"{4}",
				filepath, title, artist, album, comment);
		}

		public static void CopyTo(this MediaItem item, MediaItem other)
		{
			other.ID = item.ID;
			other.FilePath = item.FilePath;
			other.Title = item.Title;
			other.Artist = item.Artist;
			other.Album = item.Album;
			other.Comment = item.Comment;
			other.SearchHint = item.SearchHint;
			other.CreatedDate = item.CreatedDate;
			other.LastWrite = item.LastWrite;
			other.LastUpdate = item.LastUpdate;
			other.LastPlay = item.LastPlay;
			other.PlayCount = item.PlayCount;
			other.SelectCount = item.SelectCount;
			other.SkipCount = item.SkipCount;
			other.IsFavorite = item.IsFavorite;
			other.IsNotExist = item.IsNotExist;
			other._play_error_reason = item._play_error_reason;
		}

		#region 検索用文字変換(& cctor)

		// 文字比較用フラグ
		static readonly VbStrConv StrConvFlag;

		static MediaItemExtension()
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
	public static class MediaItemCache
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

	public sealed class MediaItemIDEqualityComparer : EqualityComparer<MediaItem>
	{
		public override bool Equals(MediaItem x, MediaItem y)
		{
			return x.ID == y.ID;
		}

		public override int GetHashCode(MediaItem obj)
		{
			return obj.ID.GetHashCode();
		}
	}
}
