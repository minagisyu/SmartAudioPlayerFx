using System;
using System.Xml.Linq;

namespace Quala
{
	partial class Extension
	{
		public static T SafeParseAttribute<T>(this XElement element, string attributeName)
		{
			return SafeParseAttribute(element, attributeName, default(T));
		}

		public static T SafeParseAttribute<T>(this XElement element, string attributeName, T defaultValue)
		{
			if(element == null) return defaultValue;
			return element.Attribute(attributeName).SafeParse(defaultValue);
		}

		public static void SafeParseAttribute<T>(this XElement element, string attributeName, Action<T> action)
		{
			if(element != null)
			{
				element.Attribute(attributeName).SafeParse(action);
			}
		}

		public static XElement SafeAddAttribute<T>(this XElement element, XName name, T value)
		{
			if(element == null) return element;
			if(value == null) return element;
			if(value is string && string.IsNullOrEmpty(value as string)) return element;

			element.Add(new XAttribute(name, value));
			return element;
		}
	}
}
