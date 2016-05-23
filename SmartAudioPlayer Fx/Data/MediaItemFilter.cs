using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartAudioPlayerFx.Data;

namespace SmartAudioPlayerFx.Data
{
	/// <summary>
	/// 拡張子と単語によるファイルフィルタリング
	/// </summary>
	sealed class MediaItemFilter
	{
		Dictionary<string, object> extlist_cache;
		Dictionary<string, object> wordlist_cache;

		public MediaItemFilter()
		{
			extlist_cache = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
			wordlist_cache = new Dictionary<string, object>();
			SetAcceptExtensions();
			SetIgnoreWords();
		}

		/// <summary>
		/// 許可される拡張子リスト
		/// </summary>
		public AcceptExtension[] AcceptExtensions { get; private set; }
		/// <summary>
		/// 許可される拡張子のリストを設定。
		/// リストが空の場合、デフォルト項目が追加されます
		/// </summary>
		/// <param name="extensions"></param>
		public void SetAcceptExtensions(params AcceptExtension[] extensions)
		{
			AcceptExtensions = (extensions ?? new AcceptExtension[0])
				.Where(i => !string.IsNullOrWhiteSpace(i.Extension))
				.OrderBy(i => i.Extension, StringComparer.CurrentCultureIgnoreCase)
				.ToArray();
			if (AcceptExtensions.Length == 0)
				AcceptExtensions = AcceptExtension.GetDefaultExtensions();
			extlist_cache = AcceptExtensions
				.Where(ext => ext.IsEnable)
				.ToDictionary(k => k.Extension, k => (object)null, StringComparer.CurrentCultureIgnoreCase);
		}

		/// <summary>
		/// 無視される単語リスト
		/// </summary>
		public IgnoreWord[] IgnoreWords { get; private set; }
		/// <summary>
		/// 無視される単語リストを設定
		/// </summary>
		/// <param name="words"></param>
		public void SetIgnoreWords(params IgnoreWord[] words)
		{
			IgnoreWords = (words ?? new IgnoreWord[0])
				.Where(i => !string.IsNullOrWhiteSpace(i.Word))
				.OrderBy(i => i.Word, StringComparer.CurrentCultureIgnoreCase)
				.ToArray();
			wordlist_cache = IgnoreWords
				.Where(w => w.IsEnable)
				.ToDictionary(k => MediaItem.StrConv_LowerHankakuKana(k.Word), KeyNotFoundException => (object)null);
		}

		/// <summary>
		/// 現在の設定を元にフィルタリング検証する関数。
		/// true=有効 : false=無効(フィルタリング対象)
		/// </summary>
		/// <returns></returns>
		public bool Validate(MediaItem item)
		{
			// 拡張子リストに含まれていて、MediaItem.SearchHintに単語がすべて含まれないならtrue
			return
				extlist_cache.ContainsKey(Path.GetExtension(item.FilePath)) &&
				wordlist_cache.All(w => !item.SearchHint.Contains(w.Key));
		}

		#region Definitions

		/// <summary>
		/// 許可される拡張子
		/// </summary>
		public class AcceptExtension
		{
			/// <summary>
			/// この項目は有効？
			/// </summary>
			public bool IsEnable { get; private set; }
			/// <summary>
			/// ピリオドから始まる拡張子
			/// </summary>
			public string Extension { get; private set; }

			public AcceptExtension(bool is_enable, string extension)
			{
				this.IsEnable = is_enable;
				this.Extension = extension;
			}

			#region DefaultExtensions

			/// <summary>
			/// デフォルト拡張子
			/// </summary>
			/// <returns></returns>
			public static AcceptExtension[] GetDefaultExtensions()
			{
				return new[]
				{
					// WindowsMedia
					new AcceptExtension(true, ".asf"), new AcceptExtension(true, ".wma"), new AcceptExtension(true, ".wmv"),
					// Windows
					new AcceptExtension(true, ".avi"), new AcceptExtension(true, ".wav"), new AcceptExtension(true, ".mid"),
					new AcceptExtension(true, ".midi"), new AcceptExtension(true, ".smf"),
					// Mpeg
					new AcceptExtension(true, ".mpe"), new AcceptExtension(true, ".mpeg"), new AcceptExtension(true, ".mpg"),
					new AcceptExtension(true, ".mpa"), new AcceptExtension(true, ".mp2"), new AcceptExtension(true, ".m2a"),
					new AcceptExtension(true, ".m2v"), new AcceptExtension(true, ".mp3"), new AcceptExtension(true, ".mp4"),
					new AcceptExtension(true, ".m4a"), new AcceptExtension(true, ".m4v"),
					// Ogg
					new AcceptExtension(true, ".ogg"), new AcceptExtension(true, ".ogm"),
					// Matroska
					new AcceptExtension(true, ".mkv"), new AcceptExtension(true, ".mka"),
					// Other
					new AcceptExtension(true, ".ac3"), new AcceptExtension(true, ".dts"), new AcceptExtension(true, ".aac"),
					new AcceptExtension(true, ".flv"), new AcceptExtension(true, ".divx"),
					//
				};
			}

			#endregion
		}

		/// <summary>
		/// 無視される単語
		/// </summary>
		public class IgnoreWord
		{
			/// <summary>
			/// この項目は有効？
			/// </summary>
			public bool IsEnable { get; private set; }
			/// <summary>
			/// 単語
			/// </summary>
			public string Word { get; private set; }

			public IgnoreWord(bool is_enable, string word)
			{
				this.IsEnable = is_enable;
				this.Word = word;
			}
		}

		#endregion
	}
}
