using System.Windows.Forms;

namespace Quala.Win32.Dialog
{
	public partial class MessageDialog : Form
	{
		public MessageDialog()
		{
			InitializeComponent();
		}

		public string Title
		{
			get { return this.Text; }
			set { this.Text = value; }
		}

		public string HeaderMessage
		{
			get { return label1.Text; }
			set { label1.Text = value; }
		}

		public string DescriptionMessage
		{
			get { return textBox1.Text; }
			set { textBox1.Text = value; }
		}

	}
}
