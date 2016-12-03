namespace SmartAudioPlayerFx.Views.Options
{
	partial class OptionDialog_MediaList2
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.recentInterval = new System.Windows.Forms.TrackBar();
			this.recentIntervalLabel = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.recentInterval)).BeginInit();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.BackColor = System.Drawing.Color.Transparent;
			this.groupBox1.Controls.Add(this.panel1);
			this.groupBox1.Controls.Add(this.recentIntervalLabel);
			this.groupBox1.Location = new System.Drawing.Point(0, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(444, 43);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "「最近追加」項目の設定";
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.recentInterval);
			this.panel1.Location = new System.Drawing.Point(114, 12);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(324, 30);
			this.panel1.TabIndex = 5;
			// 
			// recentInterval
			// 
			this.recentInterval.Dock = System.Windows.Forms.DockStyle.Fill;
			this.recentInterval.Location = new System.Drawing.Point(0, 0);
			this.recentInterval.Maximum = 365;
			this.recentInterval.Minimum = 1;
			this.recentInterval.Name = "recentInterval";
			this.recentInterval.Size = new System.Drawing.Size(324, 30);
			this.recentInterval.TabIndex = 1;
			this.recentInterval.TickStyle = System.Windows.Forms.TickStyle.None;
			this.recentInterval.Value = 60;
			this.recentInterval.ValueChanged += new System.EventHandler(this.recentInterval_ValueChanged);
			// 
			// recentIntervalLabel
			// 
			this.recentIntervalLabel.AutoSize = true;
			this.recentIntervalLabel.Location = new System.Drawing.Point(6, 23);
			this.recentIntervalLabel.Name = "recentIntervalLabel";
			this.recentIntervalLabel.Size = new System.Drawing.Size(102, 12);
			this.recentIntervalLabel.TabIndex = 1;
			this.recentIntervalLabel.Text = "過去xxx日まで表示";
			// 
			// OptionDialog_MediaList2
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.groupBox1);
			this.Name = "OptionDialog_MediaList2";
			this.PageName = "メディアリスト(2)";
			this.Size = new System.Drawing.Size(450, 200);
			this.Load += new System.EventHandler(this.OptionDialog_MediaList2_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.recentInterval)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.TrackBar recentInterval;
		private System.Windows.Forms.Label recentIntervalLabel;


	}
}
