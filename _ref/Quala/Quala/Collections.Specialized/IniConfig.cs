using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Quala.Interop.Win32;

namespace Quala.Collections.Specialized
{
	/// <summary>
	/// 構成情報を記録するクラス。
	/// 単にDictionaryのラッパで、ファイル保存形式がiniファイル。
	/// </summary>
	/// <remarks>
	/// 情報は Key = Value の形式で表現する。
	/// KeyやValueは小文字で表現される。
	/// 手抜きのため日本語も使えるが、推奨しない。
	/// ----------------------------------------------------------------------
	/// Keyをネームスペースのように.(ピリオド)区切り、
	/// 最初のピリオドまでがiniファイルのセクションとして表現される。
	/// aaa.bbb = ccc
	///     ↓
	/// [aaa]
	/// bbb = ccc
	/// ----------------------------------------------------------------------
	/// キー名にセクションに相当する部分がないときは"default"が付加される。
	/// "default"なしでアクセスは可能だが、"default"と競合する。
	/// bbb = ccc
	///     ↓
	/// default.bbb = ccc
	///     ↓
	/// [default]
	/// bbb = ccc
	/// ----------------------------------------------------------------------
	/// ・インデクサでKeyに対してnull or Emptyを代入すると、そのKeyを削除する。
	/// ・インデクサでKeyがない状態で取得すると、nullが返る。
	/// ・インデクサ(object)でSetterはToStringを呼び出す。
	/// ・Get()でKeyがない場合は、default(T)か、指定されたデフォルト値が返る。
	/// ・Valueはstringで保持される、Getメソッド時はParseメソッドを呼び出す。
	/// -------------------
	/// ファイル保存は自前実装を目指したが、
	/// 最後の最後でGetPrivateProfileString()APIを使って保存することに決定。
	/// 理由は、ファイルにある既存の項目(コメントなど)を残すため。
	/// </remarks>
	public sealed class IniConfig
	{
		static Regex regex = new Regex(@"(?<section>[^.]+)\.(?<key>.+)", RegexOptions.Compiled);
		Dictionary<string, IConvertible> objects = new Dictionary<string, IConvertible>();

		/// <summary>
		/// キー名を検証する。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">キー名が間違っている</exception>
		static string KeyValidation(string key)
		{
			// キー名がnull or 空白ならエラー
			if(string.IsNullOrEmpty(key))
				throw new ArgumentException("キー名が間違っています", "key");

			// まずは小文字に。
			key = key.ToLower();

			// セクションに相当する部分がないときは"default."を追加
			if(!regex.IsMatch(key))
				key = "default." + key;

			return key;
		}

		public IConvertible this[string key]
		{
			get
			{
				key = KeyValidation(key);
				return objects.ContainsKey(key) ? objects[key] : null;
			}
			set
			{
				key = KeyValidation(key);
				if(value == null) objects.Remove(key);
				else objects[key] = value;
			}
		}

		public T Get<T>(string key)
			where T : IConvertible
		{
			return Get<T>(key, default(T));
		}

		public T Get<T>(string key, T defaultValue)
			where T : IConvertible
		{
			// 変換を試みる
			object o = Convert.ChangeType(this[key], typeof(T));
			T value = (o != null) ? (T)o : defaultValue;
			return value;
		}

		public void Save(string path)
		{
			Dictionary<string, Dictionary<string, string>> sections =
				new Dictionary<string, Dictionary<string, string>>();
			foreach(KeyValuePair<string, IConvertible> item in objects)
			{
				Match m = regex.Match(item.Key);
				if(!m.Success || m.Groups.Count != 3)
				{
					throw new Exception();
				}

				// セクションとキーと値に分離
				string section = m.Groups["section"].Value;
				string key = m.Groups["key"].Value;
				string value = item.Value.ToString();
				if(!sections.ContainsKey(section))
					sections[section] = new Dictionary<string, string>();
				sections[section][key] = value;
			}

			// ファイルへ保存
			foreach(KeyValuePair<string, Dictionary<string, string>> secKeys in sections)
				foreach(KeyValuePair<string, string> keyval in secKeys.Value)
					API.WritePrivateProfileString(secKeys.Key, keyval.Key, keyval.Value, path);
		}

		public void Load(string path)
		{
			// 内容破棄。
			objects.Clear();

			// ファイルから読み込み
			foreach(string section in API.GetPrivateProfileSections(10240, path))
			{
				if(section == "defaul") break;
				foreach(string key in API.GetPrivateProfileKeys(10240, section, path))
				{
					if(key == "defaul") break;
					StringBuilder sb = new StringBuilder(10240);
					if(API.GetPrivateProfileString(section,
						key, "default", sb, (uint)sb.Capacity, path) != 0)
					{
						objects[section + "." + key] = sb.ToString();
					}
				}
			}
		}

	}
}
