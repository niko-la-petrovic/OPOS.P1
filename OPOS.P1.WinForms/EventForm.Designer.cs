namespace OPOS.P1.WinForms
{
    partial class EventForm
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
            this.eventLogRichTextBox = new System.Windows.Forms.RichTextBox();
            this.clearButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // eventLogRichTextBox
            // 
            this.eventLogRichTextBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.eventLogRichTextBox.Location = new System.Drawing.Point(0, 41);
            this.eventLogRichTextBox.Name = "eventLogRichTextBox";
            this.eventLogRichTextBox.ReadOnly = true;
            this.eventLogRichTextBox.Size = new System.Drawing.Size(326, 494);
            this.eventLogRichTextBox.TabIndex = 0;
            this.eventLogRichTextBox.Text = "";
            // 
            // clearButton
            // 
            this.clearButton.Location = new System.Drawing.Point(12, 12);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(75, 23);
            this.clearButton.TabIndex = 1;
            this.clearButton.Text = "Clear";
            this.clearButton.UseVisualStyleBackColor = true;
            this.clearButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // EventForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(326, 535);
            this.ControlBox = false;
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.eventLogRichTextBox);
            this.Name = "EventForm";
            this.Text = "Event Form";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox eventLogRichTextBox;
        private System.Windows.Forms.Button clearButton;
    }
}