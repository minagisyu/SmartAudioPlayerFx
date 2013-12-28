using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Xml.Linq;
using WinAPIs;
using Codeplex.Reactive.Extensions;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayer;

namespace SmartAudioPlayerFx.Managers
{
	[Require(typeof(PreferencesManager))]
	[Require(typeof(MediaDBViewManager))]
	sealed class RecentsManager : IDisposable
	{
		#region ctor

		const int RECENTS_PLAY_ITEMS_NUM = 100;
		const int RECENTS_OPENED_FOLDER_NUM = 20;
		readonly List<string> _recents_opened_folder = new List<string>();
		readonly List<string> _recents_play_items = new List<string>();
		readonly CompositeDisposable _disposables = new CompositeDisposable();

		public RecentsManager()
		{
			ManagerServices.PreferencesManager.PlayerSettings
				.Subscribe(x => LoadPreferences(x))
				.AddTo(_disposables);
			ManagerServices.PreferencesManager.SerializeRequestAsObservable()
				.Subscribe(_ => SavePreferences(ManagerServices.PreferencesManager.PlayerSettings.Value))
				.AddTo(_disposables);
			LoadRecentsPlayItemsFromDB();
			ManagerServices.MediaDBViewManager.FocusPath
				.Subscribe(x => AddRecentsOpenedFolder(x))
				.AddTo(_disposables);
		}
		public void Dispose()
		{
			_disposables.Dispose();
		}

		void LoadRecentsPlayItemsFromDB()
		{
			var items = ManagerServices.MediaDBManager.RecentPlayItemsPath(100)
				.Distinct(StringComparer.CurrentCultureIgnoreCase)
				.ToArray();
			lock (_recents_play_items)
			{
				_recents_play_items.AddRange(items);
			}
		}

		void LoadPreferences(XElement element)
		{
			ClearFolderRecents();
			element
				.GetArrayValues("FolderRecents", el => el.GetAttributeValueEx("Value", string.Empty))
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Reverse()
				.ForEach(x => AddRecentsOpenedFolder(x));
		}
		void SavePreferences(XElement element)
		{
			element
				.SubElement("FolderRecents", true,
					elm =>
					{
						elm.RemoveAll();
						GetRecentsOpenedFolder()
							.ToList()
							.ForEach(x =>
							{
								elm.GetOrCreateElement("Item",
									m => m.Attributes("Value")
										.Any(n => string.Equals(n.Value, x, StringComparison.CurrentCultureIgnoreCase))
									)
									.SetAttributeValueEx("Value", x);
							});
					});
		}

		#endregion

		void AddRecentsOpenedFolder(string path)
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				lock (_recents_opened_folder)
				{
					_recents_opened_folder.RemoveAll(i => i.Equals(path, StringComparison.CurrentCultureIgnoreCase));
					_recents_opened_folder.Insert(0, path);
					int overflow = _recents_opened_folder.Count - 21;
					if (overflow > 0)
					{
						_recents_opened_folder.RemoveRange(20, overflow);
					}
				}
			}
		}

		public void AddRecentsPlayItem(MediaItem item)
		{
			lock (_recents_play_items)
			{
				string current = item.FilePath;
				string top = _recents_play_items.FirstOrDefault<string>();
				if ((top == null) || !current.Equals(top, StringComparison.CurrentCultureIgnoreCase))
				{
					_recents_play_items.RemoveAll(i => i.Equals(current, StringComparison.CurrentCultureIgnoreCase));
					_recents_play_items.Insert(0, current);
					int overflow = _recents_play_items.Count - 101;
					if (overflow > 0)
					{
						_recents_play_items.RemoveRange(100, overflow);
					}
				}
			}
		}

		void ClearFolderRecents()
		{
			lock (_recents_opened_folder)
			{
				_recents_opened_folder.Clear();
			}
		}
		public string[] GetRecentsOpenedFolder()
		{
			lock (_recents_opened_folder)
			{
				return _recents_opened_folder.ToArray();
			}
		}
		public string[] GetRecentsPlayItems(int limit)
		{
			lock (_recents_play_items)
			{
				return _recents_play_items.Take(limit).ToArray();
			}
		}
	}
}
