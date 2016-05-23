using System;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Quala.Win32.Dialog
{
	/// <summary>
	/// IFileOpenDialogを使うラッパー。
	/// インターフェイスが使えない(Vista以下)ときは、WinFormsのFolderBrowserDialogを使います。
	/// プロパティなどはFolderBrowserDialog互換ですが、一部未対応の物があります。
	/// </summary>
	public sealed class FolderBrowserDialogEx : CommonDialog
	{
		string _discription;
		string _selectedPath;
		bool _showNewFolderButton;
		Environment.SpecialFolder _rootFolder;

		public FolderBrowserDialogEx()
		{
			Reset();
		}

		public override void Reset()
		{
			_discription = string.Empty;
			_selectedPath = string.Empty;
			_showNewFolderButton = true;
			_rootFolder = Environment.SpecialFolder.Desktop;
			UseNewDialog = true;
		}

		/// <summary>
		/// タイトル
		/// </summary>
		public string Description
		{
			get { return _discription; }
			set { _discription = value; }
		}

		/// <summary>
		/// 選択されたパス、及び初期選択パス
		/// </summary>
		public string SelectedPath
		{
			get { return _selectedPath; }
			set { _selectedPath = value; }
		}

		/// <summary>
		/// [新しいフォルダ]ボタン
		/// (IFileOpenDialog版は無視)
		/// </summary>
		public bool ShowNewFolderButton
		{
			get { return _showNewFolderButton; }
			set { _showNewFolderButton = value; }
		}

		/// <summary>
		/// ルートフォルダ
		/// (IFileOpenDialog版は無視)
		/// </summary>
		public Environment.SpecialFolder RootFolder
		{
			get { return _rootFolder; }
			set { _rootFolder = value; }
		}

		/// <summary>
		/// Vista以降の新しいダイアログを使用する？
		/// (vistaのフォルダ選択にはバグっぽい動作あり)
		/// </summary>
		public bool UseNewDialog
		{
			get;
			set;
		}

		protected override bool RunDialog(IntPtr hwndOwner)
		{
			if(Environment.OSVersion.Version.Major >= 6 && UseNewDialog)
			{
				using (var dialog = new CommonOpenFileDialog())
				{
					dialog.AllowNonFileSystemItems = false;
					dialog.AllowPropertyEditing = false;
					dialog.DefaultDirectory = _selectedPath;
					dialog.EnsureFileExists = true;
					dialog.EnsurePathExists = true;
					dialog.EnsureReadOnly = true;
					dialog.EnsureValidNames = true;
					dialog.InitialDirectory = _selectedPath;
					dialog.IsFolderPicker = true;
					dialog.Multiselect = false;
					dialog.NavigateToShortcut = true;
					dialog.RestoreDirectory = false;
					dialog.ShowHiddenItems = false;
					dialog.ShowPlacesList = true;
					dialog.Title = _discription;
					if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
					{
						_selectedPath = dialog.FileName;
						return true;
					}
					return false;
				}
			}

			// 通常の方法で試す。
			using(var dialog = new FolderBrowserDialog())
			{
				dialog.Description = _discription;
				dialog.SelectedPath = _selectedPath;
				dialog.RootFolder = _rootFolder;
				dialog.ShowNewFolderButton = _showNewFolderButton;
				if(dialog.ShowDialog() == DialogResult.OK)
				{
					_selectedPath = dialog.SelectedPath;
					return true;
				}
			}

			// ダメでしたー
			return false;
		}

	}
}
