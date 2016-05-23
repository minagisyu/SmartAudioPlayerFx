using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Quala
{
	partial class Preference
	{
		public class Entry
		{
			//= Entry
			readonly string _jsonFilePath;

			public Entry(string jsonFilePath)
			{
				_jsonFilePath = jsonFilePath;
			}

			//= KeyValue
			readonly ConcurrentDictionary<string, object> _keyValue = new ConcurrentDictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

			public Entry Clear()
			{
				lock (_keyValue) { _keyValue.Clear(); }
				return this;
			}

			public T GetValue<T>(string key, T defaultValue = default(T))
			{
				object value;
				bool result;
				lock (_keyValue) { result = _keyValue.TryGetValue(key, out value); }
				return (result && value is T) ? (T)value : defaultValue;
			}

			public Entry SetValue(string key, object value)
			{
				lock (_keyValue) { _keyValue[key] = value; }
				return this;
			}

			//= Save/Load
			public event Action Loaded;
			public event Action Saving;

			public void Load()
			{
				if (File.Exists(_jsonFilePath) == false) { return; }

				var json = File.ReadAllText(_jsonFilePath);
				var dic = JsonConvert.DeserializeObject<IDictionary<string, object>>(json);

				lock (_keyValue)
				{
					_keyValue.Clear();
					foreach (var d in dic) { _keyValue[d.Key] = d.Value; }
				}

				Loaded?.Invoke();
			}

			public void Save()
			{
				Saving?.Invoke();

				lock (_keyValue)
				{
					var json = JsonConvert.SerializeObject(_keyValue as IDictionary<string, object>);
					json = FormatOutput(json);
					File.WriteAllText(_jsonFilePath, json);
				}
			}
		}
	}
}
