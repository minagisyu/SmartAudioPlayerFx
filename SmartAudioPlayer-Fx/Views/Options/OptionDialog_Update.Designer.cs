namespace SmartAudioPlayerFx.Views.Options
{
	partial class OptionDialog_Update
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
			this.label1 = new System.Windows.Forms.Label();
			this.label_last_check_date = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label_last_check_version = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.checkBox_updateCheckEnabled = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(-2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(67, 12);
			this.label1.TabIndex = 0;
			this.label1.Text = "最終確認日:";
			// 
			// label_last_check_date
			// 
			this.label_last_check_date.AutoSize = true;
			this.label_last_check_date.Location = new System.Drawing.Point(131, 0);
			this.label_last_check_date.Name = "label_last_check_date";
			this.label_last_check_date.Size = new System.Drawing.Size(109, 12);
			this.label_last_check_date.TabIndex = 1;
			this.label_last_check_date.Text = "0000/00/00 00:00:00";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(-2, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(127, 12);
			this.label2.TabIndex = 2;
			this.label2.Text = "最後に確認したバージョン:";
			// 
			// label_last_check_version
			// 
			this.label_last_check_version.AutoSize = true;
			this.label_last_check_version.Location = new System.Drawing.Point(131, 16);
			this.label_last_check_version.Name = "label_last_check_version";
			this.label_last_check_version.Size = new System.Drawing.Size(53, 12);
			this.label_last_check_version.TabIndex = 3;
			this.label_last_check_version.Text = "0.0.00.000";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(3, 70);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(140, 23);
			this.button1.TabIndex = 4;
			this.button1.Text = "今すぐ更新をチェック";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// checkBox_updateCheckEnabled
			// 
			this.checkBox_updateCheckEnabled.AutoSize = true;
			this.checkBox_updateCheckEnabled.Location = new System.Drawing.Point(0, 40);
			this.checkBox_updateCheckEnabled.Name = "checkBox_updateCheckEnabled";
			this.checkBox_updateCheckEnabled.Size = new System.Drawing.Size(231, 16);
			this.checkBox_updateCheckEnabled.TabIndex = 5;
			this.checkBox_updateCheckEnabled.Text = "アプリケーションの更新を定期的にチェックする";
			this.checkBox_updateCheckEnabled.UseVisualStyleBackColor = true;
			// 
			// OptionDialog_Update
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.checkBox_updateCheckEnabled);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label_last_check_version);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label_last_check_date);
			this.Controls.Add(this.label1);
			this.DoubleBuffered = true;
			this.Name = "OptionDialog_Update";
			this.PageName = "アップデート";
			this.Size = new System.Drawing.Size(450, 155);
			this.Load += new System.EventHandler(this.OptionDialog_Update_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label_last_check_date;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label_last_check_version;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.CheckBox checkBox_updateCheckEnabled;


	}
}
