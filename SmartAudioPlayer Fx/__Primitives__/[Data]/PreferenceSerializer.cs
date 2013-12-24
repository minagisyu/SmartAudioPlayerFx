namespace __Primitives__
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Security.AccessControl;
	using System.Security.Principal;
	using System.Xml;
	using System.Xml.Linq;

	/// <summary>
	/// XElementを介した設定データ用シリアライザ＆ファイルIOメソッド
	/// </summary>
	sealed class PreferenceSerializer
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
		public PreferenceSerializer(bool useAppDataDir=true)
		{
			// exeと同じフォルダに書き込めるかテスト
			var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			var dir = Path.GetDirectoryName(asm.Location);
			if (useAppDataDir == false && IsGotWriteAccessPermission(dir))
			{
				// 書き込み権限がある
				BaseDir = dir;
			}
			else
			{
				// 権限がない or 指定されたのでAppDataへ書き込む
				var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var appname = asm.GetName().Name;
				BaseDir = Path.Combine(appdata, appname);
				Directory.CreateDirectory(BaseDir);
			}
		}

		// BaseDir+nameでファイルパスを作成、必要であればディレクトリも作成する
		public string CreateFullPath(params string[] name)
		{
			var path = Path.Combine(BaseDir ?? string.Empty, Path.Combine(name));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			return path;
		}

		/// <summary>
		/// 指定ファイルへ保存。
		/// インデント有効と文字チェック無効設定。
		/// </summary>
		/// <param name="element"></param>
		/// <param name="filepaths"></param>
		public void Save(XElement element, params string[] filepaths)
		{
			var path = CreateFullPath(filepaths);
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

		/// <summary>
		/// 指定ファイルからロードします。
		/// ファイルが無い、読み込みエラー、ルート要素名がnameと違う場合は空のXElement(name)が返ります。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="filepaths"></param>
		/// <returns></returns>
		public XElement Load(XName name, params string[] filepaths)
		{
			var path = CreateFullPath(filepaths);
			if (File.Exists(path) == false)
				return new XElement(name);

			using (var stream = File.OpenRead(path))
			using (var reader = XmlTextReader.Create(stream, new XmlReaderSettings() { CheckCharacters = false, }))
			{
				var element = XElement.Load(reader);
				return (element != null && element.Name.Equals(name)) ? element : new XElement(name);
			}
		}

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
