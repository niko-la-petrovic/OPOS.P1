using OPOS.P1.Lib.Algo;
using OPOS.P1.Lib.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPOS.P1.WinForms
{
    public partial class FftTaskSettingsForm : TaskSettingsForm
    {
        private readonly CustomSchedulerSettings _settings;

        private static bool parallelize = false;
        private static bool priorityScheduling = true;
        private static int priority = 3;
        private static int maxCores = 3;

        public FftTaskSettingsForm(CustomSchedulerSettings settings)
        {
            _settings = settings;

            InitializeComponent();

            SetComponentDefaults();
        }

        private void SetComponentDefaults()
        {
            // TODO remove
            inputFilesListBox.Items.Add(@"C:\Users\Blue-Glass\source\repos\OPOS.P1\OPOS.P1.Lib.Test\input_64s_800hz_44100_mono_16b.wav");

            deadlineDateTimePicker.Value = DateTime.Now.AddMinutes(10);
            deadlineMillisNumericUpDown.Value = (decimal)TimeSpan.FromMinutes(10).TotalMilliseconds;

            priorityNumericUpDown.Enabled = prioritySchedulingCheckBox.Checked;
            prioritySchedulingCheckBox.CheckedChanged += PrioritySchedulingCheckBox_CheckedChanged;

            priorityNumericUpDown.Value = prioritySchedulingCheckBox.Checked ? _settings.MaxCores : 0;
            maxCoresNumericUpDown.Maximum = _settings.MaxCores;
            maxCoresNumericUpDown.Minimum = 0;

            maxCoresNumericUpDown.Enabled = parallelizeCheckBox.Checked;
            parallelizeCheckBox.CheckedChanged += ParallelizeCheckBox_CheckedChanged;

            // static
            parallelizeCheckBox.Checked = parallelize;
            prioritySchedulingCheckBox.Checked = priorityScheduling;
            priorityNumericUpDown.Value = priority;
            maxCoresNumericUpDown.Value = maxCores;
        }

        private void PrioritySchedulingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            priorityNumericUpDown.Enabled = prioritySchedulingCheckBox.Checked;
            priorityNumericUpDown.Value = prioritySchedulingCheckBox.Checked ? _settings.MaxCores : 0;
            // static
            priorityScheduling = prioritySchedulingCheckBox.Checked;
        }

        private void ParallelizeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            maxCoresNumericUpDown.Enabled = parallelizeCheckBox.Checked;
            // static
            parallelize = parallelizeCheckBox.Checked;
        }

        private void InputFilesAddButton_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Wav Files (*.wav)|*.wav|All files (*.*)|*.*";
            openFileDialog.Title = "Select input files";
            openFileDialog.Multiselect = true;
            openFileDialog.CheckFileExists = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    if (!inputFilesListBox.Items.Contains(fileName))
                        inputFilesListBox.Items.Add(fileName);
                }
            }
        }

        private void InputFilesRemoveButton_Click(object sender, EventArgs e)
        {
            if (inputFilesListBox.SelectedItems.Count <= 0)
                return;

            if (inputFilesListBox.SelectedItems.Count == 0)
                return;

            var selectedItems = inputFilesListBox.SelectedItems.OfType<object>().ToList();
            foreach (var selectedItem in selectedItems)
            {
                inputFilesListBox.Items.Remove(selectedItem);
            }
        }

        private void InputFilesClearButton_Click(object sender, EventArgs e)
        {
            inputFilesListBox.Items.Clear();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (inputFilesListBox.Items.Count == 0)
            {
                MessageBox.Show("No input files selected.", "File Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var settings = new CustomTaskSettings
            {
                Deadline = deadlineDateTimePicker.Value,
                MaxRunDuration = TimeSpan.FromMilliseconds((double)deadlineMillisNumericUpDown.Value),
                MaxCores = (int)maxCoresNumericUpDown.Value,
                Parallelize = parallelizeCheckBox.Checked,
                Priority = (int)priorityNumericUpDown.Value,
            };

            var resourceFiles = inputFilesListBox.Items.OfType<object>()
                .Select(i => new CustomResourceFile((string)i))
                .ToArray();

            var files = ImmutableList.Create(resourceFiles);

            var task = new FftTask(settings, files);
            var evt = new TaskSettingsSelectedEventArgs { Task = task };
            OnTaskSettingsSelected(evt);

            Close();
        }

        private void PriorityNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            // static
            priority = (int)priorityNumericUpDown.Value;
        }

        private void MaxCoresNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            // static
            maxCores = (int)maxCoresNumericUpDown.Value;
        }
    }
}
