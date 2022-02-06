using OPOS.P1.Lib.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPOS.P1.WinForms
{
    public class TaskSettingsForm : Form
    {
        public event EventHandler<TaskSettingsSelectedEventArgs> TaskSettingsSelected;

        public class TaskSettingsSelectedEventArgs
        {
            public CustomTask Task { get; set; }
        }

        protected virtual void OnTaskSettingsSelected(TaskSettingsSelectedEventArgs e)
        {
            TaskSettingsSelected?.Invoke(this, e);
        }
    }
}
