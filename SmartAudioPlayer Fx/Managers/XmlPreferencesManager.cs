using Quala.Extensions;
using Reactive.Bindings;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace SmartAudioPlayerFx.Managers
{
	public sealed class XmlPreferencesManager
		: IDisposable
	{
		const string DATADIRNAME = "data";
		const string PLAYER_ELEMENTNAME = "Player";
		const string PLAYER_FILENAME = "player.xml";
		const string WINDOW_ELEMENTNAME = "Window";
		const string WINDOW_FILENAME = "window.xml";
		const string UPDATE_ELEMENTNAME = "UpdateSettings";
		const string UPDATE_FILENAME = "update_settings.xml";
		readonly static XmlPreferenceSerializer serializer = new XmlPreferenceSerializer();

		/// <summary>
		/// Save()によりXElementを設定する必要があることを通知します
		/// </summary>
		public event Action SerializeRequest;

		public ReactiveProperty<XElement> PlayerSettings { get; private set; }
		public ReactiveProperty<XElement> WindowSettings { get; private set; }
		public ReactiveProperty<XElement> UpdateSettings { get; private set; }

		public XmlPreferencesManager(bool isLoad = false)
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
			PlayerSettings = null;
			WindowSettings = null;
			UpdateSettings = null;
		}

		public void Load()
		{
			PlayerSettings.Value = serializer.Load(PLAYER_ELEMENTNAME, DATADIRNAME, PLAYER_FILENAME);
			WindowSettings.Value = serializer.Load(WINDOW_ELEMENTNAME, DATADIRNAME, WINDOW_FILENAME);
			UpdateSettings.Value = serializer.Load(UPDATE_ELEMENTNAME, DATADIRNAME, UPDATE_FILENAME);
		}

		public void Save()
		{
			if (SerializeRequest == null) return;

			SerializeRequest();
			serializer.Save(PlayerSettings.Value, DATADIRNAME, PLAYER_FILENAME);
			serializer.Save(WindowSettings.Value, DATADIRNAME, WINDOW_FILENAME);
			serializer.Save(UpdateSettings.Value, DATADIRNAME, UPDATE_FILENAME);
		}

	}

	public static class PreferencesManagerExtensions
	{
		public static IObservable<Unit> SerializeRequestAsObservable(this XmlPreferencesManager manager)
		{
			return Observable.FromEvent(
				v => manager.SerializeRequest += v,
				v => manager.SerializeRequest -= v);
		}
	}

}
