using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;
using System.Xml.Linq;

namespace Quala.Extensions
{
	/// <summary>
	/// XElementを介した設定データ用シリアライザ＆ファイルIOメソッド
	/// </summary>
	public sealed class XmlPreferenceSerializer
	{
		/// <summary>
		/// 設定のための基準ディレクトリ
		/// </summary>
		public string BaseDir { get; private set; }

		/// <summary>
		/// </summary>
		/// <param name="useAppDataDir">
		/// AppDataフォルダを強制的に使用する場合はtrue,
		/// falseの場合、exeと同じ場所の書き込み権限がある場合はappdir、ない場合はappdataになります。
		/// </param>
		public XmlPreferenceSerializer()
		{
			// exeと同じフォルダに書き込めるかテスト
			var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			// 権限がない or 指定されたのでAppDataへ書き込む
			var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var appname = asm.GetName().Name;
			BaseDir = Path.Combine(appdata, appname);
		}

		/// <summary>
		/// 指定ファイルへ保存。
		/// インデント有効と文字チェック無効設定。
		/// </summary>
		/// <param name="element"></param>
		/// <param name="filepaths"></param>
		public void Save(XElement element, params string[] filepaths)
		{
			var path = Path.Combine(BaseDir ?? string.Empty, Path.Combine(filepaths));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (var stream = File.Open(path, FileMode.Create, FileAccess.Write))
			using (var writer = XmlWriter.Create(stream, new XmlWriterSettings() { CheckCharacters = false, Indent = true, }))
			{
				// 整形してみる, 無効な文字のせいで例外出たらelementをそのまま使う
				XElement elm = null;
				try { elm = XElement.Parse(element.ToString()); }
				catch { elm = element; }
				elm.WriteTo(writer);
			}
		}

		/// <summary>
		/// 指定ファイルからロードします。
		/// ファイルが無い、読み込みエラー、ルート要素名がnameと違う場合は空のXElement(name)が返ります。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="filepaths"></param>
		/// <returns></returns>
		public XElement Load(XName name, params string[] filepaths)
		{
			var path = Path.Combine(BaseDir ?? string.Empty, Path.Combine(filepaths));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			if (File.Exists(path) == false)
				return new XElement(name);

			using (var stream = File.OpenRead(path))
			using (var reader = XmlTextReader.Create(stream, new XmlReaderSettings() { CheckCharacters = false, }))
			{
				var element = XElement.Load(reader);
				return (element != null && element.Name.Equals(name)) ? element : new XElement(name);
			}
		}

	}
}
