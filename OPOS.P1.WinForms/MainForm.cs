using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using OPOS.P1.WinForms.Utility;
using OPOS.P1.Lib.Threading;
using OPOS.P1.Lib.Algo;
using static OPOS.P1.WinForms.TaskSettingsForm;
using static OPOS.P1.Lib.Threading.CustomScheduler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OPOS.P1.WinForms
{
    public partial class MainForm : Form
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        private CustomScheduler scheduler;

        private int idLabelWidth = 0;
        private ConcurrentDictionary<Guid, TaskControl> taskControls = new();
        private ConcurrentDictionary<Guid, CustomTask> overviewTasks = new();
        public EventHandler<CustomTaskSettingsEventArgs> TaskSettingsSelected;

        public class CustomTaskSettingsEventArgs
        {
            public CustomTaskSettings Settings { get; set; }
        }

        public MainForm()
        {
            AllocConsole();

            FileSystem.ConfigureInMemoryFileSystem(1);

            InitializeComponent();

            InitializeComponentDefaults();
            InitializeScheduler();
        }

        private void InitializeComponentDefaults()
        {
            maxCoresNumericUpDown.Value = 3;
            maxCoresNumericUpDown.Minimum = 0;
            maxCoresNumericUpDown.Maximum = Environment.ProcessorCount;

            maxConcurrencyNumericUpDown.Value = 3;
            maxConcurrencyNumericUpDown.Minimum = 0;

            currentCoresTextBox.Text = maxCoresNumericUpDown.Value.ToString();
            currentConcurrencyTextBox.Text = maxConcurrencyNumericUpDown.Value.ToString();

            var taskOverviewHeadingLayout = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
            };
            {
                var taskControls = taskOverviewHeadingLayout.Controls;

                var idLabel = new Label { Text = "Id" };
                idLabelWidth = idLabel.Width;
                var statusLabel = new Label { Text = "Status" };
                var priorityLabel = new Label { Text = "Priority" };
                var wantsToRunLabel = new Label { Text = "Wants To Run" };
                var progressLabel = new Label { Text = "Progress [%]" };
                var dateTimeDeadlineLabel = new Label { Text = "Date Time Deadline" };
                var millisDeadlineLabel = new Label { Text = "Max Run Time" };

                taskControls.Add(idLabel);
                taskControls.Add(statusLabel);
                taskControls.Add(priorityLabel);
                taskControls.Add(wantsToRunLabel);
                taskControls.Add(progressLabel);
                taskControls.Add(dateTimeDeadlineLabel);
                taskControls.Add(millisDeadlineLabel);
            }
            taskOverviewFlowLayoutPanel.Controls.Add(taskOverviewHeadingLayout);
        }

        private void InitializeScheduler()
        {
            scheduler = new CustomScheduler(
                new CustomSchedulerSettings
                {
                    MaxConcurrentTasks = (int)maxCoresNumericUpDown.Value,
                    MaxCores = (int)maxCoresNumericUpDown.Value,
                });

            SetSchedulerEventHandlers(scheduler);
        }

        private void SetSchedulerEventHandlers(CustomScheduler scheduler)
        {
            var taskStatusChangedHandler = Scheduler_TaskStatusChanged();
            scheduler.TaskStatusChanged += taskStatusChangedHandler;

            var taskProgressChangedHandler = Scheduler_TaskProgressChanged();
            scheduler.TaskProgressChanged += taskProgressChangedHandler;
        }

        private EventHandler<TaskProgressEventArgs> Scheduler_TaskProgressChanged()
        {
            var handler = new EventHandler<TaskProgressEventArgs>((object s, TaskProgressEventArgs e) =>
            {
                var taskControl = taskControls.GetValueOrDefault(e.Task.Id);
                if (taskControl is null)
                {
                    MessageBox.Show($"Error getting control of {e.Task.Id}", "Error Getting Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                taskControl.ProgressLabel.Invoke(() => taskControl.ProgressLabel.Text = e.Progress.ToString());
            });
            return handler;
        }

        private EventHandler<TaskStatusEventArgs> Scheduler_TaskStatusChanged()
        {
            var handler = new EventHandler<TaskStatusEventArgs>((object s, TaskStatusEventArgs e) =>
            {
                //MessageBox.Show($"{e.Task.Id} {e.Status} {e.WantsToRun}", "Status Updated", MessageBoxButtons.OK);
                var taskControl = taskControls.GetValueOrDefault(e.Task.Id);
                if (taskControl is null)
                {
                    MessageBox.Show($"Error getting control of {e.Task.Id}", "Error Getting Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                taskControl.TaskStatusLabel.Invoke(() => taskControl.TaskStatusLabel.Text = e.Status.ToString());
                taskControl.WantsToRunLabel.Invoke(() => taskControl.WantsToRunLabel.Text = e.WantsToRun.ToString());
            });
            return handler;
        }

        public class TaskControl
        {
            private FlowLayoutPanel control = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding { Left = 3, Bottom = 10 },
            };
            public TaskControl(CustomTask task)
            {
                InitializeComponent(task);
            }

            private void InitializeComponent(CustomTask task)
            {
                Layout.Name = task.Id.ToString();

                IdLabel = new Label { Text = task.Id.ToString() };
                TaskStatusLabel = new Label { Text = task.Status.ToString() };
                PriorityLabel = new Label { Text = task.Settings.Priority.ToString() };
                WantsToRunLabel = new Label { Text = task.WantsToRun.ToString() };
                ProgressLabel = new Label { Text = task.Progress.ToString() };
                DateTimeDeadlineLabel = new Label { Text = task.Settings.Deadline.ToString() };
                MillisDeadlineLabel = new Label { Text = task.Settings.MaxRunDuration.ToString() };

                StartButton = new Button { Text = "Start" };
                CancelButton = new Button { Text = "Cancel" };
                PauseButton = new Button { Text = "Pause" };
                ShowSerializedStateButton = new Button { Text = "JSON State" };
                ShowExceptionMessageButton = new Button { Text = "Exception" };

                StartButton.Click += TaskStartButton_Click(task);
                CancelButton.Click += TaskCancelButton_Click(task);
                PauseButton.Click += TaskPauseButton_Click(task);
                ShowSerializedStateButton.Click += TaskSerializedStateButton_Click(task);
                ShowExceptionMessageButton.Click += TaskShowExceptionMessageButton_Click(task);

                var layoutControls = Layout.Controls;

                layoutControls.Add(IdLabel);
                layoutControls.Add(TaskStatusLabel);
                layoutControls.Add(PriorityLabel);
                layoutControls.Add(WantsToRunLabel);
                layoutControls.Add(ProgressLabel);
                layoutControls.Add(DateTimeDeadlineLabel);
                layoutControls.Add(MillisDeadlineLabel);
                layoutControls.Add(StartButton);
                layoutControls.Add(CancelButton);
                layoutControls.Add(PauseButton);
                layoutControls.Add(ShowSerializedStateButton);
                layoutControls.Add(ShowExceptionMessageButton);
            }

            private static EventHandler TaskShowExceptionMessageButton_Click(CustomTask task)
            {
                return (s, e) =>
                {
                    if (task.Exception is null)
                        return;

                    var ex = task.Exception.InnerException;
                    string message = ex.Message;
                    var stackTrace = ex.StackTrace;

                    MessageBox.Show($"{message}{stackTrace}", "Exception Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
            }

            public FlowLayoutPanel Layout { get => control; }
            public Label IdLabel { get; private set; }
            public Label TaskStatusLabel { get; private set; }
            public Label PriorityLabel { get; private set; }
            public Label WantsToRunLabel { get; private set; }
            public Label ProgressLabel { get; private set; }
            public Label DateTimeDeadlineLabel { get; private set; }
            public Label MillisDeadlineLabel { get; private set; }

            public Button StartButton { get; private set; }
            public Button CancelButton { get; private set; }
            public Button PauseButton { get; private set; }
            public Button ShowSerializedStateButton { get; private set; }
            public Button ShowExceptionMessageButton { get; private set; }
        }

        private void CreateTaskButton_Click(object sender, EventArgs e)
        {
            var taskTypeSelectForm = new TaskTypeSelectForm();
            taskTypeSelectForm.TypeSelected += (s, e) =>
            {
                TaskSettingsForm taskSettingsForm = null;
                if (typeof(FftTask).Equals(e.TaskType))
                    taskSettingsForm = GetFftTaskSettingsForm();
                // Add dialogs for other types below
                else if (!typeof(CustomTask).IsAssignableFrom(e.TaskType))
                {
                    MessageBox.Show($"Unsupported type from type hierarchy of {typeof(CustomTask)}", "Type error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    MessageBox.Show($"Type not from type hierarchy of {typeof(CustomTask)}", "Type error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                taskSettingsForm.TaskSettingsSelected += TaskSettingsForm_TaskSettingsSelected;
                taskSettingsForm.ShowDialog();
            };
            taskTypeSelectForm.ShowDialog();
        }

        private void TaskSettingsForm_TaskSettingsSelected(object sender, TaskSettingsSelectedEventArgs e)
        {
            var task = e.Task;

            scheduler.PrepareTask(task);

            var taskControl = new TaskControl(e.Task);
            var taskLayout = taskControl.Layout;
            {
                taskControl.IdLabel.Width = idLabelWidth;
            }
            taskOverviewFlowLayoutPanel.Controls.Add(taskLayout);
            overviewTasks.TryAdd(task.Id, task);
            taskControls.TryAdd(task.Id, taskControl);
        }

        private static EventHandler TaskPauseButton_Click(CustomTask task)
        {
            return (s, e) =>
            {
                try
                {
                    task.Pause();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Pausing Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private static EventHandler TaskCancelButton_Click(CustomTask task)
        {
            return (s, e) =>
            {
                try
                {
                    task.Stop();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Cancelling Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private static EventHandler TaskStartButton_Click(CustomTask task)
        {
            return (s, e) =>
            {
                try
                {
                    task.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Starting Task", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private static EventHandler TaskSerializedStateButton_Click(CustomTask task)
        {
            return (s, e) =>
            {
                MessageBox.Show($"{task.Id}: {task.Serialize<FftTaskState>()}", "JSON Task State", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
        }

        private TaskSettingsForm GetFftTaskSettingsForm()
        {
            var fftSettingsForm = new FftTaskSettingsForm(scheduler.Settings);
            return fftSettingsForm;
        }

        private void CreateSchedulerButton_Click(object sender, EventArgs e)
        {
            if (scheduler is not null && scheduler.ActiveTasks != 0)
            {
                MessageBox.Show("There are still ongoing tasks.", "Scheduler Create Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int maxConcurrentTasks = (int)maxConcurrencyNumericUpDown.Value;
            int maxCores = (int)maxCoresNumericUpDown.Value;
            scheduler = new CustomScheduler(new CustomSchedulerSettings { MaxConcurrentTasks = maxConcurrentTasks, MaxCores = maxCores });

            SetSchedulerEventHandlers(scheduler);

            currentConcurrencyTextBox.Text = maxConcurrentTasks.ToString();
            currentCoresTextBox.Text = maxCores.ToString();
        }

        private void ClearFinishedTasksButton_Click(object sender, EventArgs e)
        {
            foreach (var item in taskControls)
            {
                var task = overviewTasks.GetValueOrDefault(item.Key);
                if (task?.Status is TaskStatus.RanToCompletion or TaskStatus.Faulted or TaskStatus.Canceled)
                {
                    taskOverviewFlowLayoutPanel.Controls.Remove(item.Value.Layout);
                    taskControls.Remove(task.Id, out _);
                    overviewTasks.Remove(task.Id, out _);
                }
            }
        }
    }
}
