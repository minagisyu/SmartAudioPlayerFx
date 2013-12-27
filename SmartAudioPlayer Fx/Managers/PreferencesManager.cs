using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using __Primitives__;
using Codeplex.Reactive;
using SmartAudioPlayer;

namespace SmartAudioPlayerFx.Managers
{
	[Standalone]
	sealed class PreferencesManager : IDisposable
	{
		const string DATADIRNAME = "data";
		const string PLAYER_ELEMENTNAME = "Player";
		const string PLAYER_FILENAME = "player.xml";
		const string WINDOW_ELEMENTNAME = "Window";
		const string WINDOW_FILENAME = "window.xml";
		const string UPDATE_ELEMENTNAME = "UpdateSettings";
		const string UPDATE_FILENAME = "update_settings.xml";
		readonly static PreferenceSerializer _serializer = new PreferenceSerializer(true);

		public ReactiveProperty<XElement> PlayerSettings { get; private set; }
		public ReactiveProperty<XElement> WindowSettings { get; private set; }
		public ReactiveProperty<XElement> UpdateSettings { get; private set; }

		/// <summary>
		/// Save()によりXElementを設定する必要があることを通知します
		/// </summary>
		public event Action SerializeRequest;

		public PreferencesManager(bool isLoad = false)
		{
			PlayerSettings = new ReactiveProperty<XElement>(new XElement(PLAYER_ELEMENTNAME));
			WindowSettings = new ReactiveProperty<XElement>(new XElement(WINDOW_ELEMENTNAME));
			UpdateSettings = new ReactiveProperty<XElement>(new XElement(UPDATE_ELEMENTNAME));

			if (isLoad)
				Load();
		}
		public void Dispose()
		{
			SerializeRequest = null;
			PlayerSettings.Dispose();
			WindowSettings.Dispose();
			UpdateSettings.Dispose();
		}

		public void Load()
		{
			PlayerSettings.Value = _serializer.Load(PLAYER_ELEMENTNAME, DATADIRNAME, PLAYER_FILENAME);
			WindowSettings.Value = _serializer.Load(WINDOW_ELEMENTNAME, DATADIRNAME, WINDOW_FILENAME);
			UpdateSettings.Value = _serializer.Load(UPDATE_ELEMENTNAME, DATADIRNAME, UPDATE_FILENAME);
		}
		public void Save()
		{
			if (SerializeRequest == null) return;

			SerializeRequest();
			_serializer.Save(PlayerSettings.Value, DATADIRNAME, PLAYER_FILENAME);
			_serializer.Save(WindowSettings.Value, DATADIRNAME, WINDOW_FILENAME);
			_serializer.Save(UpdateSettings.Value, DATADIRNAME, UPDATE_FILENAME);
		}

		public static string CreateFullPath(params string[] name)
		{
			return _serializer.CreateFullPath(name);
		}
	}

	static class PreferencesManagerExtensions
	{
		public static IObservable<Unit> SerializeRequestAsObservable(this PreferencesManager manager)
		{
			return Observable.FromEvent(
				v => manager.SerializeRequest += v,
				v => manager.SerializeRequest -= v,
				CurrentThreadScheduler.Instance);
		}
	}

}
