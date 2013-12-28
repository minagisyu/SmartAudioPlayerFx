using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using WinAPIs;
using Codeplex.Reactive;
using Codeplex.Reactive.Extensions;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayer;

namespace SmartAudioPlayerFx.Managers
{
	[Require(typeof(MediaDBManager))]
	[Require(typeof(PreferencesManager))]
	[Require(typeof(MediaItemFilterManager))]
	sealed class MediaDBViewManager : IDisposable
	{
		#region ctor

		public ReactiveProperty<string> FocusPath { get; private set; }
		public VersionedCollection<MediaItem> Items { get; private set; }
		readonly CompositeDisposable _disposables;

		public MediaDBViewManager()
		{
			FocusPath = new ReactiveProperty<string>(mode: ReactivePropertyMode.RaiseLatestValueOnSubscribe);
			Items = new VersionedCollection<MediaItem>(new MediaItemIDEqualityComparer());
			_disposables = new CompositeDisposable(FocusPath);

			// Preferences
			ManagerServices.PreferencesManager.PlayerSettings
				.Subscribe(x => LoadPreferences(x))
				.AddTo(_disposables);
			ManagerServices.PreferencesManager.SerializeRequestAsObservable()
				.Subscribe(_ => SavePreferences(ManagerServices.PreferencesManager.PlayerSettings.Value))
				.AddTo(_disposables);

			// MediaItemFilter
			// 最後の通知から500ms後にItemsの再検証
			ManagerServices.MediaItemFilterManager.PropertyChangedAsObservable()
				.Throttle(TimeSpan.FromMilliseconds(500))
				.Subscribe(async _ => await RevalidateItems(FocusPath.Value))
				.AddTo(_disposables);

			// FocusPathが変更されたらItemsを再設定
			FocusPath
				.Subscribe(async x => await LoadDBItems(x))
				.AddTo(_disposables);
		}
		public void Dispose()
		{
			Task.Run(async () => await StopAllAsyncProcess());
			_disposables.Dispose();

			ItemsCollecting = null;
			ItemCollect_CoreScanFinished = null;
			ItemCollect_ScanFinished = null;
			ItemsLoaded = null;
		}
		public async Task StopAllAsyncProcess()
		{
			await LoadDBItems(null);
			await RevalidateItems(null);
			await CollectFiles(null);
			RaiseDBUpdate(null);
			RaiseDBInsert(null);
		}

		void LoadPreferences(XElement element)
		{
			FocusPath.Value = element.GetAttributeValueEx("FocusPath", default(string));
		}
		void SavePreferences(XElement element)
		{
			element
				.SetAttributeValueEx("FocusPath", FocusPath.Value);
		}

		#endregion

		/// <summary>
		/// CollectItems()処理中に呼び出されます。
		/// 状態テキストは「[n] フォルダパス」という形式です。
		/// [0] - DB読み込み中 (CollectItems)
		/// [1] - ファイル検索中 (CollectStart phase1)
		/// [2] - ファイル生存確認中 (CollectStart phase2)
		/// [3] - タグ情報取得中 (CollectStart phase3)
		/// </summary>
		public event Action<string> ItemsCollecting;

		/// <summary>
		/// ファイル検索処理中に、検索と存在確認がすんだ時点で呼び出されます。
		/// タグ情報の取得は時間がかかるので、その救済措置。
		/// </summary>
		public event Action ItemCollect_CoreScanFinished;

		/// <summary>
		/// ファイル検索処理が終了した時点で呼び出されます。
		/// ただしファイルシステムの変更監視は続行しています。
		/// </summary>
		public event Action ItemCollect_ScanFinished;

		/// <summary>
		/// アイテムの読み込みが終了した時点で呼び出されます。
		/// ただしファイルシステムの検索はまだ終わっていません。
		/// </summary>
		public event Action ItemsLoaded;

		/// <summary>
		/// 指定ファイルパスのデータを取得します。
		/// DBにアイテムが無いときは新規に作成され、DBに存在するがファイルがない場合は情報が更新されます。
		/// さらに、バックグラウンドでタグ情報を取得、更新されます。
		/// ファイルが存在しない時はnullが返ります
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		public MediaItem GetOrCreate(string filepath)
		{
			Logger.AddDebugLog("Call GetOrCreate: filepath={0}", filepath);
			if (string.IsNullOrWhiteSpace(filepath)) return null;

			// キャッシュ検索、無い場合はDBを検索
			var item = Items.GetLatest()
				.Where(x => filepath.Equals(x.FilePath, StringComparison.InvariantCultureIgnoreCase))
				.FirstOrDefault()
				?? ManagerServices.MediaDBManager.Get(filepath);

			// リアルファイルは存在する？
			var exists = File.Exists(filepath);

			// ファイルがDBに無いので追加(ファイル無いときは何もしない)
			if (item == null)
			{
				if (exists == false) return null;
				item = new MediaItem(filepath);
				RaiseDBInsert(item);
			}
			else if (exists == false)
			{
				// ファイルが消されたっぽいのでIsNotExist更新通知
				item.IsNotExist = true;
				item.LastUpdate = DateTime.UtcNow.Ticks;
				RaiseDBUpdate(item, _ => _.IsNotExist, _ => _.LastUpdate);
			}
			return item;
		}

