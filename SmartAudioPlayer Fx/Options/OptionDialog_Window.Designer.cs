namespace SmartAudioPlayerFx.Options
{
	partial class OptionDialog_Window
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
			this.activeOpacityLabel = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.active_preview = new SmartAudioPlayerFx.Options.DoubleBufferedControl();
			this.activeOpacity = new System.Windows.Forms.TrackBar();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.deactive_preview = new SmartAudioPlayerFx.Options.DoubleBufferedControl();
			this.panel2 = new System.Windows.Forms.Panel();
			this.deactiveOpacity = new System.Windows.Forms.TrackBar();
			this.deactiveOpacityLabel = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.activeOpacity)).BeginInit();
			this.groupBox2.SuspendLayout();
			this.panel2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.deactiveOpacity)).BeginInit();
			this.SuspendLayout();
			// 
			// activeOpacityLabel
			// 
			this.activeOpacityLabel.AutoSize = true;
			this.activeOpacityLabel.Location = new System.Drawing.Point(6, 23);
			this.activeOpacityLabel.Name = "activeOpacityLabel";
			this.activeOpacityLabel.Size = new System.Drawing.Size(65, 12);
			this.activeOpacityLabel.TabIndex = 1;
			this.activeOpacityLabel.Text = "透明度xx％";
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.BackColor = System.Drawing.Color.Transparent;
			this.groupBox1.Controls.Add(this.panel1);
			this.groupBox1.Controls.Add(this.activeOpacityLabel);
			this.groupBox1.Location = new System.Drawing.Point(0, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(444, 43);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "マウスカーソルが近づいたときの透明度";
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.active_preview);
			this.panel1.Controls.Add(this.activeOpacity);
			this.panel1.Location = new System.Drawing.Point(77, 12);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(361, 30);
			this.panel1.TabIndex = 5;
			// 
			// active_preview
			// 
			this.active_preview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
			this.active_preview.Location = new System.Drawing.Point(336, 1);
			this.active_preview.Name = "active_preview";
			this.active_preview.Size = new System.Drawing.Size(25, 25);
			this.active_preview.TabIndex = 2;
			// 
			// activeOpacity
			// 
			this.activeOpacity.Dock = System.Windows.Forms.DockStyle.Left;
			this.activeOpacity.Location = new System.Drawing.Point(0, 0);
			this.activeOpacity.Maximum = 95;
			this.activeOpacity.Minimum = 10;
			this.activeOpacity.Name = "activeOpacity";
			this.activeOpacity.Size = new System.Drawing.Size(334, 30);
			this.activeOpacity.TabIndex = 1;
			this.activeOpacity.TickStyle = System.Windows.Forms.TickStyle.None;
			this.activeOpacity.Value = 10;
			this.activeOpacity.ValueChanged += new System.EventHandler(this.activeOpacity_ValueChanged);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.BackColor = System.Drawing.Color.Transparent;
			this.groupBox2.Controls.Add(this.deactive_preview);
			this.groupBox2.Controls.Add(this.panel2);
			this.groupBox2.Controls.Add(this.deactiveOpacityLabel);
			this.groupBox2.Location = new System.Drawing.Point(0, 49);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(444, 43);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "マウスカーソルが離れたときの透明度";
			// 
			// deactive_preview
			// 
			this.deactive_preview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
			this.deactive_preview.Location = new System.Drawing.Point(412, 13);
			this.deactive_preview.Name = "deactive_preview";
			this.deactive_preview.Size = new System.Drawing.Size(25, 25);
			this.deactive_preview.TabIndex = 3;
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.deactiveOpacity);
			this.panel2.Location = new System.Drawing.Point(77, 13);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(361, 30);
			this.panel2.TabIndex = 5;
			// 
			// deactiveOpacity
			// 
			this.deactiveOpacity.Dock = System.Windows.Forms.DockStyle.Left;
			this.deactiveOpacity.Location = new System.Drawing.Point(0, 0);
			this.deactiveOpacity.Maximum = 95;
			this.deactiveOpacity.Minimum = 10;
			this.deactiveOpacity.Name = "deactiveOpacity";
			this.deactiveOpacity.Size = new System.Drawing.Size(334, 30);
			this.deactiveOpacity.TabIndex = 1;
			this.deactiveOpacity.TickStyle = System.Windows.Forms.TickStyle.None;
			this.deactiveOpacity.Value = 10;
			this.deactiveOpacity.ValueChanged += new System.EventHandler(this.deactiveOpacity_ValueChanged);
			// 
			// deactiveOpacityLabel
			// 
			this.deactiveOpacityLabel.AutoSize = true;
			this.deactiveOpacityLabel.Location = new System.Drawing.Point(6, 23);
			this.deactiveOpacityLabel.Name = "deactiveOpacityLabel";
			this.deactiveOpacityLabel.Size = new System.Drawing.Size(65, 12);
			this.deactiveOpacityLabel.TabIndex = 1;
			this.deactiveOpacityLabel.Text = "透明度xx％";
			// 
			// OptionDialog_Window
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.DoubleBuffered = true;
			this.Name = "OptionDialog_Window";
			this.PageName = "ウィンドウ";
			this.Size = new System.Drawing.Size(450, 155);
			this.Load += new System.EventHandler(this.OptionDialog_Window_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.activeOpacity)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.deactiveOpacity)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label activeOpacityLabel;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TrackBar activeOpacity;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label deactiveOpacityLabel;
		private System.Windows.Forms.TrackBar deactiveOpacity;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private DoubleBufferedControl active_preview;
		private DoubleBufferedControl deactive_preview;

	}
}
