using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Quala.Interop.Win32;
using SmartAudioPlayerFx.Views;

namespace SmartAudioPlayerFx.Options
{
	sealed partial class OptionDialog : Form
	{
		List<OptionPage> optionPages;

		public int PageIndex { get; set; }
		public int InactiveOpacity { get; set; }
		public int DeactiveOpacity { get; set; }
		public PlayerWindow PlayerWindow { get; set; }

		public OptionDialog()
		{
			InitializeComponent();
			optionPages = new List<OptionPage>();
			optionPages.Add(new OptionDialog_MediaList() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_Window() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_ShortcutKey() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_Update() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_DB() { ParentDialog = this, });
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= (int)WS_EX.TOPMOST;
				return cp;
			}
		}

		void OptionDialog_Load(object sender, EventArgs e)
		{
			// バージョンラベル設定
			versionLabel.Text = "SmartAudioPlayer Fx " +
				Assembly.GetEntryAssembly().GetName().Version.ToString();
			//
			var p = optionPages.Find(p2 => p2 is OptionDialog_Window) as OptionDialog_Window;
			p.InactiveOpacity = this.InactiveOpacity;
			p.DeactiveOpacity = this.DeactiveOpacity;
			//
			optionGroupList.DataSource = optionPages;
			optionGroupList.DisplayMember = "PageName";
			foreach(OptionPage page in optionPages)
			{
				page.Dock = DockStyle.Fill;
				optionPanel.Controls.Add(page);
			}
			// 最後に開いてたインデックスをデフォルトに
			optionGroupList.SelectedIndex = PageIndex;
		}

		void OptionDialog_FormClosed(object sender, FormClosedEventArgs e)
		{
			if(DialogResult != DialogResult.OK) return;
			foreach(OptionPage page in optionPages)
				page.Save();
			// 最後に開いていたインデックスを保存
			PageIndex = optionGroupList.SelectedIndex;
			//
			var p = optionPages.Find(p2 => p2 is OptionDialog_Window) as OptionDialog_Window;
			this.InactiveOpacity = p.InactiveOpacity;
			this.DeactiveOpacity = p.DeactiveOpacity;
		}

		void optionGroupList_SelectedIndexChanged(object sender, EventArgs e)
		{
			OptionPage item = optionGroupList.SelectedItem as OptionPage;
			if(item == null) return;

			contentsName.Text = item.PageName;
			item.BringToFront();
		}

		void OK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

	}

	class OptionPage : UserControl
	{
		public OptionDialog ParentDialog { get; internal set; }
		public virtual string PageName { get; set; }
		public virtual void Save() { }
	}
}
