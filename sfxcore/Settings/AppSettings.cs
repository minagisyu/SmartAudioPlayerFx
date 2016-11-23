using Newtonsoft.Json.Linq;
using Quala;
using System;
using System.IO;

namespace SmartAudioPlayerFx.Settings
{
	public sealed class AppSettings : IDisposable
	{
		readonly string DATAFILENAME = Path.Combine("data", "settings.json");

		public event Action SerializeRequest;


		public AppSettings()
		{
		}

		public void Dispose()
		{
			SerializeRequest = null;
		}

		public void Load()
		{
		}

		public void Save()
		{
			SerializeRequest?.Invoke();
		}


	}

	// Tを保持してJObjectと相互変換するクラス
	public class JsonSerializedObject<T>
	{
		JObject json;

		public void Load()
		{
		}

		public T GetInstance()
		{
			return default(T);
		}

		public void ToJson()
		{
		}

	}
}
