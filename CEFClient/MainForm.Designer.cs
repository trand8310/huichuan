namespace CefClient
{
    partial class MainForm
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
            LogTextBox = new TextBox();
            buttonStart = new Button();
            panel_container = new Panel();
            label1 = new Label();
            textBox1 = new TextBox();
            panel_container.SuspendLayout();
            SuspendLayout();
            // 
            // LogTextBox
            // 
            LogTextBox.Dock = DockStyle.Fill;
            LogTextBox.Location = new Point(0, 60);
            LogTextBox.Margin = new Padding(4, 3, 4, 3);
            LogTextBox.Multiline = true;
            LogTextBox.Name = "LogTextBox";
            LogTextBox.ScrollBars = ScrollBars.Both;
            LogTextBox.Size = new Size(937, 681);
            LogTextBox.TabIndex = 4;
            LogTextBox.WordWrap = false;
            // 
            // buttonStart
            // 
            buttonStart.Location = new Point(813, 9);
            buttonStart.Margin = new Padding(4, 5, 4, 5);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(79, 31);
            buttonStart.TabIndex = 9;
            buttonStart.Text = "测试";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // panel_container
            // 
            panel_container.Controls.Add(label1);
            panel_container.Controls.Add(textBox1);
            panel_container.Controls.Add(buttonStart);
            panel_container.Dock = DockStyle.Top;
            panel_container.Location = new Point(0, 0);
            panel_container.Margin = new Padding(4, 5, 4, 5);
            panel_container.Name = "panel_container";
            panel_container.Size = new Size(937, 60);
            panel_container.TabIndex = 10;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(22, 15);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(32, 20);
            label1.TabIndex = 11;
            label1.Text = "url:";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(72, 11);
            textBox1.Margin = new Padding(4, 5, 4, 5);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(730, 27);
            textBox1.TabIndex = 10;
            textBox1.Text = "https://www.baidu.com";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(937, 741);
            Controls.Add(LogTextBox);
            Controls.Add(panel_container);
            Margin = new Padding(4, 5, 4, 5);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "任务管理";
            Load += MainForm_Load;
            panel_container.ResumeLayout(false);
            panel_container.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.TextBox LogTextBox;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Panel panel_container;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
    }
}