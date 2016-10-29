using Quala;
using Reactive.Bindings;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SmartAudioPlayerFx.Preferences
{
	// 将来的にはjsonに切り替えるため、Windows依存でok
	[SingletonService]
	public sealed class XmlPreferencesManager
	{
		const string DATADIRNAME = "data";
		public string BaseDir { get; set; }

		const string PLAYER_ELEMENTNAME = "Player";
		const string PLAYER_FILENAME = "player.xml";
		public ReactiveProperty<XElement> PlayerSettings { get; } = new ReactiveProperty<XElement>(new XElement(PLAYER_ELEMENTNAME));

		const string WINDOW_ELEMENTNAME = "Window";
		const string WINDOW_FILENAME = "window.xml";
		public ReactiveProperty<XElement> WindowSettings { get; } = new ReactiveProperty<XElement>(new XElement(WINDOW_ELEMENTNAME));

		const string UPDATE_ELEMENTNAME = "UpdateSettings";
		const string UPDATE_FILENAME = "update_settings.xml";
		public ReactiveProperty<XElement> UpdateSettings { get; } = new ReactiveProperty<XElement>(new XElement(UPDATE_ELEMENTNAME));

		// Save()によりXElementを設定する必要があることを通知します
		public event Action SerializeRequest;

		public XmlPreferencesManager(StorageManager storage)
		{
			// BaseDir
			BaseDir = storage.AppDataDirectory.PathName;

			// Load
			Load();
		}

		public void Load()
		{
			PlayerSettings.Value = LoadCore(PLAYER_ELEMENTNAME, DATADIRNAME, PLAYER_FILENAME);
			WindowSettings.Value = LoadCore(WINDOW_ELEMENTNAME, DATADIRNAME, WINDOW_FILENAME);
			UpdateSettings.Value = LoadCore(UPDATE_ELEMENTNAME, DATADIRNAME, UPDATE_FILENAME);
		}

		public void Save()
		{
			if (SerializeRequest == null) return;

			SerializeRequest();
			SaveCore(PlayerSettings.Value, DATADIRNAME, PLAYER_FILENAME);
			SaveCore(WindowSettings.Value, DATADIRNAME, WINDOW_FILENAME);
			SaveCore(UpdateSettings.Value, DATADIRNAME, UPDATE_FILENAME);
		}

		/// <summary>
		/// 指定ファイルへ保存。
		/// インデント有効と文字チェック無効設定。
		/// </summary>
		/// <param name="element"></param>
		/// <param name="filepaths"></param>
		void SaveCore(XElement element, params string[] filepaths)
		{
			var path = Path.Combine(BaseDir ?? string.Empty, Path.Combine(filepaths));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (var stream = File.Open(path, FileMode.Create, FileAccess.Write))
			using (var writer = XmlWriter.Create(stream, new XmlWriterSettings() { CheckCharacters = false, Indent = true, }))
			{
				// 整形してみる, 無効な文字のせいで例外出たらelementをそのまま使う
				XElement elm = null;
				try { elm = XElement.Parse(element.ToString()); }
				catch { elm = element; }
				elm.WriteTo(writer);
			}
		}

		/// <summary>
		/// 指定ファイルからロードします。
		/// ファイルが無い、読み込みエラー、ルート要素名がnameと違う場合は空のXElement(name)が返ります。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="filepaths"></param>
		/// <returns></returns>
		XElement LoadCore(XName name, params string[] filepaths)
		{
			var path = Path.Combine(BaseDir ?? string.Empty, Path.Combine(filepaths));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			if (File.Exists(path) == false)
				return new XElement(name);

			using (var stream = File.OpenRead(path))
			using (var reader = XmlReader.Create(stream, new XmlReaderSettings() { CheckCharacters = false, }))
			{
				var element = XElement.Load(reader);
				return (element != null && element.Name.Equals(name)) ? element : new XElement(name);
			}
		}
	}

	public static class PreferenceManagerExtensions
	{
		public static IObservable<Unit> SerializeRequestAsObservable(this XmlPreferencesManager manager)
			=> Observable.FromEvent(v => manager.SerializeRequest += v, v => manager.SerializeRequest -= v);
	}

}
