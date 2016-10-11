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
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Quala;
using Quala.Extensions;
using SmartAudioPlayerFx.Preferences;

namespace SmartAudioPlayerFx.MediaDB
{
//	[Require(typeof(MediaDBManager))]
//	[Require(typeof(XmlPreferencesManager))]
//	[Require(typeof(MediaItemFilterManager))]
	sealed class MediaDBViewManager : IDisposable
	{
		#region ctor

		public ReactiveProperty<string> FocusPath { get; private set; }
		public VersionedCollection<MediaItem> Items { get; private set; }
		readonly CompositeDisposable _disposables;

		public MediaDBViewManager()
		{
			FocusPath = new ReactiveProperty<string>(mode: ReactivePropertyMode.RaiseLatestValueOnSubscribe);
			Items = new VersionedCollection<MediaItem>(
				new CustomEqualityComparer<MediaItem>(x => x.ID.GetHashCode(), (x, y) => x.ID == y.ID));
			_disposables = new CompositeDisposable(FocusPath);

			// Preferences
			App.Models.Get<XmlPreferencesManager>().PlayerSettings
				.Subscribe(x => LoadPreferences(x))
				.AddTo(_disposables);
			App.Models.Get<XmlPreferencesManager>().SerializeRequestAsObservable()
				.Subscribe(_ => SavePreferences(App.Models.Get<XmlPreferencesManager>().PlayerSettings.Value))
				.AddTo(_disposables);

			// MediaItemFilter
			// 最後の通知から500ms後にItemsの再検証
			ManagerServices.MediaItemFilterManager.PropertyChangedAsObservable()
				.Throttle(TimeSpan.FromMilliseconds(500))
				.Subscribe(_ => RevalidateItems(FocusPath.Value))
				.AddTo(_disposables);

			// FocusPathが変更されたらItemsを再設定
			FocusPath
				.Subscribe(x => LoadDBItems(x))
				.AddTo(_disposables);
		}
		public void Dispose()
		{
			StopAllAsyncProcess();
			_disposables.Dispose();

			ItemsCollecting = null;
			ItemCollect_CoreScanFinished = null;
			ItemCollect_ScanFinished = null;
			ItemsLoaded = null;
		}
		public void StopAllAsyncProcess()
		{
			LoadDBItems(null);
			RevalidateItems(null);
			CollectFiles(null).Wait();
			RaiseDBUpdateAsync(null);
			RaiseDBInsertAsync(null);
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
			App.Models.Get<Logging>().AddDebugLog("Call GetOrCreate: filepath={0}", filepath);
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
				RaiseDBInsertAsync(item);
			}
			else if (exists == false)
			{
				// ファイルが消されたっぽいのでIsNotExist更新通知
				item.IsNotExist = true;
				item.LastUpdate = DateTime.UtcNow.Ticks;
				RaiseDBUpdateAsync(item, _ => _.IsNotExist, _ => _.LastUpdate);
			}
			return item;
		}

		#region WaitForAsyncRaising / RaiseDBUpdate / RaiseDBInsert

		public void WaitForAsyncRaiging()
		{
			RaiseDBInsertAsync(null);
			RaiseDBUpdateAsync(null);
		}

		Task _RaiseDBUpdate_Task;

		/// <summary>
		/// itemの指定プロパティの情報をDBへ書き込みます
		/// </summary>
		/// <param name="item"></param>
		/// <param name="columns"></param>
		public Task RaiseDBUpdateAsync(MediaItem item, params Expression<Func<MediaItem, object>>[] columns)
		{
			// 以前の非同期処理の終了を待つ
			if (_RaiseDBUpdate_Task != null)
			{
				_RaiseDBUpdate_Task.Wait();
				_RaiseDBUpdate_Task = null;
			}

			if (item == null) return null;
			if (item.ID == 0) return null;
			return _RaiseDBUpdate_Task = Task.Run(()=>
			{
				ManagerServices.MediaDBManager.UseTransaction(dbaction =>
				{
					dbaction.Update(new[] { item }, columns)
						.AsParallel()
						.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
						.ForAll(x => Items.AddOrReplace(x));
				});
			});
		}

