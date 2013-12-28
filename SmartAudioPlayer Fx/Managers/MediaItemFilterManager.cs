using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Xml.Linq;
using WinAPIs;
using Codeplex.Reactive.Extensions;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayer;

namespace SmartAudioPlayerFx.Managers
{
	/// <summary>
	/// 拡張子と単語によるファイルフィルタリング
	/// </summary>
	[Require(typeof(PreferencesManager))]
	sealed class MediaItemFilterManager : IDisposable
	{
		readonly object lockObj = new object();
		HashSet<string> extlist_cache = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
		string[] wordlist_cache = new string[0];
		public event Action<string> PropertyChanged;
		readonly CompositeDisposable _disposables = new CompositeDisposable();

		public MediaItemFilterManager()
		{
			Reset();

			ManagerServices.PreferencesManager.PlayerSettings
				.Subscribe(x => LoadPreferences(x))
				.AddTo(_disposables);
			ManagerServices.PreferencesManager.SerializeRequestAsObservable()
				.Subscribe(_ => SavePreferences(ManagerServices.PreferencesManager.PlayerSettings.Value))
				.AddTo(_disposables);
		}
		public void Dispose()
		{
			_disposables.Dispose();
			PropertyChanged = null;
		}

		void LoadPreferences(XElement element)
		{
			SetAcceptExtensions(
				element.GetArrayValues<AcceptExtension>("AcceptExtensions",
					el => new AcceptExtension(el.GetAttributeValueEx<bool>("IsEnable", false),
						el.GetAttributeValueEx<string>("Extension", null)))
				.ToArray<AcceptExtension>());
			SetIgnoreWords(
				element.GetArrayValues<IgnoreWord>("IgnoreWords",
					el => new IgnoreWord(el.GetAttributeValueEx<bool>("IsEnable", false),
						el.GetAttributeValueEx<string>("Word", null))).ToArray<IgnoreWord>());
		}
		void SavePreferences(XElement element)
		{
			element
				.SubElement("AcceptExtensions", true, elm =>
				{
						elm.RemoveAll();
						AcceptExtensions.ForEach(i =>
						{
								elm
										// 同じExtension属性の値を持つ最後のElementを選択、なければ作る
										.GetOrCreateElement("Item", m => m.Attributes("Extension")
												.Any(n => string.Equals(n.Value, i.Extension, StringComparison.CurrentCultureIgnoreCase)))
										.SetAttributeValueEx(() => i.IsEnable)
										.SetAttributeValueEx(() => i.Extension);
						});
				})
				.SubElement("IgnoreWords", true, elm =>
                {
                        elm.RemoveAll();
                        IgnoreWords.ForEach(i =>
                        {
                                elm
                                        // 同じWord属性の値を持つ最後のElementを選択、なければ作る
                                        .GetOrCreateElement("Item",
                                                m => m.Attributes("Word")
                                                        .Any(n => string.Equals(n.Value, i.Word, StringComparison.CurrentCultureIgnoreCase)))
                                        .SetAttributeValueEx("IsEnable", i.IsEnable)
                                        .SetAttributeValueEx("Word", i.Word);
                        });
                });
		}

		public void Reset()
		{
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
			lock (lockObj)
			{
				AcceptExtensions = (extensions ?? new AcceptExtension[0])
					.Where(i => !string.IsNullOrWhiteSpace(i.Extension))
					.OrderBy(i => i.Extension, StringComparer.CurrentCultureIgnoreCase)
					.ToArray();
				if (AcceptExtensions.Length == 0)
					AcceptExtensions = AcceptExtension.GetDefaultExtensions();
				extlist_cache.Clear();
				AcceptExtensions
					.Where(x => x.IsEnable)
					.Select(x => x.Extension)
					.ForEach(x => extlist_cache.Add(x));
			}
			if (PropertyChanged != null)
				PropertyChanged("AcceptExtensions");
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
			lock (lockObj)
			{
				IgnoreWords = (words ?? new IgnoreWord[0])
					.Where(i => !string.IsNullOrWhiteSpace(i.Word))
					.OrderBy(i => i.Word, StringComparer.CurrentCultureIgnoreCase)
					.ToArray();
				wordlist_cache = IgnoreWords
					.Where(x => x.IsEnable)
					.Select(x => MediaItemExtension.StrConv_LowerHankakuKana(x.Word))
					.ToArray();
			}
			if (PropertyChanged != null)
				PropertyChanged("IgnoreWords");
		}

		/// <summary>
		/// 現在の設定を元にフィルタリング検証する関数。
		/// true=有効 : false=無効(フィルタリング対象)
		/// </summary>
		/// <returns></returns>
		public bool Validate(MediaItem item)
		{
			// 拡張子リストに含まれていて、MediaItem.SearchHintに単語がすべて含まれないならtrue
			lock (lockObj)
			{
				return
					extlist_cache.Contains(Path.GetExtension(item.FilePath)) &&
					wordlist_cache.All(x => item.SearchHint.Contains(x) == false);
			}
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

	static class MediaItemFilterManagerExtensions
	{
		public static IObservable<string> PropertyChangedAsObservable(this MediaItemFilterManager manager)
		{
			return Observable.FromEvent<string>(
				v => manager.PropertyChanged += v,
				v => manager.PropertyChanged -= v);
		}
	}
}
