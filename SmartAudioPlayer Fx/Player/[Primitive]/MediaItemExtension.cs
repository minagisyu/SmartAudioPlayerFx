using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.IO;

namespace SmartAudioPlayerFx.Player
{
	// MediaItemのファイルパス関連操作の高速化を目的としたキャッシュクラス
	static class MediaItemExtension
	{
		public static void ClearAllCache()
		{
			filepath_dir_cache.Clear();
			filedir_hash_cache.Clear();
		}

		// FilePathDir
		static ConcurrentDictionary<MediaItem, string> filepath_dir_cache
			= new ConcurrentDictionary<MediaItem, string>();
		public static string GetFilePathDir(this MediaItem item, bool cache_recreate = false)
		{
			string ret;
			if(filepath_dir_cache.TryGetValue(item, out ret) == false)
			{
				ret = Path.GetDirectoryName(item.FilePath);
				filepath_dir_cache.TryAdd(item, ret);
			}
			return ret;
		}

		// FileDirHashes
		static ConcurrentDictionary<string, HashSet<int>> filedir_hash_cache
			= new ConcurrentDictionary<string, HashSet<int>>();
		static ConcurrentDictionary<string, int> dirpath_hash_cache
			= new ConcurrentDictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);

		// item.FilePathにpathから始まるパスが含まれているかチェック
		public static bool ContainsDirPath(this MediaItem item, string path)
		{
			return ContainsDirPath(item.GetFilePathDir(), path);
		}

		public static bool ContainsDirPath(string item_path, string path)
		{
			HashSet<int> item_hash;
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
				filedir_hash_cache.TryAdd(item_path, item_hash);
			}
			//
			int path_hash;
			if (dirpath_hash_cache.TryGetValue(path, out path_hash) == false)
			{
				path_hash = StringComparer.CurrentCultureIgnoreCase.GetHashCode(path);
				dirpath_hash_cache.TryAdd(path, path_hash);
			}

			return item_hash.Contains(path_hash);
		}

	}
}
