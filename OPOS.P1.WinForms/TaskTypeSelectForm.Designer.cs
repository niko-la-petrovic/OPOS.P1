namespace OPOS.P1.WinForms
{
    partial class TaskTypeSelectForm
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
            this.taskTypeComboBox = new System.Windows.Forms.ComboBox();
            this.taskTypeLabel = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // taskTypeComboBox
            // 
            this.taskTypeComboBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.taskTypeComboBox.FormattingEnabled = true;
            this.taskTypeComboBox.Location = new System.Drawing.Point(12, 27);
            this.taskTypeComboBox.Name = "taskTypeComboBox";
            this.taskTypeComboBox.Size = new System.Drawing.Size(308, 23);
            this.taskTypeComboBox.TabIndex = 0;
            this.taskTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.taskTypeComboBox_SelectedIndexChanged);
            // 
            // taskTypeLabel
            // 
            this.taskTypeLabel.AutoSize = true;
            this.taskTypeLabel.Location = new System.Drawing.Point(12, 9);
            this.taskTypeLabel.Name = "taskTypeLabel";
            this.taskTypeLabel.Size = new System.Drawing.Size(56, 15);
            this.taskTypeLabel.TabIndex = 1;
            this.taskTypeLabel.Text = "Task Type";
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(12, 56);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(245, 56);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // TaskTypeSelectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(332, 101);
            this.ControlBox = false;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.taskTypeLabel);
            this.Controls.Add(this.taskTypeComboBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "TaskTypeSelectForm";
            this.Text = "Task Type Selection";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox taskTypeComboBox;
        private System.Windows.Forms.Label taskTypeLabel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}