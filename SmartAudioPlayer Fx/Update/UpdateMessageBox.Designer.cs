namespace SmartAudioPlayerFx.Update
{
	partial class UpdateMessageBox
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateMessageBox));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.textbox_current_version = new System.Windows.Forms.TextBox();
			this.textbox_new_version = new System.Windows.Forms.TextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.textbox_description = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(171, 12);
			this.label1.TabIndex = 0;
			this.label1.Text = "新しいバージョンが利用可能です！";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(353, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(86, 12);
			this.label2.TabIndex = 1;
			this.label2.Text = "現在のバージョン:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(354, 25);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(83, 12);
			this.label3.TabIndex = 3;
			this.label3.Text = "新しいバージョン:";
			// 
			// textbox_current_version
			// 
			this.textbox_current_version.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textbox_current_version.Location = new System.Drawing.Point(445, 9);
			this.textbox_current_version.Name = "textbox_current_version";
			this.textbox_current_version.ReadOnly = true;
			this.textbox_current_version.Size = new System.Drawing.Size(64, 12);
			this.textbox_current_version.TabIndex = 2;
			this.textbox_current_version.Text = "0.0.00.000";
			// 
			// textbox_new_version
			// 
			this.textbox_new_version.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textbox_new_version.Location = new System.Drawing.Point(445, 25);
			this.textbox_new_version.Name = "textbox_new_version";
			this.textbox_new_version.ReadOnly = true;
			this.textbox_new_version.Size = new System.Drawing.Size(64, 12);
			this.textbox_new_version.TabIndex = 4;
			this.textbox_new_version.Text = "0.0.0.0";
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.textbox_description);
			this.groupBox1.Location = new System.Drawing.Point(12, 43);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(497, 113);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "更新内容";
			// 
			// textbox_description
			// 
			this.textbox_description.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textbox_description.Location = new System.Drawing.Point(7, 18);
			this.textbox_description.Multiline = true;
			this.textbox_description.Name = "textbox_description";
			this.textbox_description.ReadOnly = true;
			this.textbox_description.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.textbox_description.Size = new System.Drawing.Size(484, 89);
			this.textbox_description.TabIndex = 0;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.button1.Location = new System.Drawing.Point(141, 162);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 6;
			this.button1.Text = "更新";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button2.DialogResult = System.Windows.Forms.DialogResult.Ignore;
			this.button2.Location = new System.Drawing.Point(353, 162);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 8;
			this.button2.Text = "スキップ";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// button3
			// 
			this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button3.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button3.Location = new System.Drawing.Point(434, 162);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(75, 23);
			this.button3.TabIndex = 9;
			this.button3.Text = "キャンセル";
			this.button3.UseVisualStyleBackColor = true;
			// 
			// button4
			// 
			this.button4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button4.DialogResult = System.Windows.Forms.DialogResult.No;
			this.button4.Location = new System.Drawing.Point(222, 162);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(125, 23);
			this.button4.TabIndex = 7;
			this.button4.Text = "ダウンロードして保存";
			this.button4.UseVisualStyleBackColor = true;
			// 
			// UpdateMessageBox
			// 
			this.AcceptButton = this.button1;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button3;
			this.ClientSize = new System.Drawing.Size(521, 197);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.textbox_new_version);
			this.Controls.Add(this.textbox_current_version);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "UpdateMessageBox";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "SmartAudioPlayer Fx";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textbox_current_version;
		private System.Windows.Forms.TextBox textbox_new_version;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TextBox textbox_description;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
	}
}