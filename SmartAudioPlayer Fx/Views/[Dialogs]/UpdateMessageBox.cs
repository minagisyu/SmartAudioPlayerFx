using System.Windows.Forms;

namespace SmartAudioPlayerFx.Views
{
	/// <summary>
	/// 更新内容とアップデートの可否を選択するメッセージボックス。
	/// ダウンロード→ DialogResult.No
	/// 更新→ DialogResult.Yes
	/// スキップ→ DialogResult.Ignore
	/// キャンセル→ DialogResult.Cansel
	/// </summary>
	public partial class UpdateMessageBox : Form
	{
		public UpdateMessageBox()
		{
			InitializeComponent();
		}

		public string CurrentVersionString
		{
			get { return textbox_current_version.Text; }
			set { textbox_current_version.Text = value; }
		}
		public string NewVersionString
		{
			get { return textbox_new_version.Text; }
			set { textbox_new_version.Text = value; }
		}
		public string UpdateDescription
		{
			get { return textbox_description.Text; }
			set { textbox_description.Text = value; }
		}

	}
}