		Task _RaiseDBInsert_Task;
		public Task RaiseDBInsertAsync(MediaItem item)
		{
			// 以前の非同期処理の終了を待つ
			if (_RaiseDBInsert_Task != null)
			{
				_RaiseDBInsert_Task.Wait();
				_RaiseDBInsert_Task = null;
			}

			if (item == null) return null;
			return _RaiseDBInsert_Task = Task.Run(() =>
			{
				ManagerServices.MediaDBManager.UseTransaction(dbaction =>
				{
					dbaction.Insert(new[] { item })
						.AsParallel()
						.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
						.ForAll(x => Items.AddOrReplace(x));
				});
			});
		}

		#endregion
		#region LoadDBItems

		CancellationTokenSource _LoadDBItems_CTS;
		Task _LoadDBItems_Task;

		void LoadDBItems(string path)
		{
			App.Models.Get<Logging>().AddDebugLog("Call LoadDBItems: path={0}", path);

			// 以前の非同期処理をキャンセルして終了を待つ
			if (_LoadDBItems_CTS != null)
				_LoadDBItems_CTS.Cancel();
			if (_LoadDBItems_Task != null)
			{
				_LoadDBItems_Task.Wait();
				_LoadDBItems_Task = null;
			}

			// 初期化
			_LoadDBItems_CTS = new CancellationTokenSource();
			Items.Clear();
			MediaItemExtension.ClearAllCache();

			// 読み込み
			var ct = _LoadDBItems_CTS.Token;
			var itemsLoaded_ev = ItemsLoaded;
			_LoadDBItems_Task = Task.Run(() =>
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
				App.Models.Get<Logging>().AddDebugLog(" **LoadDBItems({0}items): {1}ms", Items.Count, sw.ElapsedMilliseconds);
			})
			.ContinueWith(_ =>
			{
				CollectFiles(path);

				if (ct.IsCancellationRequested)
					CollectFiles(null).Wait();
				if (itemsLoaded_ev != null)
					itemsLoaded_ev();
				App.Models.Get<Logging>().AddDebugLog(" **LoadDBItems(full-complete)");
			});
		}

		#endregion
		#region RevalidateItems

		CancellationTokenSource _revalidateItems_CTS;
		Task _revalidateItems_Task;
		void RevalidateItems(string path)
		{
			App.Models.Get<Logging>().AddDebugLog("Call RevalidateItems: path={0}", path);

			// 以前の非同期操作をキャンセル
			if (_revalidateItems_CTS != null)
				_revalidateItems_CTS.Cancel();
			if (_revalidateItems_Task != null)
			{
				_revalidateItems_Task.Wait();
				_revalidateItems_Task = null;
			}

			_revalidateItems_CTS = new CancellationTokenSource();
			var itemsLoaded_ev = ItemsLoaded;
			var ct = _revalidateItems_CTS.Token;
			_revalidateItems_Task = Task.Run(() =>
			{
				if (string.IsNullOrWhiteSpace(path)) return;

				App.Models.Get<Logging>().AddDebugLog(" ..RevalidateTask: items version:{0}", Items.Version);
				var sw = Stopwatch.StartNew();

				// phase remove: 既存の項目に対して、Validate()が通らないものを削除
				App.Models.Get<Logging>().AddDebugLog(" ..RevalidateTask-phase1: version:{0}", Items.Version);
				Items.GetLatest()
					.TakeWhile(_ => ct.IsCancellationRequested == false)
					.AsParallel()
					.Where(x => ManagerServices.MediaItemFilterManager.Validate(x) == false)
					.ForAll(x => Items.Remove(x));

				// phase db_add: DBを再読み込みして追加or更新
				App.Models.Get<Logging>().AddDebugLog(" ..RevalidateTask-phase2: version:{0}", Items.Version);
				ManagerServices.MediaDBManager.GetFromFilePath_ExistsOnly(path)
					.TakeWhile(_ => ct.IsCancellationRequested == false)
					.AsParallel()
					.Where(x => ManagerServices.MediaItemFilterManager.Validate(x))
					.ForAll(x => Items.AddOrReplace(x));

				sw.Stop();
				App.Models.Get<Logging>().AddDebugLog(" **RevalidateItems: {0}ms", sw.ElapsedMilliseconds);
			})
			.ContinueWith(_ =>
			{
				// phase file_add: ファイルシステムを再検索して追加として通知する
				CollectFiles(path);

				if (ct.IsCancellationRequested)
					CollectFiles(null).Wait();
				if (itemsLoaded_ev != null)
					itemsLoaded_ev();
				App.Models.Get<Logging>().AddDebugLog(" **RevalidateItems(full-complete)");
			});
		}

		#endregion
		#region CollectFiles

		CancellationTokenSource _collectFiles_CTS;
		Task _collectFiles_Task;
		Task CollectFiles(string path)
		{
			App.Models.Get<Logging>().AddDebugLog("Call CollectFiles: path={0}", path);

			// 以前の検索をキャンセル
			if (_collectFiles_CTS != null)
				_collectFiles_CTS.Cancel();
			if (_collectFiles_Task != null)
			{
				_collectFiles_Task.Wait();
				_collectFiles_Task = null;
			}

			_collectFiles_CTS = new CancellationTokenSource();
			var ct = _collectFiles_CTS.Token;
			var notify = ItemsCollecting;
			var corescan_finished = ItemCollect_CoreScanFinished;
			var scan_finished = ItemCollect_ScanFinished;
			_collectFiles_Task = Task.Run(() =>
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
				App.Models.Get<Logging>().AddDebugLog(" - CollectItems start.");

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
					foreach (var x in new DirectoryInfo(path).EnumerateAllFiles())
					{
						if (notify != null)
							notify("[1] " + x.DirectoryName);

						ManagerServices.MediaDBManager.UseTransaction(dbaction =>
						{
							// MEMO:
							// LastWrite = MinValueはphase3で更新する際に識別するためのフラグとして使用する
							var xs = x.Files
								.Select(y => new MediaItem(y) { LastWrite = DateTime.MinValue.Ticks })
								.Where(y => ManagerServices.MediaItemFilterManager.Validate(y));
							foreach (var y in dbaction.Insert(xs))
							{
								add_count++;
								Items.AddOrReplace(y);
							}
						});
					}
					sw.Stop();
					App.Models.Get<Logging>().AddDebugLog(" - CollectFiles phase1({0}items add, elapsed {1}ms)", add_count, sw.ElapsedMilliseconds);
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
						ManagerServices.MediaDBManager.UseTransaction(dbaction =>
						{
							// MEMO:
							// .ToArray()で引っ張ってならないとupdateListの中身が処理されない...
							dbaction.Update(updateList, _ => _.LastUpdate, _ => _.IsNotExist)
								.ToArray();
						});
					}
					sw.Stop();
					App.Models.Get<Logging>().AddDebugLog(" - CollectFiles phase2({0}items update, elapsed {1}ms)", updateList.Count, sw.ElapsedMilliseconds);

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
								var tag = MediaTagUtil.Get(new FileInfo(x.FilePath));
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
						ManagerServices.MediaDBManager.UseTransaction(dbaction =>
						{
							dbaction.Update(updateList,
								_ => _.FilePath,
								_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
								_ => _.CreatedDate, _ => _.LastUpdate, _ => _.LastWrite, _ => _.IsNotExist)
							.ToArray();
						});
					}
					sw.Stop();
					App.Models.Get<Logging>().AddDebugLog(" - CollectFiles phase3({0}items update, ellapsed {1}ms", updateList.Count, sw.ElapsedMilliseconds);

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
						App.Models.Get<Logging>().AddDebugLog("FSW-Notify, RaiseRescan.");
						Task.Run(() => CollectFiles(path));
						break;
					}
				}
				App.Models.Get<Logging>().AddDebugLog(" - CollectFiles finished.");
			});

			return _collectFiles_Task;
		}

		#endregion
	}

	static class MediaDBViewManagerExtensions
	{
		public static IObservable<string> ItemsCollectingAsObservable(this MediaDBViewManager manager)
		{
			return Observable.FromEvent<string>(
				v => manager.ItemsCollecting += v,
				v => manager.ItemsCollecting -= v);
		}
		public static IObservable<Unit> ItemCollect_CoreScanFinishedAsObservable(this MediaDBViewManager manager)
		{
			return Observable.FromEvent(
				v => manager.ItemCollect_CoreScanFinished += v,
				v => manager.ItemCollect_CoreScanFinished -= v);
		}
		public static IObservable<Unit> ItemCollect_ScanFinishedAsObservable(this MediaDBViewManager manager)
		{
			return Observable.FromEvent(
				v => manager.ItemCollect_ScanFinished += v,
				v => manager.ItemCollect_ScanFinished -= v);
		}
		public static IObservable<Unit> ItemsLoadedAsObservable(this MediaDBViewManager manager)
		{
			return Observable.FromEvent(
				v => manager.ItemsLoaded += v,
				v => manager.ItemsLoaded -= v);
		}

	}
}
