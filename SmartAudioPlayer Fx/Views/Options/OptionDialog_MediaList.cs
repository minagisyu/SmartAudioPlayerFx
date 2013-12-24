namespace SmartAudioPlayerFx.Views.Options
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows.Forms;
	using SmartAudioPlayerFx.Data;
	using SmartAudioPlayerFx.Managers;
	using __Primitives__;

	sealed partial class OptionDialog_MediaList : OptionPage
	{
		public OptionDialog_MediaList()
		{
			InitializeComponent();
		}

		void OptionDialog_PlayList_Load(object sender, EventArgs e)
		{
			ManagerServices.MediaItemFilterManager.AcceptExtensions
				.OrderBy(i => i.Extension, StringComparer.CurrentCultureIgnoreCase)
				.Select(i => new object[] { i.IsEnable, i.Extension })
				.ForEach(i => accept_exts.Rows.Add(i));
			ManagerServices.MediaItemFilterManager.IgnoreWords
				.OrderBy(i => i.Word, StringComparer.CurrentCultureIgnoreCase)
				.Select(i => new object[] { i.IsEnable, i.Word})
				.ForEach(i => ignore_words.Rows.Add(i));
		}

		public override void Save()
		{
			var exts = GetDataGridViewItems(accept_exts)
				.Where(i => !string.IsNullOrWhiteSpace(i.Value))
				.Where(i => !ValueCheck_PeriodStart(i.Value))
				.Where(i => !ValueCheck_HasValue(i.Value))
				.Select(i => new MediaItemFilterManager.AcceptExtension(i.Key, i.Value))
				.ToArray();
			ManagerServices.MediaItemFilterManager.SetAcceptExtensions(exts);
			var words = GetDataGridViewItems(ignore_words)
				.Where(i => !string.IsNullOrWhiteSpace(i.Value))
				.Where(i => !ValueCheck_HasValue(i.Value))
				.Select(i => new MediaItemFilterManager.IgnoreWord(i.Key, i.Value))
				.ToArray();
			ManagerServices.MediaItemFilterManager.SetIgnoreWords(words);
		}

		IEnumerable<KeyValuePair<bool, string>> GetDataGridViewItems(DataGridView grid)
		{
			return grid.Rows
				.OfType<DataGridViewRow>()
				.Select(i => new KeyValuePair<bool, string>(
					(bool)(i.Cells[0].Value ?? false),
					(string)i.Cells[1].Value))
				.Where(i => !string.IsNullOrWhiteSpace(i.Value));
		}

		bool ValueCheck_PeriodStart(string text)
		{
			return text[0] != '.';
		}
		bool ValueCheck_HasValue(string text)
		{
			var count = GetDataGridViewItems(accept_exts)
				.Where(i => string.Equals(text, i.Value, StringComparison.CurrentCultureIgnoreCase))
				.Count();
			return count > 1;
		}
		void accept_exts_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
		{
			if (e.ColumnIndex == 1)
			{
				var text = e.FormattedValue as string;

				// 空文字は無視(保存時に除外)
				if (string.IsNullOrEmpty(text)) return;

				accept_exts[e.ColumnIndex, e.RowIndex].ErrorText =
					ValueCheck_PeriodStart(text) ? "拡張子はピリオドから始めてください" :
					ValueCheck_HasValue(text) ? "この拡張子は既に存在します" :
					null;
			}
		}

		bool ValueCheck_InvalidPathChar(string text)
		{
			return Path.GetInvalidPathChars()
				.Any(i => !text.Contains(i));
		}

	}
}