		#region RaiseDBUpdate / RaiseDBInsert

		/// <summary>
		/// itemの指定プロパティの情報をDBへ書き込みます
		/// </summary>
		/// <param name="item"></param>
		/// <param name="columns"></param>
		public void RaiseDBUpdate(MediaItem item, params Expression<Func<MediaItem, object>>[] columns)
		{
			if (item == null) return;
			if (item.ID == 0) return;

			using (var dbaction = ManagerServices.MediaDBManager.BeginTransaction())
			{
				dbaction.Update(new[] { item }, columns)
					.AsParallel()
					.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
					.ForAll(x => Items.AddOrReplace(x));
				dbaction.Commit();
			}
		}

		public void RaiseDBInsert(MediaItem item)
		{
			if (item == null) return;

			using (var dbaction = ManagerServices.MediaDBManager.BeginTransaction())
			{
				dbaction.Insert(new[] { item })
					.AsParallel()
					.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
					.ForAll(x => Items.AddOrReplace(x));
				dbaction.Commit();
			}
		}

		#endregion
		#region LoadDBItems

		CancellationTokenSource _LoadDBItems_CTS;

		async Task LoadDBItems(string path)
		{
			Logger.AddDebugLog("Call LoadDBItems: path={0}", path);

			// 以前の非同期処理をキャンセルして終了を待つ
			if (_LoadDBItems_CTS != null)
				_LoadDBItems_CTS.Cancel();

			// 初期化
			_LoadDBItems_CTS = new CancellationTokenSource();
			Items.Clear();
			MediaItemCache.ClearAllCache();

			// 読み込み
			var ct = _LoadDBItems_CTS.Token;
			var itemsLoaded_ev = ItemsLoaded;
			await Task.Run(() =>
			{
				if (string.IsNullOrWhiteSpace(path)) return;
				if (Path.IsPathRooted(path) == false) return;

				var sw = Stopwatch.StartNew();
				var cd = new ConcurrentDictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
				var notify = ItemsCollecting;

				ManagerServices.MediaDBManager.GetFromFilePath_ExistsOnly(path)
					.TakeWhile(_ => ct.IsCancellationRequested == false)
					.AsParallel()
					.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
					.ForAll(x =>
					{
						var dir = x.GetFilePathDir();
						if (cd.TryAdd(dir, null) && notify != null)
						{
							notify("[0] " + dir);
						}
						Items.AddOrReplace(x);
					});
				sw.Stop();
				Logger.AddDebugLog(" **LoadDBItems({0}items): {1}ms", Items.Count, sw.ElapsedMilliseconds);
			})
			.ContinueWith(async _ =>
			{
				await CollectFiles(path);

				if (ct.IsCancellationRequested)
					await CollectFiles(null);
				if (itemsLoaded_ev != null)
					itemsLoaded_ev();
				Logger.AddDebugLog(" **LoadDBItems(full-complete)");
			});

		}

		#endregion
		#region RevalidateItems

		CancellationTokenSource _revalidateItems_CTS;

