using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quala;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace SmartAudioPlayerFx.Preferences
{
	// 将来的にはjsonに切り替えるため、Windows依存でok
	[SingletonService]
	public sealed class JsonPreferencesManager
	{
		const string DATADIRNAME = "data";
		public string BaseDir { get; set; }

		const string PLAYER_FILENAME = "player.json";
		public ReactiveProperty<JObject> PlayerSettings { get; } = new ReactiveProperty<JObject>(new JObject());

		const string WINDOW_FILENAME = "window.json";
		public ReactiveProperty<JObject> WindowSettings { get; } = new ReactiveProperty<JObject>(new JObject());

		const string UPDATE_FILENAME = "update_settings.json";
		public ReactiveProperty<JObject> UpdateSettings { get; } = new ReactiveProperty<JObject>(new JObject());

		// Save()によりXElementを設定する必要があることを通知します
		public event Action SerializeRequest;

		public JsonPreferencesManager(StorageManager storage)
		{
			// BaseDir
			BaseDir = storage.AppDataDirectory.PathName;

			// Load
			Load();
		}

		public void Load()
		{
			PlayerSettings.Value = LoadCore(DATADIRNAME, PLAYER_FILENAME);
			WindowSettings.Value = LoadCore(DATADIRNAME, WINDOW_FILENAME);
			UpdateSettings.Value = LoadCore(DATADIRNAME, UPDATE_FILENAME);
		}

		public void Save()
		{
			if (SerializeRequest == null) return;

			SerializeRequest();
			SaveCore(PlayerSettings.Value, DATADIRNAME, PLAYER_FILENAME);
			SaveCore(WindowSettings.Value, DATADIRNAME, WINDOW_FILENAME);
			SaveCore(UpdateSettings.Value, DATADIRNAME, UPDATE_FILENAME);
		}

		void SaveCore(JObject element, params string[] filepaths)
		{
			var path = Path.Combine(BaseDir ?? string.Empty, Path.Combine(filepaths));
			Directory.CreateDirectory(Path.GetDirectoryName(path));

			var json_string = FormatOutput(element.ToString());
			File.WriteAllText(path, json_string);
		}

		JObject LoadCore(params string[] filepaths)
		{
			var path = Path.Combine(BaseDir ?? string.Empty, Path.Combine(filepaths));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			if (File.Exists(path) == false)
				return new JObject();

			var json_string = File.ReadAllText(path);
			return JObject.Parse(json_string);
		}

		//= Json
		/// <summary>
		/// Adds indentation and line breaks to output of JavaScriptSerializer
		/// http://stackoverflow.com/questions/5881204/how-to-set-formatting-with-javascriptserializer-when-json-serializing
		/// </summary>
		internal static string FormatOutput(string jsonString)
		{
			bool escaped = false;
			bool inquotes = false;
			int column = 0;
			int indentation = 0;
			Stack<int> indentations = new Stack<int>();
			int TABBING = 4;
			StringBuilder sb = new StringBuilder();
			foreach (char x in jsonString)
			{
				sb.Append(x);
				column++;
				if (escaped)
				{
					escaped = false;
				}
				else
				{
					if (x == '\\')
					{
						escaped = true;
					}
					else if (x == '\"')
					{
						inquotes = !inquotes;
					}
					else if (!inquotes)
					{
						if (x == ',')
						{
							// if we see a comma, go to next line, and indent to the same depth
							sb.Append("\r\n");
							column = 0;
							for (int i = 0; i < indentation; i++)
							{
								sb.Append(" ");
								column++;
							}
						}
						else if (x == '[' || x == '{')
						{
							// if we open a bracket or brace, indent further (push on stack)
							indentations.Push(indentation);
							indentation = column;
						}
						else if (x == ']' || x == '}')
						{
							// if we close a bracket or brace, undo one level of indent (pop)
							indentation = indentations.Pop();
						}
						else if (x == ':')
						{
							// if we see a colon, add spaces until we get to the next
							// tab stop, but without using tab characters!
							while ((column % TABBING) != 0)
							{
								sb.Append(' ');
								column++;
							}
						}
					}
				}
			}
			return sb.ToString();
		}

	}

	public static class JsonPreferenceManagerExtensions
	{
		public static IObservable<Unit> SerializeRequestAsObservable(this JsonPreferencesManager manager)
			=> Observable.FromEvent(v => manager.SerializeRequest += v, v => manager.SerializeRequest -= v);
	}

}
