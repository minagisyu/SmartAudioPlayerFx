//============================================================================
// 2008.xx.xx
//  - 初期実装
//    IElement, SaveEntry, LoadEntry, Item<T>,
//    DeserializeFromFile(), DeserializeFromStream(),
//    SerializeToFile(), SerializeToStream()
//
// 2008.09.19
//  - ElementManager
//
//============================================================================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Quala.Runtime.Serialization
{
	public static class XSerializer
	{
		public interface IElement
		{
			void Serialize(XSerializer.SaveEntry entry);
			void Deserialize(XSerializer.LoadEntry entry);
		}

		public static void DeserializeFromFile(this IElement element, string name, string filepath)
		{
			var entry = new LoadEntry(name, filepath);
			element.Deserialize(entry);
		}

		public static void DeserializeFromStream(this IElement element, string name, Stream stream)
		{
			var entry = new LoadEntry(name, stream);
			element.Deserialize(entry);
		}

		public static void SerializeToFile(this IElement element, string name, string filepath)
		{
			var entry = new SaveEntry();
			element.Serialize(entry);
			entry.Save(name, filepath);
		}

		public static void SerializeToStream(this IElement element, string name, Stream stream)
		{
			var entry = new SaveEntry();
			element.Serialize(entry);
			entry.Save(name, stream);
		}

		/// <summary>
		/// シリアライズの処理エントリー
		/// </summary>
		public sealed class SaveEntry
		{
			readonly List<XAttribute> _attrs = new List<XAttribute>();
			readonly List<XElement> _elms = new List<XElement>();

			/// <summary>
			/// エントリーの内容をXElementに変換して出力。
			/// エントリーが空の場合、nullが返されます。
			/// </summary>
			/// <param name="name"></param>
			/// <returns></returns>
			public XElement ToXElement(string name)
			{
				return (_attrs.Count + _elms.Count > 0) ?
					new XElement(name, _attrs.ToArray(), _elms.ToArray()) : null;
			}

			/// <summary>
			/// エントリーの内容をXElementに追加出力。
			/// エントリーが空の場合は何も追加されません。
			/// </summary>
			/// <param name="element"></param>
			public void AddToXElement(XElement element)
			{
				if(_attrs.Count + _elms.Count > 0)
					element.Add(_attrs.ToArray(), _elms.ToArray());
			}

			/// <summary>
			/// シリアライズする値を追加。
			/// IXSerializerElement実装クラスはXMLタグの子要素(XElement)、
			/// それ以外は属性(XAttribute)として出力されます。
			/// </summary>
			/// <param name="name">属性(またはタグ)の名前</param>
			/// <param name="value">
			/// 値、string以外の場合はTypeDescriptor.GetConverter()によってstringに変換されます。
			/// 変換に失敗した場合は例外が発生します。
			/// </param>
			public void AddValue(string name, object value)
			{
				// 簡単なチェック
				if(string.IsNullOrEmpty(name))
					throw new ArgumentException("name");
				if(value == null)
					return;

				if(value is IElement)
				{
					var entry = new SaveEntry();
					((IElement)value).Serialize(entry);

					// 空タグは追加しないように。
					var elm = entry.ToXElement(name);
					if(elm != null) _elms.Add(elm);
				}
				else
				{
					// 変換してみる
					var v = value as string;
					if(v == null)
					{
						var conv = TypeDescriptor.GetConverter(value);
						if(conv.CanConvertTo(typeof(string)) == false) return;
						v = conv.ConvertToString(value);
					}
					if(string.IsNullOrEmpty(v)) return;
					_attrs.Add(new XAttribute(name, v));
				}
			}

			/// <summary>
			/// コンストラクタで指定したファイルへ書き込み
			/// </summary>
			/// <param name="name"></param>
			/// <param name="filepath"></param>
			public void Save(string name, string filepath)
			{
				if(string.IsNullOrEmpty(filepath))
					throw new InvalidOperationException("書き込み先を特定出来ません");

				File.Open(filepath, FileMode.Create, FileAccess.Write)
					.Using(stream => Save(name, stream));
			}

			/// <summary>
			/// ストリームを指定して書き込み。
			/// 呼び出される前に書き込み可能な状態で開いておき、呼び出し後にクローズしてください。
			/// </summary>
			/// <param name="name"></param>
			/// <param name="stream">書き込み先のストリーム</param>
			public void Save(string name, Stream stream)
			{
				// 保存
				var element = ToXElement(name);
				if(element == null) return;

				XmlTextWriter
					.Create(stream, new XmlWriterSettings() { CheckCharacters = false, Indent = true, })
					.Using(writer => element.Save(writer));
			}
		}


		/// <summary>
		/// デシリアライズの処理エントリー
		/// </summary>
		public sealed class LoadEntry
		{
			XElement _element;

			public LoadEntry(XElement element)
			{
				_element = element;
			}

			/// <summary>
			/// 指定したファイルから読み込み。
			/// </summary>
			public LoadEntry(string name, string filepath)
			{
				if(string.IsNullOrEmpty(filepath))
					throw new InvalidOperationException("読み込み元を特定出来ません");

				// ファイルあるときだけ読み込み。
				if(File.Exists(filepath))
				{
					File.OpenRead(filepath).Using(stream => Load(name, stream));
				}
			}

			/// <summary>
			/// ストリームを指定して読み込み。
			/// 呼び出される前に読み込み可能な状態で開いておき、呼び出し後にクローズしてください。
			/// </summary>
			/// <param name="name">ルートタグ名。null or String.Emptyでチェック無効。</param>
			/// <param name="stream">読み込み元のストリーム</param>
			public LoadEntry(string name, Stream stream)
			{
				Load(name, stream);
			}

			void Load(string name, Stream stream)
			{
				if(stream == null)
					throw new ArgumentNullException("stream");

				var element = XmlTextReader
					.Create(stream, new XmlReaderSettings() { CheckCharacters = false, })
					.Using(reader =>
					{
						try { return XElement.Load(reader); }
						catch(XmlException) { }
						return null;
					});

				// 要素がないっす
				if(element == null)
					return;

				// タグ名が違うなら何もせず。
				if(string.IsNullOrEmpty(name) == false &&
					element.Name.LocalName != name)
					return;

				// 読み込み
				_element = element;
			}

			/// <summary>
			/// デシリアライズされた値を取得。
			/// IXSerializerElement実装クラスはXMLタグの子要素、それ以外は属性から値を取得します。
			/// </summary>
			/// <typeparam name="T">デシリアライズされる型</typeparam>
			/// <param name="name">属性(またはタグ)の名前</param>
			/// <returns>
			/// 値はIXSerializerElement実装クラスならDeserialize()、
			/// それ以外はTypeDescriptor.GetConverter()によってTに変換されます。
			/// 失敗した場合はdefault(T)が返ります。
			/// </returns>
			public T GetValue<T>(string name)
			{
				return GetValue(name, default(T));
			}

			/// <summary>
			/// デシリアライズされた値を取得。
			/// IXSerializerElement実装クラスはXMLタグの子要素、それ以外は属性から値を取得します。
			/// </summary>
			/// <typeparam name="T">デシリアライズされる型</typeparam>
			/// <param name="name">属性(またはタグ)の名前</param>
			/// <param name="ret">
			/// 値はIXSerializerElement実装クラスならDeserialize()、
			/// それ以外はTypeDescriptor.GetConverter()によってTに変換されます。
			/// 失敗した場合はdefault(T)が代入されます。
			/// </param>
			public void GetValue<T>(string name, out T ret)
			{
				ret = GetValue(name, default(T));
			}

			/// <summary>
			/// デシリアライズされた値を取得。
			/// IXSerializerElement実装クラスはXMLタグの子要素、それ以外は属性から値を取得します。
			/// </summary>
			/// <typeparam name="T">デシリアライズされる型</typeparam>
			/// <param name="name">属性(またはタグ)の名前</param>
			/// <param name="defaultValue">
			/// /// IXSerializerElement実装クラス = シリアライズされるインスタンス,
			/// それ以外 = エラーが発生したときに返されるデフォルト値。
			/// </param>
			/// <param name="ret">
			/// 値はIXSerializerElement実装クラスならDeserialize()、
			/// それ以外はTypeDescriptor.GetConverter()によってTに変換されます。
			/// 失敗した場合はdefaultValueが代入されます。
			/// </param>
			public void GetValue<T>(string name, out T ret, T defaultValue)
			{
				ret = GetValue(name, defaultValue);
			}

			/// <summary>
			/// デシリアライズされた値を取得。
			/// IXSerializerElement実装クラスはXMLタグの子要素、それ以外は属性から値を取得します。
			/// </summary>
			/// <typeparam name="T">デシリアライズされる型</typeparam>
			/// <param name="name">属性(またはタグ)の名前。</param>
			/// <param name="defaultValue">
			/// IXSerializerElement実装クラス = シリアライズされるインスタンス,
			/// それ以外 = エラーが発生したときに返されるデフォルト値。
			/// </param>
			/// <returns>
			/// 値はIXSerializerElement実装クラスならdefaultValue.Deserialize()、
			/// それ以外はTypeDescriptor.GetConverter()によってTに変換されます。
			/// 失敗した場合はdefaultValueが返ります。
			/// </returns>
			/// <exception cref="ArgumentException">
			/// 「(T is IXSerializerElement) && (defaultValue == null)」…IXSerializerElementのインスタンスが必要。
			/// </exception>
			public T GetValue<T>(string name, T defaultValue)
			{
				if(string.IsNullOrEmpty(name))
					return defaultValue;

				if(typeof(IElement).IsAssignableFrom(typeof(T)))
				{
					// element
					var e = defaultValue as IElement;
					if(e == null)
						throw new ArgumentException("IXSerializerElementのインスタンスが必要です", "defaultValue");

					// 変換
					// _element.Elemenet()の返値がnullでもそのまま渡してやる
					// これは、LoadEntry処理中にデフォルト値を設定するようなプログラムを可能にするため
					e.Deserialize(new LoadEntry((_element != null) ? _element.Element(name) : null));
					return (T)e;
				}
				else
				{
					// 要素がないなら何も出来んなぁ…
					if(_element == null)
						return defaultValue;

					// 属性
					var attr = _element.Attribute(name);
					if(attr == null || string.IsNullOrEmpty(attr.Value))
						return defaultValue;

					// 変換チェック
					var conv = TypeDescriptor.GetConverter(typeof(T));
					if(conv.CanConvertFrom(typeof(string)) == false)
						return defaultValue;

					// 変換
					try { return (T)conv.ConvertFromString(attr.Value); }
					catch(NotSupportedException) { }
					catch(FormatException) { }

					// 失敗
					return defaultValue;
				}
			}

			/// <summary>
			/// デシリアライズされた値を取得。
			/// IXSerializerElement型の要素をまとめて処理するときに使用します。
			/// </summary>
			/// <typeparam name="T">
			/// デシリアライズされる型、
			/// IXSerializerElement実装クラスであり、new()出来る必要があります。
			/// </typeparam>
			/// <param name="name">属性(またはタグ)の名前</param>
			/// <returns>Tの配列</returns>
			public IEnumerable<T> GetValues<T>(string name)
				where T : IElement, new()
			{
				if(_element == null)
					yield break;

				if(string.IsNullOrEmpty(name))
					throw new ArgumentException("name");

				foreach(var elm in _element.Elements(name))
				{
					var e = new T();
					e.Deserialize(new LoadEntry(elm));
					yield return e;
				}
			}
		}

		/// <summary>
		/// IElementを実装できない単一型用のラッパー
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public struct Item<T> : IElement
		{
			public T Value;
			public Item(T value) { Value = value; }
			#region IXSerializerElement

			public void Serialize(XSerializer.SaveEntry entry)
			{
				entry.AddValue("Value", Value);
			}

			public void Deserialize(XSerializer.LoadEntry entry)
			{
				entry.GetValue("Value", out Value);
			}

			#endregion
		}

		/// <summary>
		/// 複数のIElementをまとめて保存したり読み込んだり
		/// </summary>
		public sealed class ElementsManager : IElement
		{
			readonly string _rootName;
			readonly string _filePath;
			readonly Dictionary<string, IElement> _elements;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="rootElementName">XMLのルート要素名</param>
			/// <param name="filePath">ファイルフルパス</param>
			public ElementsManager(string rootElementName, string filePath)
			{
				_rootName = rootElementName;
				_filePath = filePath;
				_elements = new Dictionary<string, IElement>();
			}

			/// <summary>
			/// Save()/Load()の対象となるIElementの参照を追加したり消したり。
			/// </summary>
			/// <param name="name">名前。XMLの要素名になります。</param>
			/// <param name="element">要素。参照を消す場合はnullを。</param>
			public void SetReference(string name, IElement element)
			{
				if(element != null)
				{
					// 上書きするのでAdd()を使わずに。
					_elements[name] = element;
				}
				else
				{
					_elements.Remove(name);
				}
			}

			/// <summary>
			/// 指定されたファイルから読み込みます。
			/// </summary>
			public void Load()
			{
				this.DeserializeFromFile(_rootName, _filePath);
			}

			/// <summary>
			/// 指定されたファイルへ保存します。
			/// </summary>
			public void Save()
			{
				this.SerializeToFile(_rootName, _filePath);
			}

			#region XSerializer.IElement

			void IElement.Serialize(SaveEntry entry)
			{
				_elements.ForEach(item => entry.AddValue(item.Key, item.Value));
			}

			void IElement.Deserialize(LoadEntry entry)
			{
				_elements.ForEach(item => entry.GetValue(item.Key, item.Value));
			}

			#endregion
		}
	}
}