		async Task RevalidateItems(string path)
		{
			Logger.AddDebugLog("Call RevalidateItems: path={0}", path);

			// 以前の非同期操作をキャンセル
			if (_revalidateItems_CTS != null)
				_revalidateItems_CTS.Cancel();

			_revalidateItems_CTS = new CancellationTokenSource();
			var itemsLoaded_ev = ItemsLoaded;
			var ct = _revalidateItems_CTS.Token;
			await Task.Run(() =>
			{
				if (string.IsNullOrWhiteSpace(path)) return;

				Logger.AddDebugLog(" ..RevalidateTask: items version:{0}", Items.Version);
				var sw = Stopwatch.StartNew();

				// phase remove: 既存の項目に対して、Validate()が通らないものを削除
				Logger.AddDebugLog(" ..RevalidateTask-phase1: version:{0}", Items.Version);
				Items.GetLatest()
					.TakeWhile(_ => ct.IsCancellationRequested == false)
					.AsParallel()
					.Where(x => ManagerServices.MediaItemFilterManager.Validate(x) == false)
					.ForAll(x => Items.Remove(x));

				// phase db_add: DBを再読み込みして追加or更新
				Logger.AddDebugLog(" ..RevalidateTask-phase2: version:{0}", Items.Version);
				ManagerServices.MediaDBManager.GetFromFilePath_ExistsOnly(path)
					.TakeWhile(_ => ct.IsCancellationRequested == false)
					.AsParallel()
					.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
					.ForAll(x => Items.AddOrReplace(x));

				sw.Stop();
				Logger.AddDebugLog(" **RevalidateItems: {0}ms", sw.ElapsedMilliseconds);
			})
			.ContinueWith(async _ =>
			{
				// phase file_add: ファイルシステムを再検索して追加として通知する
				await CollectFiles(path);

				if (ct.IsCancellationRequested)
					await CollectFiles(null);
				if (itemsLoaded_ev != null)
					itemsLoaded_ev();
				Logger.AddDebugLog(" **RevalidateItems(full-complete)");
			});
		}

		#endregion
		#region CollectFiles

		CancellationTokenSource _collectFiles_CTS;

