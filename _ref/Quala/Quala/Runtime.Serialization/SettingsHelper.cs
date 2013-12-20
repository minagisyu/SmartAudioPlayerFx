using System;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;

namespace Quala.Runtime.Serialization
{
	public static class SettingsHelper
	{
		// アプリフォルダかユーザーフォルダからファイルを読み込む
		// 1. アプリフォルダの書き込み権限がないならユーザーフォルダから読み込む
		// 2. ユーザーフォルダにファイルがないならアプリフォルダから読み込みを試す
		// 3. アプリフォルダにもファイルがないなら、読み込まれなかったという事でnullを返す
		public static T Load<T>(string filename)
		{
			var asm = Assembly.GetEntryAssembly();
			var appDir_FilePath = Path.Combine(Path.GetDirectoryName(asm.Location), filename);
			var userDir_FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.Combine(asm.GetName().Name, filename));

			// アプリフォルダにファイルある？
			if (File.Exists(appDir_FilePath))
			{
				// アプリフォルダに書き込み権限ある？(日付を更新して確認)
				try
				{
					var time = File.GetLastWriteTime(appDir_FilePath);
					File.SetLastWriteTime(appDir_FilePath, time);
					return Deserialize<T>(appDir_FilePath);
				}
				catch (UnauthorizedAccessException)
				{
					// 書き込み権限ないのでUserDirから。。。
					if (File.Exists(userDir_FilePath))
					{
						return Deserialize<T>(userDir_FilePath);
					}
				}
			}

			return default(T);
		}

		// アプリフォルダかユーザーフォルダにファイルを書き込む
		// 1. アプリフォルダの書き込み権限がないならユーザーフォルダに書き込む
		// 2. アプリフォルダに書き込めてユーザーフォルダにもファイルがあるなら削除する
		public static void Save<T>(string filename, T obj)
		{
			var asm = Assembly.GetEntryAssembly();
			var appDir_FilePath = Path.Combine(Path.GetDirectoryName(asm.Location), filename);
			var userDir_FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.Combine(asm.GetName().Name, filename));

			// アプリフォルダに書ける？
			try
			{
				Serialize(appDir_FilePath, obj);
				// UserDirにもあったら消してみる
				if (File.Exists(userDir_FilePath))
					File.Delete(userDir_FilePath);
			}
			catch (UnauthorizedAccessException)
			{
				// 書けないのでUserDirへ…
				Serialize(userDir_FilePath, obj);
			}
		}

		static void Serialize<T>(string path, T obj)
		{
			using (var writer = XmlWriter.Create(path, new XmlWriterSettings() { Indent = true }))
			{
				new DataContractSerializer(typeof(T))
					.WriteObject(writer, obj);
			}
		}

		static T Deserialize<T>(string path)
		{
			using (var stream = File.OpenRead(path))
			{
				try { return (T)new DataContractSerializer(typeof(T)).ReadObject(stream); }
				catch (XmlException) { }
				return default(T);
			}
		}

	}
}
