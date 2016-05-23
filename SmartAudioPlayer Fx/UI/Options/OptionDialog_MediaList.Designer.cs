namespace SmartAudioPlayerFx.UI.Options
{
	partial class OptionDialog_MediaList
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.accept_exts = new System.Windows.Forms.DataGridView();
			this.exts_col1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.exts_col2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.ignore_words = new System.Windows.Forms.DataGridView();
			this.dataGridViewCheckBoxColumn1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.accept_exts)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ignore_words)).BeginInit();
			this.SuspendLayout();
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.accept_exts);
			this.groupBox2.Location = new System.Drawing.Point(3, 3);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(106, 187);
			this.groupBox2.TabIndex = 13;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "読み込む拡張子";
			// 
			// accept_exts
			// 
			this.accept_exts.AllowUserToResizeColumns = false;
			this.accept_exts.AllowUserToResizeRows = false;
			this.accept_exts.BackgroundColor = System.Drawing.SystemColors.Window;
			this.accept_exts.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.accept_exts.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.accept_exts.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.accept_exts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.accept_exts.ColumnHeadersVisible = false;
			this.accept_exts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.exts_col1,
            this.exts_col2});
			this.accept_exts.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystroke;
			this.accept_exts.Location = new System.Drawing.Point(6, 18);
			this.accept_exts.MultiSelect = false;
			this.accept_exts.Name = "accept_exts";
			this.accept_exts.RowHeadersVisible = false;
			this.accept_exts.RowTemplate.Height = 21;
			this.accept_exts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.accept_exts.Size = new System.Drawing.Size(94, 162);
			this.accept_exts.TabIndex = 0;
			this.accept_exts.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.accept_exts_CellValidating);
			// 
			// exts_col1
			// 
			this.exts_col1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.exts_col1.HeaderText = "bool";
			this.exts_col1.Name = "exts_col1";
			this.exts_col1.Width = 20;
			// 
			// exts_col2
			// 
			this.exts_col2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.exts_col2.HeaderText = "text";
			this.exts_col2.Name = "exts_col2";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.ignore_words);
			this.groupBox1.Location = new System.Drawing.Point(115, 3);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(329, 187);
			this.groupBox1.TabIndex = 14;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "無視ワード(&&フォルダ)";
			// 
			// ignore_words
			// 
			this.ignore_words.AllowUserToResizeColumns = false;
			this.ignore_words.AllowUserToResizeRows = false;
			this.ignore_words.BackgroundColor = System.Drawing.SystemColors.Window;
			this.ignore_words.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.ignore_words.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("MS UI Gothic", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.ignore_words.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
			this.ignore_words.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.ignore_words.ColumnHeadersVisible = false;
			this.ignore_words.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewCheckBoxColumn1,
            this.dataGridViewTextBoxColumn1});
			this.ignore_words.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystroke;
			this.ignore_words.Location = new System.Drawing.Point(6, 18);
			this.ignore_words.MultiSelect = false;
			this.ignore_words.Name = "ignore_words";
			this.ignore_words.RowHeadersVisible = false;
			this.ignore_words.RowTemplate.Height = 21;
			this.ignore_words.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.ignore_words.Size = new System.Drawing.Size(317, 162);
			this.ignore_words.TabIndex = 1;
			// 
			// dataGridViewCheckBoxColumn1
			// 
			this.dataGridViewCheckBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewCheckBoxColumn1.HeaderText = "bool";
			this.dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
			this.dataGridViewCheckBoxColumn1.Width = 20;
			// 
			// dataGridViewTextBoxColumn1
			// 
			this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.dataGridViewTextBoxColumn1.HeaderText = "text";
			this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
			// 
			// OptionDialog_MediaList
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.groupBox2);
			this.Name = "OptionDialog_MediaList";
			this.PageName = "メディアリスト";
			this.Size = new System.Drawing.Size(450, 200);
			this.Load += new System.EventHandler(this.OptionDialog_PlayList_Load);
			this.groupBox2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.accept_exts)).EndInit();
			this.groupBox1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.ignore_words)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.DataGridView accept_exts;
		private System.Windows.Forms.DataGridViewCheckBoxColumn exts_col1;
		private System.Windows.Forms.DataGridViewTextBoxColumn exts_col2;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.DataGridView ignore_words;
		private System.Windows.Forms.DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;
		private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;

	}
}