		async Task CollectFiles(string path)
		{
			Logger.AddDebugLog("Call CollectFiles: path={0}", path);

			// 以前の検索をキャンセル
			if (_collectFiles_CTS != null)
				_collectFiles_CTS.Cancel();

			_collectFiles_CTS = new CancellationTokenSource();
			var ct = _collectFiles_CTS.Token;
			var notify = ItemsCollecting;
			var corescan_finished = ItemCollect_CoreScanFinished;
			var scan_finished = ItemCollect_ScanFinished;
			await Task.Run(async () =>
			{
				if (string.IsNullOrWhiteSpace(path) || Directory.Exists(path) == false)
				{
					// イベント発行して終わる
					if (corescan_finished != null)
					{
						corescan_finished();
					}
					if (scan_finished != null)
					{
						scan_finished();
					}
					return;
				}
				Logger.AddDebugLog(" - CollectItems start.");

				// フォルダ監視を有効に
				var fsw = new FileSystemWatcher(path)
				{
					IncludeSubdirectories = true,
					InternalBufferSize = 0x10000,
					NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite,
					EnableRaisingEvents = true,
				};
				using (fsw)
				{
					var sw = Stopwatch.StartNew();

					// phase.1: ファイルシステムを検索してValidate()が通るものをDBへ追加、追加されたものをAdd
					var add_count = 0;
					foreach (var x in FileSystemUtil.EnumerateAllFiles(path, ct))
					{
						if (notify != null)
							notify("[1] " + x.DirectoryName);

						// MEMO:
						// LastWrite = MinValueはphase3で更新する際に識別するためのフラグとして使用する
						var xs = x.Files
							.Select(y => new MediaItem(y) { LastWrite = DateTime.MinValue.Ticks })
							.Where(y => ManagerServices.MediaItemFilterManager.Validate(y));
						using (var dbaction = ManagerServices.MediaDBManager.BeginTransaction())
						{
							dbaction.Insert(xs)
								.AsParallel()
								.ForAll(y =>
								{
									Interlocked.Increment(ref add_count);
									Items.AddOrReplace(y);
								});
							dbaction.Commit();
						}
					}
					sw.Stop();
					Logger.AddDebugLog(" - CollectFiles phase1({0}items add, elapsed {1}ms)", add_count, sw.ElapsedMilliseconds);
					if (notify != null)
						notify(string.Empty);

					// phase.2: ファイルの存在チェック
					sw.Restart();
					var cd = new ConcurrentDictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
					var updateList = new ConcurrentBag<MediaItem>();
					ManagerServices.MediaDBManager.GetFromFilePath(path)
						.TakeWhile(_ => ct.IsCancellationRequested == false)
						.AsParallel()
						.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
						.ForAll(x =>
						{
							var dir = x.GetFilePathDir();
							if (cd.TryAdd(dir, null) && notify != null)
								notify("[2] " + dir);
							if (x.IsNotExist == File.Exists(x.FilePath))
							{
								x.IsNotExist = !x.IsNotExist;
								updateList.Add(x);
								if (x.IsNotExist)
								{
									Items.Remove(x);
								}
								else
								{
									Items.AddOrReplace(x);
								}
							}
						});
					if (notify != null)
						notify(string.Empty);
					if (!updateList.IsEmpty)
					{
						using (var dbaction = ManagerServices.MediaDBManager.BeginTransaction())
						{
							// MEMO:
							// .ToArray()で引っ張ってならないとupdateListの中身が処理されない...
							dbaction.Update(updateList, _ => _.LastUpdate, _ => _.IsNotExist)
								.ToArray();
							dbaction.Commit();
						}
					}
					sw.Stop();
					Logger.AddDebugLog(" - CollectFiles phase2({0}items update, elapsed {1}ms)", updateList.Count, sw.ElapsedMilliseconds);

					// corescan finish
					if (corescan_finished != null)
						corescan_finished();
					ManagerServices.MediaDBManager.Recycle(false);

					// phase.3: taginfo recheck...
					sw.Restart();
					cd.Clear();
					updateList = new ConcurrentBag<MediaItem>();
					ManagerServices.MediaDBManager.GetFromFilePath_ExistsOnly(path)
						.TakeWhile(_ => ct.IsCancellationRequested == false)
						.AsParallel()
						.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
						.ForAll(x =>
						{
							var dir = x.GetFilePathDir();
							if (cd.TryAdd(dir, null) && notify != null)
								notify("[3] " + dir);
							if ((x.LastWrite == DateTime.MinValue.Ticks) ||
								(x.LastWrite < File.GetLastWriteTimeUtc(x.FilePath).Ticks))
							{
								var tag = MediaTagUtil.Get(x.FilePath);
								if (tag.IsTagLoaded)
								{
									x.FilePath = tag.FilePath;
									x.Title = tag.Title;
									x.Artist = tag.Artist;
									x.Album = tag.Album;
									x.Comment = tag.Comment;
									x.UpdateSearchHint();
									x.CreatedDate = File.GetCreationTimeUtc(tag.FilePath).Ticks;
									x.LastWrite = File.GetLastWriteTimeUtc(tag.FilePath).Ticks;
									x.IsNotExist = false;
									x.GetFilePathDir(true);
									updateList.Add(x);
									Items.AddOrReplace(x);
								}
							}
						});
					if (notify != null)
						notify(string.Empty);
					if (!updateList.IsEmpty)
					{
						using (var dbaction = ManagerServices.MediaDBManager.BeginTransaction())
						{
							dbaction.Update(updateList,
								_ => _.FilePath,
								_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
								_ => _.CreatedDate, _ => _.LastUpdate, _ => _.LastWrite, _ => _.IsNotExist)
								.ToArray();
							dbaction.Commit();
						}
					}
					sw.Stop();
					Logger.AddDebugLog(" - CollectFiles phase3({0}items update, ellapsed {1}ms", updateList.Count, sw.ElapsedMilliseconds);

					// scan finish
					if (scan_finished != null)
						scan_finished();

					// phase.4: append check...
					// MEMO: FileSystemWatcherがあまりに挙動不審なので、最後の通知から2秒後にフルスキャンを行う
					while (ct.IsCancellationRequested == false)
					{
						// 1秒のタイムアウト付きで変更を監視
						var result = fsw.WaitForChanged(WatcherChangeTypes.All, 1000);
						if (result.TimedOut) continue;

						// 最後の通知から2秒経過するまで待ってみる(2秒のタイムアウトが発生するまで待つ)
						while (fsw.WaitForChanged(WatcherChangeTypes.All, 2000).TimedOut == false) ;
						Logger.AddDebugLog("FSW-Notify, RaiseRescan.");
						await Task.Run(async () => await CollectFiles(path));
						break;
					}
				}
				Logger.AddDebugLog(" - CollectFiles finished.");
			});
		}

		#endregion
	}

	static class MediaDBViewManagerExtensions
	{
		public static IObservable<string> ItemsCollectingAsObservable(this MediaDBViewManager manager)
		{
			return Observable.FromEvent<string>(v => manager.ItemsCollecting += v, v => manager.ItemsCollecting -= v);
		}
		public static IObservable<Unit> ItemCollect_CoreScanFinishedAsObservable(this MediaDBViewManager manager)
		{
			return Observable.FromEvent(v => manager.ItemCollect_CoreScanFinished += v, v => manager.ItemCollect_CoreScanFinished -= v);
		}
		public static IObservable<Unit> ItemCollect_ScanFinishedAsObservable(this MediaDBViewManager manager)
		{
			return Observable.FromEvent(v => manager.ItemCollect_ScanFinished += v, v => manager.ItemCollect_ScanFinished -= v);
		}
		public static IObservable<Unit> ItemsLoadedAsObservable(this MediaDBViewManager manager)
		{
			return Observable.FromEvent(v => manager.ItemsLoaded += v, v => manager.ItemsLoaded -= v);
		}

	}
}
