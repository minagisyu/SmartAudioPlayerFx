namespace SmartAudioPlayerFx.Options
{
	partial class OptionDialog_ShortcutKey
	{
		/// <summary> 
		/// 必要なデザイナ変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
		protected override void Dispose(bool disposing)
		{
			if(disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region コンポーネント デザイナで生成されたコード

		/// <summary> 
		/// デザイナ サポートに必要なメソッドです。このメソッドの内容を 
		/// コード エディタで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.keySetting = new SmartAudioPlayerFx.Options.KeySettingListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// keySetting
			// 
			this.keySetting.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.keySetting.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.keySetting.FullRowSelect = true;
			this.keySetting.GridLines = true;
			this.keySetting.Location = new System.Drawing.Point(0, 0);
			this.keySetting.MultiSelect = false;
			this.keySetting.Name = "keySetting";
			this.keySetting.Size = new System.Drawing.Size(450, 193);
			this.keySetting.TabIndex = 1;
			this.keySetting.UseCompatibleStateImageBehavior = false;
			this.keySetting.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "機能";
			this.columnHeader1.Width = 267;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "キー";
			this.columnHeader2.Width = 152;
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 208);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(370, 72);
			this.label2.TabIndex = 2;
			this.label2.Text = "※他のアプリケーションを操作しているときも有効です.\r\n\r\n・設定するには、機能を選択し対応させたいキーを押します.\r\n・設定を『なし』にするには、修飾キー(Ct" +
				"rl / Alt / Shiftのいずれか)を押します.\r\n・修飾キーの左右区別はありません.\r\n・一部のキーの組み合わせは設定できません.";
			// 
			// OptionDialog_Shortcut
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.label2);
			this.Controls.Add(this.keySetting);
			this.Name = "OptionDialog_Shortcut";
			this.PageName = "ショートカットキー";
			this.Size = new System.Drawing.Size(450, 280);
			this.Load += new System.EventHandler(this.OptionDialog_Shortcut_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private KeySettingListView keySetting;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Label label2;


	}
}
