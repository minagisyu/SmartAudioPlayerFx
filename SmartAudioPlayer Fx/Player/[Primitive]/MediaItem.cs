using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using Microsoft.VisualBasic;
using Quala;
using Quala.Data;
using System.Collections.Generic;

namespace SmartAudioPlayerFx.Player
{
	[DebuggerDisplay("{Title}")]
	[SimpleDB.TableName("media")]
	sealed class MediaItem
	{
		[SimpleDB.Ignore]
		public const char SearchHintSplitChar = '\b';
		[SimpleDB.Ignore]
		public string _play_error_reason;

		[SimpleDB.PrimaryKey]
		public long ID;				// ID(自動生成)
		[SimpleDB.Indexed, SimpleDB.Unique, SimpleDB.Collate("NOCASE")]
		public string FilePath;		// ファイルパス
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

		public static MediaItem CreateDefault(string path)
		{
			var fi = new FileInfo(path);
			var item = new MediaItem();
			item.ID = 0;
			item.FilePath = fi.FullName;
			item.Title = fi.Name;
			item.Artist = string.Empty;
			item.Album = string.Empty;
			item.Comment = string.Empty;
			item.UpdateSearchHint();
			item.CreatedDate = ((fi.Exists) ? fi.CreationTimeUtc : DateTime.MinValue).Ticks;
			item.LastWrite = ((fi.Exists) ? fi.LastWriteTimeUtc : DateTime.MinValue).Ticks;
			item.LastUpdate = DateTime.UtcNow.Ticks;
			item.LastPlay = DateTime.MinValue.Ticks;
			item.PlayCount = 0;
			item.SkipCount = 0;
			item.IsFavorite = false;
			item.IsNotExist = !fi.Exists;
			return item;
		}

		public void UpdateSearchHint()
		{
			// filepath, title, artist, album, commentを半角小文字カナに変換して\bで連結
			var filepath = StrConv_LowerHankakuKana(this.FilePath);
			var title = StrConv_LowerHankakuKana(this.Title);
			var artist = StrConv_LowerHankakuKana(this.Artist);
			var album = StrConv_LowerHankakuKana(this.Album);
			var comment = StrConv_LowerHankakuKana(this.Comment);
			this.SearchHint = string.Format(
				"{0}" + SearchHintSplitChar +
				"{1}" + SearchHintSplitChar +
				"{2}" + SearchHintSplitChar +
				"{3}" + SearchHintSplitChar +
				"{4}",
				filepath, title, artist, album, comment);
		}

		public void CopyTo(MediaItem item)
		{
			item.ID = this.ID;
			item.FilePath = this.FilePath;
			item.Title = this.Title;
			item.Artist = this.Artist;
			item.Album = this.Album;
			item.Comment = this.Comment;
			item.SearchHint = this.SearchHint;
			item.CreatedDate = this.CreatedDate;
			item.LastWrite = this.LastWrite;
			item.LastUpdate = this.LastUpdate;
			item.LastPlay = this.LastPlay;
			item.PlayCount = this.PlayCount;
			item.SelectCount = this.SelectCount;
			item.SkipCount = this.SkipCount;
			item.IsFavorite = this.IsFavorite;
			item.IsNotExist = this.IsNotExist;
			item._play_error_reason = this._play_error_reason;
		}

		public XElement ToXML()
		{
			var elm = new XElement("media");
			elm.SetAttributeValue("ID", ID);
			elm.SetAttributeValue("FilePath", FilePath);
			elm.SetAttributeValue("Title", Title);
			elm.SetAttributeValue("Artist", Artist);
			elm.SetAttributeValue("Album", Album);
			elm.SetAttributeValue("Comment", Comment);
			elm.SetAttributeValue("SearchHint", SearchHint);
			elm.SetAttributeValue("CreatedDate", CreatedDate);
			elm.SetAttributeValue("LastWrite", LastWrite);
			elm.SetAttributeValue("LastUpdate", LastUpdate);
			elm.SetAttributeValue("LastPlay", LastPlay);
			elm.SetAttributeValue("PlayCount", PlayCount);
			elm.SetAttributeValue("SelectCount", SelectCount);
			elm.SetAttributeValue("SkipCount", SkipCount);
			elm.SetAttributeValue("IsFavorite", IsFavorite);
			elm.SetAttributeValue("IsNotExist", IsNotExist);
			return elm;
		}

		#region 検索用文字変換(& cctor)

		// 文字比較用フラグ
		[SimpleDB.Ignore]
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
}
