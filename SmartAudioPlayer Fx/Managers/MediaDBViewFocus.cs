using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Xml.Linq;
using SmartAudioPlayerFx.Managers;
using Reactive.Bindings.Extensions;
using Quala;
using Quala.Extensions;

namespace SmartAudioPlayerFx.Data
{
	// マーキング用
	public interface ISpecialMediaDBViewFocus
	{
	}

	// MediaDBViewの内容をフィルタリングする
	// - FocusPath:
	//   - 設定するとViewMode=Defaultに設定
	// - ViewMode:
	//   - Default: FocusPathによるフィルタ
	//   - 他: _view.Itemsから特定の物を対象にする、FocusPathの設定値は無視
	//
	public class MediaDBViewFocus : IDisposable
	{
		public string FocusPath { get; private set; }
		public VersionedCollection<MediaItem> Items { get; private set; }
		readonly CompositeDisposable _disposables;

		public MediaDBViewFocus(string focusPath, bool loadItems = true)
		{
			FocusPath = focusPath;
			Items = new VersionedCollection<MediaItem>(
				new CustomEqualityComparer<MediaItem>(x => x.ID.GetHashCode(), (x, y) => x.ID == y.ID));
			_disposables = new CompositeDisposable();

			ManagerServices.MediaDBViewManager.Items	// SerialDisposableつかえる？
				.GetNotifyObservable()
				.Subscribe(x =>
				{
					if (x.Item != null && ValidateItem(x.Item))
					{
						if (x.Type == VersionedCollection<MediaItem>.NotifyType.Add ||
							x.Type == VersionedCollection<MediaItem>.NotifyType.Update)
						{
							this.Items.AddOrReplace(x.Item);
						}
						else if (x.Type == VersionedCollection<MediaItem>.NotifyType.Remove)
						{
							this.Items.Remove(x.Item);
						}
					}
					// Clearは無条件で対応
					if (x.Type == VersionedCollection<MediaItem>.NotifyType.Clear)
					{
						this.Items.Clear();
					}
				})
				.AddTo(_disposables);

			if(loadItems)
				LoadViewItems();
		}

		public virtual void Dispose()
		{
			_disposables.Dispose();
			GC.SuppressFinalize(this);
		}

		protected virtual bool ValidateItem(MediaItem item)
		{
			var fpath = FocusPath;
			if (string.IsNullOrWhiteSpace(fpath)) return false;
			return item.ContainsDirPath(fpath);
		}

		#region LoadViewItems

		protected void LoadViewItems()
		{
			AppService.Log.AddDebugLog("Call LoadViewItems[{0}]", this.GetHashCode());

			// 初期化
			Items.Clear();
			var fpath = FocusPath;

			// パスが空なら空を返す
			// 検索処理中止の為にFocusPath=nullが許容されるため
			if (string.IsNullOrWhiteSpace(fpath)) return;

			// パスが無効なので例外
			// nullは許容されるためこの位置で判定する
			if (Path.IsPathRooted(fpath) == false) return;

			// parentから読み込み
			var sw = Stopwatch.StartNew();
			ManagerServices.MediaDBViewManager.Items
				.GetLatest()
				.AsParallel()
				.Where(x => ValidateItem(x))
				.ForAll(x => Items.AddOrReplace(x));
			sw.Stop();
			AppService.Log.AddDebugLog(" **LoadViewItems[{0}]({1}items): {2}ms", this.GetHashCode(), Items.Count, sw.ElapsedMilliseconds);
		}

		#endregion
	}

	sealed class MediaDBViewFocus_FavoriteOnly : MediaDBViewFocus, ISpecialMediaDBViewFocus
	{
		public MediaDBViewFocus_FavoriteOnly(string focusPath) : base(focusPath) { }
		protected override bool ValidateItem(MediaItem item)
		{
			return base.ValidateItem(item) && item.IsFavorite;
		}
	}
	sealed class MediaDBViewFocus_NonPlayedOnly : MediaDBViewFocus, ISpecialMediaDBViewFocus
	{
		public MediaDBViewFocus_NonPlayedOnly(string focusPath) : base(focusPath) { }
		protected override bool ValidateItem(MediaItem item)
		{
			return base.ValidateItem(item) && (item.PlayCount == 0);
		}
	}
	sealed class MediaDBViewFocus_LatestAddOnly : MediaDBViewFocus, ISpecialMediaDBViewFocus
	{
		public static int RecentIntervalDays = 60;

		public MediaDBViewFocus_LatestAddOnly(string focusPath) : base(focusPath) { }
		protected override bool ValidateItem(MediaItem item)
		{
			var days = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - item.CreatedDate).Days;
			return base.ValidateItem(item) && (days < RecentIntervalDays);
		}
	}
	sealed class MediaDBViewFocus_SearchWord : MediaDBViewFocus, ISpecialMediaDBViewFocus
	{
		public string SearchWord { get; private set; }
		string[] _splittedWords;

		public MediaDBViewFocus_SearchWord(string focusPath, string word)
			: base(focusPath, false)
		{
			SearchWord = word;
			_splittedWords = string.IsNullOrWhiteSpace(word) ?
					new string[0] :
					MediaItem.StrConv_LowerHankakuKana(word).Split(' ');
			LoadViewItems();
		}
		protected override bool ValidateItem(MediaItem item)
		{
			if (string.IsNullOrWhiteSpace(SearchWord)) return true;

			var result = _splittedWords.All(x => item.SearchHint.Contains(x));
			return base.ValidateItem(item) && result;
		}
	}
}
