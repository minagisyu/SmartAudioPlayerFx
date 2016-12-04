using Newtonsoft.Json;
using System;

namespace Quala
{
	public static class EntryExtension
	{
		public static PreferenceManager.Entry GetValueFromJson<T>(this PreferenceManager.Entry entry, string key, Action<T> valueAction)
			=> entry.GetValue(key, v => valueAction?.Invoke(JsonConvert.DeserializeObject<T>(v as string)));

		public static PreferenceManager.Entry SetValueToJson(this PreferenceManager.Entry entry, string key, object value)
			=> entry.SetValue(key, PreferenceManager.FormatOutput(JsonConvert.SerializeObject(value)));
	}
}
