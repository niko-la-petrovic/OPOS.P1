namespace OPOS.P1.WinForms
{
    partial class FftTaskSettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.prioritySchedulingCheckBox = new System.Windows.Forms.CheckBox();
            this.priorityLabel = new System.Windows.Forms.Label();
            this.priorityNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.deadlineDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.deadlineDateTimelabel = new System.Windows.Forms.Label();
            this.deadlineMillisNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.deadlineMillisLabel = new System.Windows.Forms.Label();
            this.maxCoresLabel = new System.Windows.Forms.Label();
            this.maxCoresNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.parallelizeCheckBox = new System.Windows.Forms.CheckBox();
            this.inputFilesListBox = new System.Windows.Forms.ListBox();
            this.inputFilesLabel = new System.Windows.Forms.Label();
            this.inputFilesAddButton = new System.Windows.Forms.Button();
            this.inputFilesRemoveButton = new System.Windows.Forms.Button();
            this.inputFilesClearButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.priorityNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.deadlineMillisNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxCoresNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // prioritySchedulingCheckBox
            // 
            this.prioritySchedulingCheckBox.AutoSize = true;
            this.prioritySchedulingCheckBox.Checked = true;
            this.prioritySchedulingCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.prioritySchedulingCheckBox.Location = new System.Drawing.Point(12, 12);
            this.prioritySchedulingCheckBox.Name = "prioritySchedulingCheckBox";
            this.prioritySchedulingCheckBox.Size = new System.Drawing.Size(126, 19);
            this.prioritySchedulingCheckBox.TabIndex = 0;
            this.prioritySchedulingCheckBox.Text = "Priority Scheduling";
            this.prioritySchedulingCheckBox.UseVisualStyleBackColor = true;
            // 
            // priorityLabel
            // 
            this.priorityLabel.AutoSize = true;
            this.priorityLabel.Location = new System.Drawing.Point(12, 38);
            this.priorityLabel.Name = "priorityLabel";
            this.priorityLabel.Size = new System.Drawing.Size(45, 15);
            this.priorityLabel.TabIndex = 2;
            this.priorityLabel.Text = "Priority";
            // 
            // priorityNumericUpDown
            // 
            this.priorityNumericUpDown.Location = new System.Drawing.Point(12, 60);
            this.priorityNumericUpDown.Name = "priorityNumericUpDown";
            this.priorityNumericUpDown.Size = new System.Drawing.Size(120, 23);
            this.priorityNumericUpDown.TabIndex = 3;
            this.priorityNumericUpDown.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.priorityNumericUpDown.ValueChanged += new System.EventHandler(this.PriorityNumericUpDown_ValueChanged);
            // 
            // deadlineDateTimePicker
            // 
            this.deadlineDateTimePicker.CustomFormat = "HH:mm:ss dd.MM.yyyy|";
            this.deadlineDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.deadlineDateTimePicker.Location = new System.Drawing.Point(12, 112);
            this.deadlineDateTimePicker.Name = "deadlineDateTimePicker";
            this.deadlineDateTimePicker.Size = new System.Drawing.Size(200, 23);
            this.deadlineDateTimePicker.TabIndex = 4;
            // 
            // deadlineDateTimelabel
            // 
            this.deadlineDateTimelabel.AutoSize = true;
            this.deadlineDateTimelabel.Location = new System.Drawing.Point(12, 94);
            this.deadlineDateTimelabel.Name = "deadlineDateTimelabel";
            this.deadlineDateTimelabel.Size = new System.Drawing.Size(109, 15);
            this.deadlineDateTimelabel.TabIndex = 5;
            this.deadlineDateTimelabel.Text = "Deadline Date Time";
            // 
            // deadlineMillisNumericUpDown
            // 
            this.deadlineMillisNumericUpDown.Location = new System.Drawing.Point(12, 164);
            this.deadlineMillisNumericUpDown.Maximum = new decimal(new int[] {
            9000000,
            0,
            0,
            0});
            this.deadlineMillisNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.deadlineMillisNumericUpDown.Name = "deadlineMillisNumericUpDown";
            this.deadlineMillisNumericUpDown.Size = new System.Drawing.Size(120, 23);
            this.deadlineMillisNumericUpDown.TabIndex = 6;
            this.deadlineMillisNumericUpDown.Value = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            // 
            // deadlineMillisLabel
            // 
            this.deadlineMillisLabel.AutoSize = true;
            this.deadlineMillisLabel.Location = new System.Drawing.Point(10, 142);
            this.deadlineMillisLabel.Name = "deadlineMillisLabel";
            this.deadlineMillisLabel.Size = new System.Drawing.Size(122, 15);
            this.deadlineMillisLabel.TabIndex = 7;
            this.deadlineMillisLabel.Text = "Deadline Milliseconds";
            // 
            // maxCoresLabel
            // 
            this.maxCoresLabel.AutoSize = true;
            this.maxCoresLabel.Location = new System.Drawing.Point(12, 220);
            this.maxCoresLabel.Name = "maxCoresLabel";
            this.maxCoresLabel.Size = new System.Drawing.Size(63, 15);
            this.maxCoresLabel.TabIndex = 8;
            this.maxCoresLabel.Text = "Max Cores";
            // 
            // maxCoresNumericUpDown
            // 
            this.maxCoresNumericUpDown.Location = new System.Drawing.Point(12, 242);
            this.maxCoresNumericUpDown.Name = "maxCoresNumericUpDown";
            this.maxCoresNumericUpDown.Size = new System.Drawing.Size(120, 23);
            this.maxCoresNumericUpDown.TabIndex = 9;
            this.maxCoresNumericUpDown.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.maxCoresNumericUpDown.ValueChanged += new System.EventHandler(this.MaxCoresNumericUpDown_ValueChanged);
            // 
            // parallelizeCheckBox
            // 
            this.parallelizeCheckBox.AutoSize = true;
            this.parallelizeCheckBox.Location = new System.Drawing.Point(12, 194);
            this.parallelizeCheckBox.Name = "parallelizeCheckBox";
            this.parallelizeCheckBox.Size = new System.Drawing.Size(78, 19);
            this.parallelizeCheckBox.TabIndex = 11;
            this.parallelizeCheckBox.Text = "Parallelize";
            this.parallelizeCheckBox.UseVisualStyleBackColor = true;
            // 
            // inputFilesListBox
            // 
            this.inputFilesListBox.FormattingEnabled = true;
            this.inputFilesListBox.HorizontalScrollbar = true;
            this.inputFilesListBox.ItemHeight = 15;
            this.inputFilesListBox.Location = new System.Drawing.Point(12, 272);
            this.inputFilesListBox.Name = "inputFilesListBox";
            this.inputFilesListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.inputFilesListBox.Size = new System.Drawing.Size(356, 94);
            this.inputFilesListBox.TabIndex = 12;
            // 
            // inputFilesLabel
            // 
            this.inputFilesLabel.AutoSize = true;
            this.inputFilesLabel.Location = new System.Drawing.Point(12, 373);
            this.inputFilesLabel.Name = "inputFilesLabel";
            this.inputFilesLabel.Size = new System.Drawing.Size(61, 15);
            this.inputFilesLabel.TabIndex = 13;
            this.inputFilesLabel.Text = "Input Files";
            // 
            // inputFilesAddButton
            // 
            this.inputFilesAddButton.Location = new System.Drawing.Point(12, 395);
            this.inputFilesAddButton.Name = "inputFilesAddButton";
            this.inputFilesAddButton.Size = new System.Drawing.Size(75, 23);
            this.inputFilesAddButton.TabIndex = 14;
            this.inputFilesAddButton.Text = "Add";
            this.inputFilesAddButton.UseVisualStyleBackColor = true;
            this.inputFilesAddButton.Click += new System.EventHandler(this.InputFilesAddButton_Click);
            // 
            // inputFilesRemoveButton
            // 
            this.inputFilesRemoveButton.Location = new System.Drawing.Point(91, 395);
            this.inputFilesRemoveButton.Name = "inputFilesRemoveButton";
            this.inputFilesRemoveButton.Size = new System.Drawing.Size(75, 23);
            this.inputFilesRemoveButton.TabIndex = 15;
            this.inputFilesRemoveButton.Text = "Remove";
            this.inputFilesRemoveButton.UseVisualStyleBackColor = true;
            this.inputFilesRemoveButton.Click += new System.EventHandler(this.InputFilesRemoveButton_Click);
            // 
            // inputFilesClearButton
            // 
            this.inputFilesClearButton.Location = new System.Drawing.Point(172, 395);
            this.inputFilesClearButton.Name = "inputFilesClearButton";
            this.inputFilesClearButton.Size = new System.Drawing.Size(75, 23);
            this.inputFilesClearButton.TabIndex = 16;
            this.inputFilesClearButton.Text = "Clear";
            this.inputFilesClearButton.UseVisualStyleBackColor = true;
            this.inputFilesClearButton.Click += new System.EventHandler(this.InputFilesClearButton_Click);
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(12, 487);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 17;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(93, 487);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 18;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // FftTaskSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(389, 522);
            this.ControlBox = false;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.inputFilesClearButton);
            this.Controls.Add(this.inputFilesRemoveButton);
            this.Controls.Add(this.inputFilesAddButton);
            this.Controls.Add(this.inputFilesLabel);
            this.Controls.Add(this.inputFilesListBox);
            this.Controls.Add(this.parallelizeCheckBox);
            this.Controls.Add(this.maxCoresNumericUpDown);
            this.Controls.Add(this.maxCoresLabel);
            this.Controls.Add(this.deadlineMillisLabel);
            this.Controls.Add(this.deadlineMillisNumericUpDown);
            this.Controls.Add(this.deadlineDateTimelabel);
            this.Controls.Add(this.deadlineDateTimePicker);
            this.Controls.Add(this.priorityNumericUpDown);
            this.Controls.Add(this.priorityLabel);
            this.Controls.Add(this.prioritySchedulingCheckBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "FftTaskSettingsForm";
            this.Text = "FFT Task Settings";
            ((System.ComponentModel.ISupportInitialize)(this.priorityNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.deadlineMillisNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxCoresNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox prioritySchedulingCheckBox;
        private System.Windows.Forms.Label priorityLabel;
        private System.Windows.Forms.NumericUpDown priorityNumericUpDown;
        private System.Windows.Forms.DateTimePicker deadlineDateTimePicker;
        private System.Windows.Forms.Label deadlineDateTimelabel;
        private System.Windows.Forms.NumericUpDown deadlineMillisNumericUpDown;
        private System.Windows.Forms.Label deadlineMillisLabel;
        private System.Windows.Forms.Label maxCoresLabel;
        private System.Windows.Forms.NumericUpDown maxCoresNumericUpDown;
        private System.Windows.Forms.CheckBox parallelizeCheckBox;
        private System.Windows.Forms.ListBox inputFilesListBox;
        private System.Windows.Forms.Label inputFilesLabel;
        private System.Windows.Forms.Button inputFilesAddButton;
        private System.Windows.Forms.Button inputFilesRemoveButton;
        private System.Windows.Forms.Button inputFilesClearButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}