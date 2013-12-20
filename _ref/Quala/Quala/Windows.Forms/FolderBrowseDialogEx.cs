using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Quala.Interop.Win32;
using Quala.Interop.Win32.COM;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Quala.Windows.Forms
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

		protected override bool RunDialog(IntPtr hwndOwner)
		{
			if(Environment.OSVersion.Version.Major >= 6)
			{
			/*	IFileOpenDialog dialog = null;
				IShellItem shiDefaultFolder = null;
				try
				{
					dialog = (IFileOpenDialog)Activator
						.CreateInstance(Type.GetTypeFromCLSID(CLSID.FileOpenDialog));
					dialog.SetTitle(_discription);
					dialog.SetOptions(
						FILEOPENDIALOGOPTIONS.PICKFOLDERS |
						FILEOPENDIALOGOPTIONS.FILEMUSTEXIST |
						FILEOPENDIALOGOPTIONS.PATHMUSTEXIST);

					try
					{
						shiDefaultFolder = API.GetShellItemFromPath(_selectedPath);
						dialog.SetDefaultFolder(shiDefaultFolder);
						dialog.SetFolder(shiDefaultFolder);
					}
					catch(Exception e) { }	// 例外は無視。

					dialog.Show(hwndOwner);


					IShellItem ppsi;
					dialog.GetResult(out ppsi);
					IntPtr name;
					ppsi.GetDisplayName(SIGDN.FILESYSPATH, out name);
					var name2 = Marshal.PtrToStringAuto(name);
					Marshal.FreeCoTaskMem(name);
					_selectedPath = name2;
					return true;
				}
				catch(COMException e)
				{
					// キャンセルが押された
					if(e.ErrorCode == unchecked((int)0x800704C7)) // ERROR_CANCELLED
					{
						return false;
					}

					// それ以外は別の方法を試してみる
				}
				finally
				{
					if(shiDefaultFolder != null)
					{
						var refCount = Marshal.ReleaseComObject(shiDefaultFolder);
						shiDefaultFolder = null;
					}
					if(dialog != null)
					{
						var refCount = Marshal.ReleaseComObject(dialog);
						dialog = null;
					}
				}*/

				using (var dialog = new CommonOpenFileDialog())
				{
					dialog.AllowNonFileSystemItems = false;
					dialog.AllowPropertyEditing = false;
					dialog.DefaultDirectory = _selectedPath;
					dialog.EnsurePathExists = true;
					dialog.EnsureReadOnly = true;
					dialog.InitialDirectory = _selectedPath;
					dialog.IsFolderPicker = true;
					dialog.Multiselect = false;
					dialog.NavigateToShortcut = true;
					dialog.RestoreDirectory = false;
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
