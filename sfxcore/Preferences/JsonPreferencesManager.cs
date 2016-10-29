using Quala;
using System;

namespace SmartAudioPlayerFx.Managers
{
	public sealed class JsonPreferencesManager
		: IDisposable
	{
		const string DATADIRNAME = "data";
		const string PLAYER_KEY = "Player";
		const string WINDOW_KEY = "Window";
		const string UPDATE_KEY = "UpdateSettings";

		/// <summary>
		/// Save()によりXElementを設定する必要があることを通知します
		/// </summary>
		public event Action SerializeRequest;

		public Preference.Entry PlayerSettings { get { return AppService.Preference[PLAYER_KEY]; } }
		public Preference.Entry WindowSettings { get { return AppService.Preference[WINDOW_KEY]; } }
		public Preference.Entry UpdateSettings { get { return AppService.Preference[UPDATE_KEY]; } }

		public JsonPreferencesManager(bool isLoad = false)
		{
			var storage = AppService.Storage.AppDataRoaming;
			AppService.Preference
				.ClearEntry()
				.AddEntry(PLAYER_KEY, storage.CreateDirectoryPath(DATADIRNAME, "player.json"))
				.AddEntry(WINDOW_KEY, storage.CreateDirectoryPath(DATADIRNAME, "window.json"))
				.AddEntry(UPDATE_KEY, storage.CreateDirectoryPath(DATADIRNAME, "update_settings.json"));

			if (isLoad)
				Load();
		}

		public void Dispose()
		{
			SerializeRequest = null;
			AppService.Preference.ClearEntry();
		}

		public void Load()
		{
			AppService.Preference.LoadAll();
		}

		public void Save()
		{
			SerializeRequest?.Invoke();
			AppService.Preference.SaveAll();
		}

	}
}
