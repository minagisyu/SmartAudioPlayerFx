using System;
using System.ComponentModel;

namespace Quala
{
	public static class EntryExtension
	{
		public static Preference.Entry GetValue<T>(this Preference.Entry entry, string key, Action<T> valueAction, T defaultValue = default(T))
		{
			valueAction?.Invoke((entry != null) ? entry.GetValue<T>(key, defaultValue) : defaultValue);
			return entry;
		}

		public static Preference.Entry GetConvertedValue<T>(this Preference.Entry entry, string key, Action<T> valueAction, T defaultValue = default(T))
		{
			string obj = null;
			entry?.GetValue<string>(key, o => obj = o);

			var conv = TypeDescriptor.GetConverter(typeof(T));
			valueAction?.Invoke((obj != null && conv.CanConvertFrom(typeof(string))) ? (T)conv.ConvertFromString(obj) : defaultValue);

			return entry;
		}

		public static Preference.Entry SetConvertedValue<T>(this Preference.Entry entry, string key, T value)
		{
			var conv = TypeDescriptor.GetConverter(typeof(T));
			return entry?.SetValue(key, (value != null && conv.CanConvertTo(typeof(string))) ? conv.ConvertToString(value) : null);
		}
	}
}
