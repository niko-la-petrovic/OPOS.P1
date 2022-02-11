
namespace OPOS.P1.WinForms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.createTaskButton = new System.Windows.Forms.Button();
            this.maxCoresLabel = new System.Windows.Forms.Label();
            this.maxConcurrencyLabel = new System.Windows.Forms.Label();
            this.maxCoresNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.maxConcurrencyNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.createSchedulerButton = new System.Windows.Forms.Button();
            this.schedulerSettingsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.taskOverviewFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.currentCoreCountLabel = new System.Windows.Forms.Label();
            this.curentConcurrencyLabel = new System.Windows.Forms.Label();
            this.currentCoresTextBox = new System.Windows.Forms.TextBox();
            this.currentConcurrencyTextBox = new System.Windows.Forms.TextBox();
            this.clearFinishedTasksButton = new System.Windows.Forms.Button();
            this.taskControlsGroupBox = new System.Windows.Forms.GroupBox();
            this.openSavesButton = new System.Windows.Forms.Button();
            this.saveStateButton = new System.Windows.Forms.Button();
            this.restoreStateButton = new System.Windows.Forms.Button();
            this.startUnstartedTasksButton = new System.Windows.Forms.Button();
            this.schedulerControlsGroupBox = new System.Windows.Forms.GroupBox();
            this.currentInfoGroupBox = new System.Windows.Forms.GroupBox();
            this.unterminatedTasksTextBox = new System.Windows.Forms.TextBox();
            this.unterminatedTasksLabel = new System.Windows.Forms.Label();
            this.taskOverviewGroupBox = new System.Windows.Forms.GroupBox();
            this.autosaveCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.maxCoresNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxConcurrencyNumericUpDown)).BeginInit();
            this.schedulerSettingsPanel.SuspendLayout();
            this.taskControlsGroupBox.SuspendLayout();
            this.schedulerControlsGroupBox.SuspendLayout();
            this.currentInfoGroupBox.SuspendLayout();
            this.taskOverviewGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // createTaskButton
            // 
            this.createTaskButton.Location = new System.Drawing.Point(9, 22);
            this.createTaskButton.Name = "createTaskButton";
            this.createTaskButton.Size = new System.Drawing.Size(75, 23);
            this.createTaskButton.TabIndex = 0;
            this.createTaskButton.Text = "Create Task";
            this.createTaskButton.UseVisualStyleBackColor = true;
            this.createTaskButton.Click += new System.EventHandler(this.CreateTaskButton_Click);
            // 
            // maxCoresLabel
            // 
            this.maxCoresLabel.AutoSize = true;
            this.maxCoresLabel.Location = new System.Drawing.Point(146, 0);
            this.maxCoresLabel.Name = "maxCoresLabel";
            this.maxCoresLabel.Size = new System.Drawing.Size(63, 15);
            this.maxCoresLabel.TabIndex = 1;
            this.maxCoresLabel.Text = "Max Cores";
            // 
            // maxConcurrencyLabel
            // 
            this.maxConcurrencyLabel.AutoSize = true;
            this.maxConcurrencyLabel.Location = new System.Drawing.Point(272, 0);
            this.maxConcurrencyLabel.Name = "maxConcurrencyLabel";
            this.maxConcurrencyLabel.Size = new System.Drawing.Size(126, 15);
            this.maxConcurrencyLabel.TabIndex = 2;
            this.maxConcurrencyLabel.Text = "Max Task Concurrency";
            // 
            // maxCoresNumericUpDown
            // 
            this.maxCoresNumericUpDown.Location = new System.Drawing.Point(215, 3);
            this.maxCoresNumericUpDown.Name = "maxCoresNumericUpDown";
            this.maxCoresNumericUpDown.Size = new System.Drawing.Size(51, 23);
            this.maxCoresNumericUpDown.TabIndex = 3;
            // 
            // maxConcurrencyNumericUpDown
            // 
            this.maxConcurrencyNumericUpDown.Location = new System.Drawing.Point(404, 3);
            this.maxConcurrencyNumericUpDown.Name = "maxConcurrencyNumericUpDown";
            this.maxConcurrencyNumericUpDown.Size = new System.Drawing.Size(63, 23);
            this.maxConcurrencyNumericUpDown.TabIndex = 4;
            // 
            // createSchedulerButton
            // 
            this.createSchedulerButton.Location = new System.Drawing.Point(3, 3);
            this.createSchedulerButton.Name = "createSchedulerButton";
            this.createSchedulerButton.Size = new System.Drawing.Size(137, 23);
            this.createSchedulerButton.TabIndex = 5;
            this.createSchedulerButton.Text = "Create New Scheduler";
            this.createSchedulerButton.UseVisualStyleBackColor = true;
            this.createSchedulerButton.Click += new System.EventHandler(this.CreateSchedulerButton_Click);
            // 
            // schedulerSettingsPanel
            // 
            this.schedulerSettingsPanel.Controls.Add(this.createSchedulerButton);
            this.schedulerSettingsPanel.Controls.Add(this.maxCoresLabel);
            this.schedulerSettingsPanel.Controls.Add(this.maxCoresNumericUpDown);
            this.schedulerSettingsPanel.Controls.Add(this.maxConcurrencyLabel);
            this.schedulerSettingsPanel.Controls.Add(this.maxConcurrencyNumericUpDown);
            this.schedulerSettingsPanel.Location = new System.Drawing.Point(6, 22);
            this.schedulerSettingsPanel.Name = "schedulerSettingsPanel";
            this.schedulerSettingsPanel.Size = new System.Drawing.Size(486, 32);
            this.schedulerSettingsPanel.TabIndex = 6;
            // 
            // taskOverviewFlowLayoutPanel
            // 
            this.taskOverviewFlowLayoutPanel.AutoScroll = true;
            this.taskOverviewFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.taskOverviewFlowLayoutPanel.Location = new System.Drawing.Point(9, 22);
            this.taskOverviewFlowLayoutPanel.Name = "taskOverviewFlowLayoutPanel";
            this.taskOverviewFlowLayoutPanel.Size = new System.Drawing.Size(1173, 322);
            this.taskOverviewFlowLayoutPanel.TabIndex = 7;
            this.taskOverviewFlowLayoutPanel.WrapContents = false;
            // 
            // currentCoreCountLabel
            // 
            this.currentCoreCountLabel.AutoSize = true;
            this.currentCoreCountLabel.Location = new System.Drawing.Point(6, 19);
            this.currentCoreCountLabel.Name = "currentCoreCountLabel";
            this.currentCoreCountLabel.Size = new System.Drawing.Size(37, 15);
            this.currentCoreCountLabel.TabIndex = 6;
            this.currentCoreCountLabel.Text = "Cores";
            // 
            // curentConcurrencyLabel
            // 
            this.curentConcurrencyLabel.AutoSize = true;
            this.curentConcurrencyLabel.Location = new System.Drawing.Point(86, 19);
            this.curentConcurrencyLabel.Name = "curentConcurrencyLabel";
            this.curentConcurrencyLabel.Size = new System.Drawing.Size(100, 15);
            this.curentConcurrencyLabel.TabIndex = 7;
            this.curentConcurrencyLabel.Text = "Task Concurrency";
            // 
            // currentCoresTextBox
            // 
            this.currentCoresTextBox.Location = new System.Drawing.Point(49, 22);
            this.currentCoresTextBox.Name = "currentCoresTextBox";
            this.currentCoresTextBox.ReadOnly = true;
            this.currentCoresTextBox.Size = new System.Drawing.Size(31, 23);
            this.currentCoresTextBox.TabIndex = 8;
            this.currentCoresTextBox.Text = "3";
            // 
            // currentConcurrencyTextBox
            // 
            this.currentConcurrencyTextBox.Location = new System.Drawing.Point(192, 22);
            this.currentConcurrencyTextBox.Name = "currentConcurrencyTextBox";
            this.currentConcurrencyTextBox.ReadOnly = true;
            this.currentConcurrencyTextBox.Size = new System.Drawing.Size(56, 23);
            this.currentConcurrencyTextBox.TabIndex = 9;
            this.currentConcurrencyTextBox.Text = "3";
            // 
            // clearFinishedTasksButton
            // 
            this.clearFinishedTasksButton.Location = new System.Drawing.Point(90, 22);
            this.clearFinishedTasksButton.Name = "clearFinishedTasksButton";
            this.clearFinishedTasksButton.Size = new System.Drawing.Size(125, 23);
            this.clearFinishedTasksButton.TabIndex = 10;
            this.clearFinishedTasksButton.Text = "Clear Finished Tasks";
            this.clearFinishedTasksButton.UseVisualStyleBackColor = true;
            this.clearFinishedTasksButton.Click += new System.EventHandler(this.ClearFinishedTasksButton_Click);
            // 
            // taskControlsGroupBox
            // 
            this.taskControlsGroupBox.Controls.Add(this.autosaveCheckBox);
            this.taskControlsGroupBox.Controls.Add(this.openSavesButton);
            this.taskControlsGroupBox.Controls.Add(this.saveStateButton);
            this.taskControlsGroupBox.Controls.Add(this.restoreStateButton);
            this.taskControlsGroupBox.Controls.Add(this.startUnstartedTasksButton);
            this.taskControlsGroupBox.Controls.Add(this.createTaskButton);
            this.taskControlsGroupBox.Controls.Add(this.clearFinishedTasksButton);
            this.taskControlsGroupBox.Location = new System.Drawing.Point(12, 83);
            this.taskControlsGroupBox.Name = "taskControlsGroupBox";
            this.taskControlsGroupBox.Size = new System.Drawing.Size(754, 59);
            this.taskControlsGroupBox.TabIndex = 11;
            this.taskControlsGroupBox.TabStop = false;
            this.taskControlsGroupBox.Text = "Task Controls";
            // 
            // openSavesButton
            // 
            this.openSavesButton.Location = new System.Drawing.Point(537, 22);
            this.openSavesButton.Name = "openSavesButton";
            this.openSavesButton.Size = new System.Drawing.Size(92, 23);
            this.openSavesButton.TabIndex = 14;
            this.openSavesButton.Text = "Open Saves";
            this.openSavesButton.UseVisualStyleBackColor = true;
            this.openSavesButton.Click += new System.EventHandler(this.OpenSavesButton_Click);
            // 
            // saveStateButton
            // 
            this.saveStateButton.Location = new System.Drawing.Point(456, 22);
            this.saveStateButton.Name = "saveStateButton";
            this.saveStateButton.Size = new System.Drawing.Size(75, 23);
            this.saveStateButton.TabIndex = 13;
            this.saveStateButton.Text = "Save State";
            this.saveStateButton.UseVisualStyleBackColor = true;
            this.saveStateButton.Click += new System.EventHandler(this.SaveStateButton_Click);
            // 
            // restoreStateButton
            // 
            this.restoreStateButton.Location = new System.Drawing.Point(355, 22);
            this.restoreStateButton.Name = "restoreStateButton";
            this.restoreStateButton.Size = new System.Drawing.Size(95, 23);
            this.restoreStateButton.TabIndex = 12;
            this.restoreStateButton.Text = "Restore State";
            this.restoreStateButton.UseVisualStyleBackColor = true;
            this.restoreStateButton.Click += new System.EventHandler(this.RestoreStateButton_Click);
            // 
            // startUnstartedTasksButton
            // 
            this.startUnstartedTasksButton.Location = new System.Drawing.Point(221, 22);
            this.startUnstartedTasksButton.Name = "startUnstartedTasksButton";
            this.startUnstartedTasksButton.Size = new System.Drawing.Size(128, 23);
            this.startUnstartedTasksButton.TabIndex = 11;
            this.startUnstartedTasksButton.Text = "Start Unstarted Tasks";
            this.startUnstartedTasksButton.UseVisualStyleBackColor = true;
            this.startUnstartedTasksButton.Click += new System.EventHandler(this.StartUnstartedTasksButton_Click);
            // 
            // schedulerControlsGroupBox
            // 
            this.schedulerControlsGroupBox.Controls.Add(this.schedulerSettingsPanel);
            this.schedulerControlsGroupBox.Location = new System.Drawing.Point(12, 12);
            this.schedulerControlsGroupBox.Name = "schedulerControlsGroupBox";
            this.schedulerControlsGroupBox.Size = new System.Drawing.Size(500, 65);
            this.schedulerControlsGroupBox.TabIndex = 12;
            this.schedulerControlsGroupBox.TabStop = false;
            this.schedulerControlsGroupBox.Text = "Scheduler Controls";
            // 
            // currentInfoGroupBox
            // 
            this.currentInfoGroupBox.Controls.Add(this.unterminatedTasksTextBox);
            this.currentInfoGroupBox.Controls.Add(this.unterminatedTasksLabel);
            this.currentInfoGroupBox.Controls.Add(this.currentCoreCountLabel);
            this.currentInfoGroupBox.Controls.Add(this.curentConcurrencyLabel);
            this.currentInfoGroupBox.Controls.Add(this.currentCoresTextBox);
            this.currentInfoGroupBox.Controls.Add(this.currentConcurrencyTextBox);
            this.currentInfoGroupBox.Location = new System.Drawing.Point(518, 12);
            this.currentInfoGroupBox.Name = "currentInfoGroupBox";
            this.currentInfoGroupBox.Size = new System.Drawing.Size(440, 65);
            this.currentInfoGroupBox.TabIndex = 13;
            this.currentInfoGroupBox.TabStop = false;
            this.currentInfoGroupBox.Text = "Current Info";
            // 
            // unterminatedTasksTextBox
            // 
            this.unterminatedTasksTextBox.Location = new System.Drawing.Point(370, 19);
            this.unterminatedTasksTextBox.Name = "unterminatedTasksTextBox";
            this.unterminatedTasksTextBox.ReadOnly = true;
            this.unterminatedTasksTextBox.Size = new System.Drawing.Size(56, 23);
            this.unterminatedTasksTextBox.TabIndex = 11;
            this.unterminatedTasksTextBox.Text = "0";
            // 
            // unterminatedTasksLabel
            // 
            this.unterminatedTasksLabel.AutoSize = true;
            this.unterminatedTasksLabel.Location = new System.Drawing.Point(254, 19);
            this.unterminatedTasksLabel.Name = "unterminatedTasksLabel";
            this.unterminatedTasksLabel.Size = new System.Drawing.Size(110, 15);
            this.unterminatedTasksLabel.TabIndex = 10;
            this.unterminatedTasksLabel.Text = "Unterminated Tasks";
            // 
            // taskOverviewGroupBox
            // 
            this.taskOverviewGroupBox.Controls.Add(this.taskOverviewFlowLayoutPanel);
            this.taskOverviewGroupBox.Location = new System.Drawing.Point(12, 148);
            this.taskOverviewGroupBox.Name = "taskOverviewGroupBox";
            this.taskOverviewGroupBox.Size = new System.Drawing.Size(1188, 360);
            this.taskOverviewGroupBox.TabIndex = 14;
            this.taskOverviewGroupBox.TabStop = false;
            this.taskOverviewGroupBox.Text = "Task Overview";
            // 
            // autosaveCheckBox
            // 
            this.autosaveCheckBox.AutoSize = true;
            this.autosaveCheckBox.Location = new System.Drawing.Point(635, 25);
            this.autosaveCheckBox.Name = "autosaveCheckBox";
            this.autosaveCheckBox.Size = new System.Drawing.Size(79, 19);
            this.autosaveCheckBox.TabIndex = 15;
            this.autosaveCheckBox.Text = "Auto Save";
            this.autosaveCheckBox.UseVisualStyleBackColor = true;
            this.autosaveCheckBox.CheckedChanged += new System.EventHandler(this.AutosaveCheckBox_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1212, 530);
            this.Controls.Add(this.taskOverviewGroupBox);
            this.Controls.Add(this.currentInfoGroupBox);
            this.Controls.Add(this.schedulerControlsGroupBox);
            this.Controls.Add(this.taskControlsGroupBox);
            this.Name = "MainForm";
            this.Text = "Main Form";
            ((System.ComponentModel.ISupportInitialize)(this.maxCoresNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxConcurrencyNumericUpDown)).EndInit();
            this.schedulerSettingsPanel.ResumeLayout(false);
            this.schedulerSettingsPanel.PerformLayout();
            this.taskControlsGroupBox.ResumeLayout(false);
            this.taskControlsGroupBox.PerformLayout();
            this.schedulerControlsGroupBox.ResumeLayout(false);
            this.currentInfoGroupBox.ResumeLayout(false);
            this.currentInfoGroupBox.PerformLayout();
            this.taskOverviewGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button createTaskButton;
        private System.Windows.Forms.Label maxCoresLabel;
        private System.Windows.Forms.Label maxConcurrencyLabel;
        private System.Windows.Forms.NumericUpDown maxCoresNumericUpDown;
        private System.Windows.Forms.NumericUpDown maxConcurrencyNumericUpDown;
        private System.Windows.Forms.Button createSchedulerButton;
        private System.Windows.Forms.FlowLayoutPanel schedulerSettingsPanel;
        private System.Windows.Forms.FlowLayoutPanel taskOverviewFlowLayoutPanel;
        private System.Windows.Forms.Label currentCoreCountLabel;
        private System.Windows.Forms.Label curentConcurrencyLabel;
        private System.Windows.Forms.TextBox currentCoresTextBox;
        private System.Windows.Forms.TextBox currentConcurrencyTextBox;
        private System.Windows.Forms.Button clearFinishedTasksButton;
        private System.Windows.Forms.GroupBox taskControlsGroupBox;
        private System.Windows.Forms.GroupBox schedulerControlsGroupBox;
        private System.Windows.Forms.GroupBox currentInfoGroupBox;
        private System.Windows.Forms.GroupBox taskOverviewGroupBox;
        private System.Windows.Forms.Button startUnstartedTasksButton;
        private System.Windows.Forms.Label unterminatedTasksLabel;
        private System.Windows.Forms.TextBox unterminatedTasksTextBox;
        private System.Windows.Forms.Button saveStateButton;
        private System.Windows.Forms.Button restoreStateButton;
        private System.Windows.Forms.Button openSavesButton;
        private System.Windows.Forms.CheckBox autosaveCheckBox;
    }
}

