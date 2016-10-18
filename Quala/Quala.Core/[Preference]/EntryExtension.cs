using Newtonsoft.Json;
using System;

namespace Quala
{
	public static class EntryExtension
	{
		public static Preference.Entry GetValueFromJson<T>(this Preference.Entry entry, string key, Action<T> valueAction)
			=> entry.GetValue(key, v => valueAction?.Invoke(JsonConvert.DeserializeObject<T>(v as string)));

		public static Preference.Entry SetValueToJson(this Preference.Entry entry, string key, object value)
			=> entry.SetValue(key, Preference.FormatOutput(JsonConvert.SerializeObject(value)));
	}
}
