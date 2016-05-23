namespace SmartAudioPlayerFx.Views.Options
{
	partial class OptionDialog
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

		#region Windows フォーム デザイナで生成されたコード

		/// <summary>
		/// デザイナ サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディタで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionDialog));
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.contentsName = new System.Windows.Forms.Label();
			this.optionGroupList = new System.Windows.Forms.ListBox();
			this.optionPanel = new System.Windows.Forms.Panel();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.versionLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(468, 239);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 3;
			this.OK.Text = "OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(549, 239);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 4;
			this.Cancel.Text = "キャンセル";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// contentsName
			// 
			this.contentsName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.contentsName.BackColor = System.Drawing.SystemColors.ControlDark;
			this.contentsName.Font = new System.Drawing.Font("MS UI Gothic", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.contentsName.ForeColor = System.Drawing.SystemColors.ControlLightLight;
			this.contentsName.Location = new System.Drawing.Point(169, 7);
			this.contentsName.Name = "contentsName";
			this.contentsName.Size = new System.Drawing.Size(455, 20);
			this.contentsName.TabIndex = 7;
			this.contentsName.Text = "contansName";
			this.contentsName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// optionGroupList
			// 
			this.optionGroupList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.optionGroupList.Font = new System.Drawing.Font("MS UI Gothic", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.optionGroupList.FormattingEnabled = true;
			this.optionGroupList.IntegralHeight = false;
			this.optionGroupList.ItemHeight = 15;
			this.optionGroupList.Location = new System.Drawing.Point(7, 7);
			this.optionGroupList.Name = "optionGroupList";
			this.optionGroupList.Size = new System.Drawing.Size(159, 255);
			this.optionGroupList.TabIndex = 0;
			this.optionGroupList.SelectedIndexChanged += new System.EventHandler(this.optionGroupList_SelectedIndexChanged);
			// 
			// optionPanel
			// 
			this.optionPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.optionPanel.AutoScroll = true;
			this.optionPanel.BackColor = System.Drawing.Color.Transparent;
			this.optionPanel.Location = new System.Drawing.Point(171, 30);
			this.optionPanel.Name = "optionPanel";
			this.optionPanel.Size = new System.Drawing.Size(453, 200);
			this.optionPanel.TabIndex = 1;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Location = new System.Drawing.Point(172, 234);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(452, 2);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			// 
			// versionLabel
			// 
			this.versionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.versionLabel.AutoSize = true;
			this.versionLabel.BackColor = System.Drawing.Color.Transparent;
			this.versionLabel.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.versionLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.versionLabel.Location = new System.Drawing.Point(172, 244);
			this.versionLabel.Name = "versionLabel";
			this.versionLabel.Size = new System.Drawing.Size(171, 12);
			this.versionLabel.TabIndex = 6;
			this.versionLabel.Text = "SmartAudioPlayer Fx x.x.xxxx.xx";
			// 
			// OptionDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(630, 267);
			this.Controls.Add(this.contentsName);
			this.Controls.Add(this.versionLabel);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Controls.Add(this.optionPanel);
			this.Controls.Add(this.optionGroupList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OptionDialog";
			this.Text = "オプション";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OptionDialog_FormClosed);
			this.Load += new System.EventHandler(this.OptionDialog_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox optionGroupList;
		private System.Windows.Forms.Panel optionPanel;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label versionLabel;
		private System.Windows.Forms.Label contentsName;
		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
	}
}