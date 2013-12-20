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
	/// �\�������L�^����N���X�B
	/// �P��Dictionary�̃��b�p�ŁA�t�@�C���ۑ��`����ini�t�@�C���B
	/// </summary>
	/// <remarks>
	/// ���� Key = Value �̌`���ŕ\������B
	/// Key��Value�͏������ŕ\�������B
	/// �蔲���̂��ߓ��{����g���邪�A�������Ȃ��B
	/// ----------------------------------------------------------------------
	/// Key���l�[���X�y�[�X�̂悤��.(�s���I�h)��؂�A
	/// �ŏ��̃s���I�h�܂ł�ini�t�@�C���̃Z�N�V�����Ƃ��ĕ\�������B
	/// aaa.bbb = ccc
	///     ��
	/// [aaa]
	/// bbb = ccc
	/// ----------------------------------------------------------------------
	/// �L�[���ɃZ�N�V�����ɑ������镔�����Ȃ��Ƃ���"default"���t�������B
	/// "default"�Ȃ��ŃA�N�Z�X�͉\�����A"default"�Ƌ�������B
	/// bbb = ccc
	///     ��
	/// default.bbb = ccc
	///     ��
	/// [default]
	/// bbb = ccc
	/// ----------------------------------------------------------------------
	/// �E�C���f�N�T��Key�ɑ΂���null or Empty��������ƁA����Key���폜����B
	/// �E�C���f�N�T��Key���Ȃ���ԂŎ擾����ƁAnull���Ԃ�B
	/// �E�C���f�N�T(object)��Setter��ToString���Ăяo���B
	/// �EGet()��Key���Ȃ��ꍇ�́Adefault(T)���A�w�肳�ꂽ�f�t�H���g�l���Ԃ�B
	/// �EValue��string�ŕێ������AGet���\�b�h����Parse���\�b�h���Ăяo���B
	/// -------------------
	/// �t�@�C���ۑ��͎��O������ڎw�������A
	/// �Ō�̍Ō��GetPrivateProfileString()API���g���ĕۑ����邱�ƂɌ���B
	/// ���R�́A�t�@�C���ɂ�������̍���(�R�����g�Ȃ�)���c�����߁B
	/// </remarks>
	public sealed class IniConfig
	{
		static Regex regex = new Regex(@"(?<section>[^.]+)\.(?<key>.+)", RegexOptions.Compiled);
		Dictionary<string, IConvertible> objects = new Dictionary<string, IConvertible>();

		/// <summary>
		/// �L�[�������؂���B
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">�L�[�����Ԉ���Ă���</exception>
		static string KeyValidation(string key)
		{
			// �L�[����null or �󔒂Ȃ�G���[
			if(string.IsNullOrEmpty(key))
				throw new ArgumentException("�L�[�����Ԉ���Ă��܂�", "key");

			// �܂��͏������ɁB
			key = key.ToLower();

			// �Z�N�V�����ɑ������镔�����Ȃ��Ƃ���"default."��ǉ�
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
			// �ϊ������݂�
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

				// �Z�N�V�����ƃL�[�ƒl�ɕ���
				string section = m.Groups["section"].Value;
				string key = m.Groups["key"].Value;
				string value = item.Value.ToString();
				if(!sections.ContainsKey(section))
					sections[section] = new Dictionary<string, string>();
				sections[section][key] = value;
			}

			// �t�@�C���֕ۑ�
			foreach(KeyValuePair<string, Dictionary<string, string>> secKeys in sections)
				foreach(KeyValuePair<string, string> keyval in secKeys.Value)
					API.WritePrivateProfileString(secKeys.Key, keyval.Key, keyval.Value, path);
		}

		public void Load(string path)
		{
			// ���e�j���B
			objects.Clear();

			// �t�@�C������ǂݍ���
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
