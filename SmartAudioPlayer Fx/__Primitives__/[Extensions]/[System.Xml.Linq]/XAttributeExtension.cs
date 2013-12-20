using System;
using System.ComponentModel;
using System.Xml.Linq;

namespace __Primitives__
{
	static class XAttributeExtension
	{
		public static T SafeParse<T>(this XAttribute element, T defaultValue)
		{
			if(element == null || string.IsNullOrEmpty(element.Value))
				return defaultValue;

			var conv = TypeDescriptor.GetConverter(typeof(T));
			if(conv.CanConvertFrom(typeof(string)) == false)
				return defaultValue;

			try { return (T)conv.ConvertFromString(element.Value); }
			catch(NotSupportedException) { }
			catch(FormatException) { }

			return defaultValue;
		}

		public static void SafeParse<T>(this XAttribute element, Action<T> action)
		{
			if(element == null || string.IsNullOrEmpty(element.Value))
				return;

			var conv = TypeDescriptor.GetConverter(typeof(T));
			if(conv.CanConvertFrom(typeof(string)) == false)
				return;

			try { action((T)conv.ConvertFromString(element.Value)); }
			catch(NotSupportedException) { }
		}
	}
}
