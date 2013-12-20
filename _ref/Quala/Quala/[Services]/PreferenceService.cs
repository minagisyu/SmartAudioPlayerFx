using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;
using System.Xml.Linq;

namespace Quala
{
	/// <summary>
	/// 環境サービス --- XElementを介した設定データ用IOサービス
	/// デフォルトでAppDataを使用、ResetBaseDir()を使用することでexeと同じフォルダに設定可能(非推奨)
	/// </summary>
	public static class PreferenceService
	{
		/// <summary>
		/// 設定のための基準ディレクトリ
		/// </summary>
		public static string BaseDir { get; private set; }

		static PreferenceService()
		{
			ResetBaseDir(true);
		}

		/// <summary>
		/// BaseDir設定をリセット(再設定)します
		/// </summary>
		/// <param name="forced_appdata_dir">
		/// AppDataフォルダを強制的に使用する場合はtrue,
		/// falseの場合、exeと同じ場所の書き込み権限がある場合はappdir、ない場合はappdataになります。
		/// </param>
		public static void ResetBaseDir(bool forced_appdata_dir = false)
		{
			// exeと同じフォルダに書き込めるかテスト
			var asm = Assembly.GetEntryAssembly();
			var dir = Path.GetDirectoryName(asm.Location);
			if (forced_appdata_dir == false && IsGotWriteAccessPermission(dir))
			{
				// 書き込み権限がある
				BaseDir = dir;
			}
			else
			{
				// 権限がないのでAppDataへ書き込む
				var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var appname = asm.GetName().Name;
				BaseDir = Path.Combine(appdata, appname);
				Directory.CreateDirectory(BaseDir);
			}
		}

		// BaseDir+nameでファイルパスを作成、必要であればディレクトリも作成する
		public static string CreateFullPath(params string[] name)
		{
			var path = BaseDir ?? string.Empty;
			new List<string>(name).ForEach(n => path = Path.Combine(path, n));
			var dir = Path.GetDirectoryName(path);
			Directory.CreateDirectory(dir);
			return path;
		}

		public static void Save(XElement element, params string[] name)
		{
			var path = CreateFullPath(name);
			using (var stream = File.Open(path, FileMode.Create, FileAccess.Write))
			using (var writer = XmlTextWriter.Create(stream, new XmlWriterSettings() { CheckCharacters = false, Indent = true, }))
			{
				// 整形してみる, 無効な文字のせいで例外出たらelementをそのまま使う
				XElement elm = null;
				try { elm = XElement.Parse(element.ToString()); }
				catch { elm = element; }
				elm.WriteTo(writer);
			}
		}

		public static XElement Load(params string[] name)
		{
			var path = CreateFullPath(name);
			if (File.Exists(path) == false)
				return null;

			using (var stream = File.OpenRead(path))
			using (var reader = XmlTextReader.Create(stream, new XmlReaderSettings() { CheckCharacters = false, }))
			{
				return XElement.Load(reader);
			}
		}

		#region XElement系拡張メソッド

		[Obsolete("use XElement.SetAttributeValue() or XElement.SetElementValue()")]
		public static XElement AddValue<T>(this XElement element, string name, T value)
		{
			if (value != null)
				element.Add(new XAttribute(name, value));
			return element;
		}

		public static XElement AddValues<T>(this XElement element, string name, IEnumerable<T> values, Action<XElement, T> action)
		{
			if (values != null && action != null)
			{
				var p_element = new XElement(name);
				element.Add(p_element);
				foreach (var v in values)
				{
					var item = new XElement("Item");
					action(item, v);
					p_element.Add(item);
				}
			}
			return element;
		}

		public static T GetOrDefaultValue<T>(this XElement element, string name, T defaultValue)
		{
			// チェック
			if (element == null) return defaultValue;

			// 属性チェック
			var attr = element.Attribute(name);
			if (attr == null || string.IsNullOrEmpty(attr.Value)) return defaultValue;

			// 変換チェック
			var conv = TypeDescriptor.GetConverter(typeof(T));
			if (conv.CanConvertFrom(typeof(string)) == false)
				return defaultValue;

			// 変換
			return (T)conv.ConvertFromString(attr.Value);
		}

		public static IEnumerable<T> GetArrayValues<T>(this XElement element, string name, Func<XElement, T> func)
		{
			// チェック
			if (func == null || element == null) return new T[0];

			// 要素チェック
			var elm = element.Element(name);
			if (elm == null) return new T[0];

			return elm.Elements("Item").Select(el => func(el));
		}

		#endregion
		#region 権限ヘルパー

		// 書き込み権限があるか確認する
		//  --- TODO:「C:\TEST」などの場所は標準ユーザーでも権限があるはずなのに無いといわれる…。
		static bool IsGotWriteAccessPermission(string path)
		{
			var rule = GetCurrentAccessRule(path);
			return ((rule != null) && ((rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write));
		}

		// 現在のユーザーが持っている指定パスのFileSystemAccessRuleを得る
		static FileSystemAccessRule GetCurrentAccessRule(string path)
		{
			var fileSecurity = File.GetAccessControl(path);
			var rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)).OfType<FileSystemAccessRule>();
			var currentIdentity = WindowsIdentity.GetCurrent();
			var sids = new[] { currentIdentity.User }.Concat(currentIdentity.Groups);

			// アクセスルール内にユーザーSIDがある？無ければグループSIDも探す
			return rules.FirstOrDefault(rule => sids.Contains(rule.IdentityReference));
		}

		#endregion

	}
}
