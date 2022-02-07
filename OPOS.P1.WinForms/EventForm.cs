using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPOS.P1.WinForms
{
    public partial class EventForm : Form
    {
        public EventForm()
        {
            InitializeComponent();
        }

        public void WriteEventLog(string line)
        {
            eventLogRichTextBox.Invoke(() => eventLogRichTextBox.Text += $"{line}{Environment.NewLine}");
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            eventLogRichTextBox.Text = string.Empty;
        }
    }
}
