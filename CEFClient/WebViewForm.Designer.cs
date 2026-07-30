
namespace CefClient
{
    partial class WebViewForm
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
            textBox_Address = new Label();
            SuspendLayout();
            // 
            // textBox_Address
            // 
            textBox_Address.BackColor = SystemColors.ControlLightLight;
            textBox_Address.Dock = DockStyle.Top;
            textBox_Address.FlatStyle = FlatStyle.Flat;
            textBox_Address.Location = new Point(0, 0);
            textBox_Address.Margin = new Padding(3);
            textBox_Address.Name = "textBox_Address";
            textBox_Address.Padding = new Padding(2);
            textBox_Address.Size = new Size(942, 27);
            textBox_Address.TabIndex = 2;
            textBox_Address.Text = "label1";
            // 
            // WebViewForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(942, 953);
            Controls.Add(textBox_Address);
            Margin = new Padding(4, 5, 4, 5);
            MdiChildrenMinimizedAnchorBottom = false;
            MinimizeBox = false;
            Name = "WebViewForm";
            ShowIcon = false;
            Text = "曝光浏览器";
            Load += WebViewForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private Label textBox_Address;
    }
}