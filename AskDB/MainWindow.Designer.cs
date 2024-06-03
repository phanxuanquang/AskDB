namespace AskDB
{
    partial class MainWindow
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
            ApiKeyBox = new TextBox();
            OutputTable = new DataGridView();
            ResponseBox = new RichTextBox();
            SendBtn = new Button();
            ((System.ComponentModel.ISupportInitialize)OutputTable).BeginInit();
            SuspendLayout();
            // 
            // ApiKeyBox
            // 
            ApiKeyBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ApiKeyBox.BackColor = Color.White;
            ApiKeyBox.Font = new Font("Segoe UI", 11F);
            ApiKeyBox.Location = new Point(12, 12);
            ApiKeyBox.Name = "ApiKeyBox";
            ApiKeyBox.PlaceholderText = "   Enter your your question . . .";
            ApiKeyBox.Size = new Size(1051, 37);
            ApiKeyBox.TabIndex = 0;
            // 
            // OutputTable
            // 
            OutputTable.AllowUserToAddRows = false;
            OutputTable.AllowUserToDeleteRows = false;
            OutputTable.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            OutputTable.BackgroundColor = SystemColors.ControlLight;
            OutputTable.BorderStyle = BorderStyle.None;
            OutputTable.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            OutputTable.Location = new Point(12, 435);
            OutputTable.Name = "OutputTable";
            OutputTable.ReadOnly = true;
            OutputTable.RowHeadersWidth = 62;
            OutputTable.Size = new Size(1181, 312);
            OutputTable.TabIndex = 1;
            // 
            // ResponseBox
            // 
            ResponseBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ResponseBox.BackColor = Color.White;
            ResponseBox.BorderStyle = BorderStyle.None;
            ResponseBox.Font = new Font("Segoe UI", 10F);
            ResponseBox.Location = new Point(12, 55);
            ResponseBox.Name = "ResponseBox";
            ResponseBox.ReadOnly = true;
            ResponseBox.Size = new Size(1181, 363);
            ResponseBox.TabIndex = 2;
            ResponseBox.Text = "";
            // 
            // SendBtn
            // 
            SendBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SendBtn.BackColor = Color.White;
            SendBtn.Location = new Point(1069, 12);
            SendBtn.Name = "SendBtn";
            SendBtn.Size = new Size(124, 37);
            SendBtn.TabIndex = 3;
            SendBtn.Text = "Send";
            SendBtn.UseVisualStyleBackColor = false;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1205, 759);
            Controls.Add(SendBtn);
            Controls.Add(ResponseBox);
            Controls.Add(OutputTable);
            Controls.Add(ApiKeyBox);
            DoubleBuffered = true;
            Name = "MainWindow";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AskDb";
            ((System.ComponentModel.ISupportInitialize)OutputTable).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox ApiKeyBox;
        private DataGridView OutputTable;
        private RichTextBox ResponseBox;
        private Button SendBtn;
    }
}
