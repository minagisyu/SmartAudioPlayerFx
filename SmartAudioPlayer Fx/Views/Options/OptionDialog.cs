using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;
using Quala.Win32;
using Quala.Extensions;

namespace SmartAudioPlayerFx.Views.Options
{
	sealed partial class OptionDialog : Form
	{
		List<OptionPage> optionPages;

		public int PageIndex { get; set; }
		public int InactiveOpacity { get; set; }
		public int DeactiveOpacity { get; set; }
		public MainWindow MainWindow { get; set; }

		public OptionDialog()
		{
			InitializeComponent();
			optionPages = new List<OptionPage>();
			optionPages.Add(new OptionDialog_MediaList() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_MediaList2() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_Window() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_ShortcutKey() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_Update() { ParentDialog = this, });
			optionPages.Add(new OptionDialog_DB() { ParentDialog = this, });

			// Preferences
			ManagerServices.PreferencesManager.WindowSettings.Subscribe(x => LoadWindowPreferences(x));
		}

		void SavePreferences()
		{
			ManagerServices.PreferencesManager.WindowSettings.Value
				.SetAttributeValueEx("OptionPage", PageIndex);
		}
		void LoadWindowPreferences(XElement windowSettings)
		{
			windowSettings
				.GetAttributeValueEx(this, _ => PageIndex, "OptionPage");
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= (int)WinAPI.WS_EX.TOPMOST;
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
			SavePreferences();
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
