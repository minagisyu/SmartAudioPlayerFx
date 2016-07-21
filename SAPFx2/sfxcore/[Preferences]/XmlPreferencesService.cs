using Quala.Extensions;
using Reactive.Bindings;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace SmartAudioPlayerFx
{
	/// <summary>
	/// SmartAudioPlayer Fx 3.3以前の設定ファイル(XMLタイプ)を扱う
	/// </summary>
	public sealed class XmlPreferencesService : IDisposable
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

		public ReactiveProperty<XElement> PlayerSettings { get; } = new ReactiveProperty<XElement>(new XElement(PLAYER_ELEMENTNAME));
		public ReactiveProperty<XElement> WindowSettings { get; } = new ReactiveProperty<XElement>(new XElement(WINDOW_ELEMENTNAME));
		public ReactiveProperty<XElement> UpdateSettings { get; } = new ReactiveProperty<XElement>(new XElement(UPDATE_ELEMENTNAME));

		public XmlPreferencesService()
		{
			// シリアライザの保存パスを手動設定
			var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var appname = "SmartAudioPlayer Fx";
			serializer.BaseDir = Path.Combine(appdata, appname);

			Load();
		}
		void IDisposable.Dispose()
		{
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

	public static class XmlPreferencesServiceExtensions
	{
		public static IObservable<Unit> SerializeRequestAsObservable(this XmlPreferencesService service)
		{
			return Observable.FromEvent(
				v => service.SerializeRequest += v,
				v => service.SerializeRequest -= v);
		}
	}

}
