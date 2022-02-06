using OPOS.P1.Lib.Algo;
using OPOS.P1.Lib.Threading;
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
    public partial class TaskTypeSelectForm : Form
    {
        private readonly List<Type> taskTypes = new();

        public class TaskTypeEventArgs : EventArgs
        {
            private Type taskType;

            public Type TaskType { get => taskType; set => taskType = value ?? throw new NullReferenceException(nameof(value)); }
        }

        public event EventHandler<TaskTypeEventArgs> TypeSelected;

        public TaskTypeSelectForm()
        {
            InitializeComponent();

            InitializeTaskTypes();

            taskTypeComboBox.SelectedIndex = 0;
        }

        private void InitializeTaskTypes()
        {
            taskTypes.Add(typeof(FftTask));

            foreach (var item in taskTypes)
            {
                taskTypeComboBox.Items.Add(item);
            }
        }

        protected virtual void OnTypeSelected(object sender, TaskTypeEventArgs e)
        {
            TypeSelected?.Invoke(this, e);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (taskTypeComboBox.SelectedIndex == -1 || taskTypeComboBox.SelectedItem is null)
            {
                MessageBox.Show("No type selected.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OnTypeSelected(
                sender,
                new TaskTypeEventArgs { TaskType = taskTypeComboBox.SelectedItem as Type });

            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void taskTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
