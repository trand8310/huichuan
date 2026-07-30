namespace MainClient
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
            groupBox1 = new GroupBox();
            taskInfoListView = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();
            columnHeader6 = new ColumnHeader();
            groupBox2 = new GroupBox();
            checkBox_UVsTriggerOne = new CheckBox();
            label38 = new Label();
            label39 = new Label();
            numericUpDown_IpTtl = new NumericUpDown();
            label25 = new Label();
            numericUpDown_DspBidPrice = new NumericUpDown();
            label24 = new Label();
            label16 = new Label();
            numericUpDown_PageLoadingTimeout = new NumericUpDown();
            label15 = new Label();
            numericUpDown_MaximumCacheCount = new NumericUpDown();
            checkBox_UseCacheJS = new CheckBox();
            checkBox_UseCacheCss = new CheckBox();
            checkBox_UseCacheVideo = new CheckBox();
            checkBox_UseCacheImg = new CheckBox();
            label6 = new Label();
            label8 = new Label();
            label7 = new Label();
            label9 = new Label();
            label5 = new Label();
            linkLabel1 = new LinkLabel();
            checkBox_IsDetailLog = new CheckBox();
            textBox_DevApiUrl = new TextBox();
            label14 = new Label();
            checkBox_DisableUserCache = new CheckBox();
            checkBox_DisableLoadImage = new CheckBox();
            label19 = new Label();
            label18 = new Label();
            numericUpDown_SubResetTimeout = new NumericUpDown();
            label17 = new Label();
            numericUpDown_MainResetTimeout = new NumericUpDown();
            label26 = new Label();
            checkBox_IsProxyMode = new CheckBox();
            checkBox_IsHiddenMode = new CheckBox();
            checkBox_IsCheckIp = new CheckBox();
            groupBox5 = new GroupBox();
            button2 = new Button();
            label23 = new Label();
            textBox_SmsPhone = new TextBox();
            label22 = new Label();
            numericUpDown_SendSmsTimeout = new NumericUpDown();
            label21 = new Label();
            label20 = new Label();
            textBox_SmsName = new TextBox();
            checkBox_SendSms = new CheckBox();
            checkBox_IsRealIp = new CheckBox();
            label11 = new Label();
            numericUpDown_Multiple = new NumericUpDown();
            buttonClear = new Button();
            textBox_TaskApiUrl = new TextBox();
            label10 = new Label();
            label13 = new Label();
            numericUpDown_FetchTaskInterval = new NumericUpDown();
            buttonStart = new Button();
            label12 = new Label();
            numericUpDown_MaximumConcurrency = new NumericUpDown();
            label90 = new Label();
            numericUpDown_UVInterval = new NumericUpDown();
            label4 = new Label();
            textBox_TaskName = new TextBox();
            label3 = new Label();
            label2 = new Label();
            textBox_ProxyIpUrl = new TextBox();
            label1 = new Label();
            groupBox6 = new GroupBox();
            radioButton_UseLocalDev = new RadioButton();
            radioButton_UsingRealDev = new RadioButton();
            radioButton_UseSystemDev = new RadioButton();
            LogTextBox = new TextBox();
            groupBox3 = new GroupBox();
            LogDetailTextBox = new TextBox();
            groupBox4 = new GroupBox();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            toolStripStatusLabel3 = new ToolStripStatusLabel();
            toolStripStatusLabel4 = new ToolStripStatusLabel();
            toolStripStatusLabel5 = new ToolStripStatusLabel();
            toolStripStatusLabel6 = new ToolStripStatusLabel();
            checkBox_PersistAdx = new CheckBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_IpTtl).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_DspBidPrice).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_PageLoadingTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumCacheCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SubResetTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MainResetTimeout).BeginInit();
            groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SendSmsTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_Multiple).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_FetchTaskInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumConcurrency).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_UVInterval).BeginInit();
            groupBox6.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(taskInfoListView);
            groupBox1.Dock = DockStyle.Left;
            groupBox1.Location = new Point(0, 377);
            groupBox1.Margin = new Padding(5, 4, 5, 4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(5, 4, 5, 4);
            groupBox1.Size = new Size(509, 181);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "任务列表";
            // 
            // taskInfoListView
            // 
            taskInfoListView.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader3, columnHeader4, columnHeader5, columnHeader6 });
            taskInfoListView.Dock = DockStyle.Fill;
            taskInfoListView.FullRowSelect = true;
            taskInfoListView.GridLines = true;
            taskInfoListView.Location = new Point(5, 24);
            taskInfoListView.Margin = new Padding(5, 4, 5, 4);
            taskInfoListView.Name = "taskInfoListView";
            taskInfoListView.Size = new Size(499, 153);
            taskInfoListView.TabIndex = 0;
            taskInfoListView.UseCompatibleStateImageBehavior = false;
            taskInfoListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "任务名称";
            columnHeader1.Width = 120;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "真实IP";
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "延迟";
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "归属地";
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "状态";
            columnHeader6.Width = 120;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(checkBox_PersistAdx);
            groupBox2.Controls.Add(checkBox_UVsTriggerOne);
            groupBox2.Controls.Add(label38);
            groupBox2.Controls.Add(label39);
            groupBox2.Controls.Add(numericUpDown_IpTtl);
            groupBox2.Controls.Add(label25);
            groupBox2.Controls.Add(numericUpDown_DspBidPrice);
            groupBox2.Controls.Add(label24);
            groupBox2.Controls.Add(label16);
            groupBox2.Controls.Add(numericUpDown_PageLoadingTimeout);
            groupBox2.Controls.Add(label15);
            groupBox2.Controls.Add(numericUpDown_MaximumCacheCount);
            groupBox2.Controls.Add(checkBox_UseCacheJS);
            groupBox2.Controls.Add(checkBox_UseCacheCss);
            groupBox2.Controls.Add(checkBox_UseCacheVideo);
            groupBox2.Controls.Add(checkBox_UseCacheImg);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(label8);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(linkLabel1);
            groupBox2.Controls.Add(checkBox_IsDetailLog);
            groupBox2.Controls.Add(textBox_DevApiUrl);
            groupBox2.Controls.Add(label14);
            groupBox2.Controls.Add(checkBox_DisableUserCache);
            groupBox2.Controls.Add(checkBox_DisableLoadImage);
            groupBox2.Controls.Add(label19);
            groupBox2.Controls.Add(label18);
            groupBox2.Controls.Add(numericUpDown_SubResetTimeout);
            groupBox2.Controls.Add(label17);
            groupBox2.Controls.Add(numericUpDown_MainResetTimeout);
            groupBox2.Controls.Add(label26);
            groupBox2.Controls.Add(checkBox_IsProxyMode);
            groupBox2.Controls.Add(checkBox_IsHiddenMode);
            groupBox2.Controls.Add(checkBox_IsCheckIp);
            groupBox2.Controls.Add(groupBox5);
            groupBox2.Controls.Add(checkBox_IsRealIp);
            groupBox2.Controls.Add(label11);
            groupBox2.Controls.Add(numericUpDown_Multiple);
            groupBox2.Controls.Add(buttonClear);
            groupBox2.Controls.Add(textBox_TaskApiUrl);
            groupBox2.Controls.Add(label10);
            groupBox2.Controls.Add(label13);
            groupBox2.Controls.Add(numericUpDown_FetchTaskInterval);
            groupBox2.Controls.Add(buttonStart);
            groupBox2.Controls.Add(label12);
            groupBox2.Controls.Add(numericUpDown_MaximumConcurrency);
            groupBox2.Controls.Add(label90);
            groupBox2.Controls.Add(numericUpDown_UVInterval);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(textBox_TaskName);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(textBox_ProxyIpUrl);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(groupBox6);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.Location = new Point(0, 0);
            groupBox2.Margin = new Padding(5, 4, 5, 4);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(5, 4, 5, 4);
            groupBox2.Size = new Size(1129, 377);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "设置";
            // 
            // checkBox_UVsTriggerOne
            // 
            checkBox_UVsTriggerOne.AutoSize = true;
            checkBox_UVsTriggerOne.Location = new Point(581, 283);
            checkBox_UVsTriggerOne.Margin = new Padding(6);
            checkBox_UVsTriggerOne.Name = "checkBox_UVsTriggerOne";
            checkBox_UVsTriggerOne.Size = new Size(185, 24);
            checkBox_UVsTriggerOne.TabIndex = 150;
            checkBox_UVsTriggerOne.Text = "多UV时,仅触发1次点击";
            checkBox_UVsTriggerOne.UseVisualStyleBackColor = true;
            // 
            // label38
            // 
            label38.AutoSize = true;
            label38.Location = new Point(230, 260);
            label38.Margin = new Padding(6, 0, 6, 0);
            label38.Name = "label38";
            label38.Size = new Size(24, 20);
            label38.TabIndex = 144;
            label38.Text = "秒";
            // 
            // label39
            // 
            label39.AutoSize = true;
            label39.Location = new Point(31, 259);
            label39.Margin = new Padding(6, 0, 6, 0);
            label39.Name = "label39";
            label39.Size = new Size(86, 20);
            label39.TabIndex = 142;
            label39.Text = "IP有效时长:";
            // 
            // numericUpDown_IpTtl
            // 
            numericUpDown_IpTtl.Location = new Point(120, 255);
            numericUpDown_IpTtl.Margin = new Padding(6, 5, 6, 5);
            numericUpDown_IpTtl.Maximum = new decimal(new int[] { 1800, 0, 0, 0 });
            numericUpDown_IpTtl.Name = "numericUpDown_IpTtl";
            numericUpDown_IpTtl.Size = new Size(104, 27);
            numericUpDown_IpTtl.TabIndex = 143;
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Location = new Point(317, 294);
            label25.Margin = new Padding(5, 0, 5, 0);
            label25.Name = "label25";
            label25.Size = new Size(73, 20);
            label25.TabIndex = 95;
            label25.Text = "最低竞价:";
            // 
            // numericUpDown_DspBidPrice
            // 
            numericUpDown_DspBidPrice.Location = new Point(395, 290);
            numericUpDown_DspBidPrice.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_DspBidPrice.Name = "numericUpDown_DspBidPrice";
            numericUpDown_DspBidPrice.Size = new Size(87, 27);
            numericUpDown_DspBidPrice.TabIndex = 96;
            numericUpDown_DspBidPrice.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(485, 259);
            label24.Margin = new Padding(5, 0, 5, 0);
            label24.Name = "label24";
            label24.Size = new Size(24, 20);
            label24.TabIndex = 94;
            label24.Text = "秒";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(287, 259);
            label16.Margin = new Padding(5, 0, 5, 0);
            label16.Name = "label16";
            label16.Size = new Size(103, 20);
            label16.TabIndex = 92;
            label16.Text = "页面加载超时:";
            // 
            // numericUpDown_PageLoadingTimeout
            // 
            numericUpDown_PageLoadingTimeout.Location = new Point(395, 255);
            numericUpDown_PageLoadingTimeout.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_PageLoadingTimeout.Name = "numericUpDown_PageLoadingTimeout";
            numericUpDown_PageLoadingTimeout.Size = new Size(87, 27);
            numericUpDown_PageLoadingTimeout.TabIndex = 93;
            numericUpDown_PageLoadingTimeout.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(318, 226);
            label15.Margin = new Padding(5, 0, 5, 0);
            label15.Name = "label15";
            label15.Size = new Size(73, 20);
            label15.TabIndex = 90;
            label15.Text = "缓存数量:";
            // 
            // numericUpDown_MaximumCacheCount
            // 
            numericUpDown_MaximumCacheCount.Location = new Point(395, 222);
            numericUpDown_MaximumCacheCount.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_MaximumCacheCount.Name = "numericUpDown_MaximumCacheCount";
            numericUpDown_MaximumCacheCount.Size = new Size(87, 27);
            numericUpDown_MaximumCacheCount.TabIndex = 91;
            numericUpDown_MaximumCacheCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // checkBox_UseCacheJS
            // 
            checkBox_UseCacheJS.AutoSize = true;
            checkBox_UseCacheJS.Location = new Point(699, 256);
            checkBox_UseCacheJS.Margin = new Padding(5);
            checkBox_UseCacheJS.Name = "checkBox_UseCacheJS";
            checkBox_UseCacheJS.Size = new Size(76, 24);
            checkBox_UseCacheJS.TabIndex = 89;
            checkBox_UseCacheJS.Text = "缓存JS";
            checkBox_UseCacheJS.UseVisualStyleBackColor = true;
            // 
            // checkBox_UseCacheCss
            // 
            checkBox_UseCacheCss.AutoSize = true;
            checkBox_UseCacheCss.Location = new Point(786, 256);
            checkBox_UseCacheCss.Margin = new Padding(5);
            checkBox_UseCacheCss.Name = "checkBox_UseCacheCss";
            checkBox_UseCacheCss.Size = new Size(89, 24);
            checkBox_UseCacheCss.TabIndex = 88;
            checkBox_UseCacheCss.Text = "缓存CSS";
            checkBox_UseCacheCss.UseVisualStyleBackColor = true;
            // 
            // checkBox_UseCacheVideo
            // 
            checkBox_UseCacheVideo.AutoSize = true;
            checkBox_UseCacheVideo.Location = new Point(699, 229);
            checkBox_UseCacheVideo.Margin = new Padding(5);
            checkBox_UseCacheVideo.Name = "checkBox_UseCacheVideo";
            checkBox_UseCacheVideo.Size = new Size(91, 24);
            checkBox_UseCacheVideo.TabIndex = 87;
            checkBox_UseCacheVideo.Text = "缓存视频";
            checkBox_UseCacheVideo.UseVisualStyleBackColor = true;
            // 
            // checkBox_UseCacheImg
            // 
            checkBox_UseCacheImg.AutoSize = true;
            checkBox_UseCacheImg.Location = new Point(699, 201);
            checkBox_UseCacheImg.Margin = new Padding(5);
            checkBox_UseCacheImg.Name = "checkBox_UseCacheImg";
            checkBox_UseCacheImg.Size = new Size(91, 24);
            checkBox_UseCacheImg.TabIndex = 86;
            checkBox_UseCacheImg.Text = "缓存图片";
            checkBox_UseCacheImg.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(622, 44);
            label6.Margin = new Padding(5, 0, 5, 0);
            label6.Name = "label6";
            label6.Size = new Size(82, 20);
            label6.TabIndex = 85;
            label6.Text = "提交数量:0";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(622, 96);
            label8.Margin = new Padding(5, 0, 5, 0);
            label8.Name = "label8";
            label8.Size = new Size(82, 20);
            label8.TabIndex = 84;
            label8.Text = "点击数量:0";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(622, 71);
            label7.Margin = new Padding(5, 0, 5, 0);
            label7.Name = "label7";
            label7.Size = new Size(82, 20);
            label7.TabIndex = 83;
            label7.Text = "曝光数量:0";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(622, 122);
            label9.Margin = new Padding(5, 0, 5, 0);
            label9.Name = "label9";
            label9.Size = new Size(82, 20);
            label9.TabIndex = 82;
            label9.Text = "运行时间:0";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(622, 18);
            label5.Margin = new Padding(5, 0, 5, 0);
            label5.Name = "label5";
            label5.Size = new Size(82, 20);
            label5.TabIndex = 81;
            label5.Text = "请求数量:0";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(827, 220);
            linkLabel1.Margin = new Padding(4, 0, 4, 0);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(69, 20);
            linkLabel1.TabIndex = 80;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "开机启动";
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            // 
            // checkBox_IsDetailLog
            // 
            checkBox_IsDetailLog.AutoSize = true;
            checkBox_IsDetailLog.Location = new Point(581, 256);
            checkBox_IsDetailLog.Margin = new Padding(5);
            checkBox_IsDetailLog.Name = "checkBox_IsDetailLog";
            checkBox_IsDetailLog.Size = new Size(91, 24);
            checkBox_IsDetailLog.TabIndex = 75;
            checkBox_IsDetailLog.Text = "详细日志";
            checkBox_IsDetailLog.UseVisualStyleBackColor = true;
            // 
            // textBox_DevApiUrl
            // 
            textBox_DevApiUrl.Location = new Point(120, 89);
            textBox_DevApiUrl.Margin = new Padding(5, 4, 5, 4);
            textBox_DevApiUrl.Name = "textBox_DevApiUrl";
            textBox_DevApiUrl.Size = new Size(361, 27);
            textBox_DevApiUrl.TabIndex = 74;
            textBox_DevApiUrl.Text = "http://117.21.200.18:9000/api/getdev.php";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(42, 93);
            label14.Margin = new Padding(5, 0, 5, 0);
            label14.Name = "label14";
            label14.Size = new Size(73, 20);
            label14.TabIndex = 73;
            label14.Text = "设备接口:";
            // 
            // checkBox_DisableUserCache
            // 
            checkBox_DisableUserCache.AutoSize = true;
            checkBox_DisableUserCache.Location = new Point(699, 145);
            checkBox_DisableUserCache.Margin = new Padding(5);
            checkBox_DisableUserCache.Name = "checkBox_DisableUserCache";
            checkBox_DisableUserCache.Size = new Size(121, 24);
            checkBox_DisableUserCache.TabIndex = 71;
            checkBox_DisableUserCache.Text = "禁止本地缓存";
            checkBox_DisableUserCache.UseVisualStyleBackColor = true;
            // 
            // checkBox_DisableLoadImage
            // 
            checkBox_DisableLoadImage.AutoSize = true;
            checkBox_DisableLoadImage.Location = new Point(699, 173);
            checkBox_DisableLoadImage.Margin = new Padding(5);
            checkBox_DisableLoadImage.Name = "checkBox_DisableLoadImage";
            checkBox_DisableLoadImage.Size = new Size(121, 24);
            checkBox_DisableLoadImage.TabIndex = 69;
            checkBox_DisableLoadImage.Text = "禁止加载图片";
            checkBox_DisableLoadImage.UseVisualStyleBackColor = true;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(485, 156);
            label19.Margin = new Padding(5, 0, 5, 0);
            label19.Name = "label19";
            label19.Size = new Size(83, 20);
            label19.TabIndex = 65;
            label19.Text = "分钟±30秒";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(485, 191);
            label18.Margin = new Padding(5, 0, 5, 0);
            label18.Name = "label18";
            label18.Size = new Size(83, 20);
            label18.TabIndex = 64;
            label18.Text = "分钟±30秒";
            // 
            // numericUpDown_SubResetTimeout
            // 
            numericUpDown_SubResetTimeout.Location = new Point(395, 154);
            numericUpDown_SubResetTimeout.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_SubResetTimeout.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            numericUpDown_SubResetTimeout.Name = "numericUpDown_SubResetTimeout";
            numericUpDown_SubResetTimeout.Size = new Size(87, 27);
            numericUpDown_SubResetTimeout.TabIndex = 63;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(302, 158);
            label17.Margin = new Padding(5, 0, 5, 0);
            label17.Name = "label17";
            label17.Size = new Size(88, 20);
            label17.TabIndex = 62;
            label17.Text = "子进程重置:";
            // 
            // numericUpDown_MainResetTimeout
            // 
            numericUpDown_MainResetTimeout.Location = new Point(395, 187);
            numericUpDown_MainResetTimeout.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_MainResetTimeout.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            numericUpDown_MainResetTimeout.Name = "numericUpDown_MainResetTimeout";
            numericUpDown_MainResetTimeout.Size = new Size(87, 27);
            numericUpDown_MainResetTimeout.TabIndex = 61;
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Location = new Point(302, 191);
            label26.Margin = new Padding(5, 0, 5, 0);
            label26.Name = "label26";
            label26.Size = new Size(88, 20);
            label26.TabIndex = 60;
            label26.Text = "主进程重置:";
            // 
            // checkBox_IsProxyMode
            // 
            checkBox_IsProxyMode.AutoSize = true;
            checkBox_IsProxyMode.Location = new Point(581, 173);
            checkBox_IsProxyMode.Margin = new Padding(5);
            checkBox_IsProxyMode.Name = "checkBox_IsProxyMode";
            checkBox_IsProxyMode.Size = new Size(91, 24);
            checkBox_IsProxyMode.TabIndex = 59;
            checkBox_IsProxyMode.Text = "代理模式";
            checkBox_IsProxyMode.UseVisualStyleBackColor = true;
            // 
            // checkBox_IsHiddenMode
            // 
            checkBox_IsHiddenMode.AutoSize = true;
            checkBox_IsHiddenMode.Location = new Point(581, 145);
            checkBox_IsHiddenMode.Margin = new Padding(5);
            checkBox_IsHiddenMode.Name = "checkBox_IsHiddenMode";
            checkBox_IsHiddenMode.Size = new Size(91, 24);
            checkBox_IsHiddenMode.TabIndex = 58;
            checkBox_IsHiddenMode.Text = "隐藏模式";
            checkBox_IsHiddenMode.UseVisualStyleBackColor = true;
            // 
            // checkBox_IsCheckIp
            // 
            checkBox_IsCheckIp.AutoSize = true;
            checkBox_IsCheckIp.Location = new Point(581, 229);
            checkBox_IsCheckIp.Margin = new Padding(5);
            checkBox_IsCheckIp.Name = "checkBox_IsCheckIp";
            checkBox_IsCheckIp.Size = new Size(119, 24);
            checkBox_IsCheckIp.TabIndex = 51;
            checkBox_IsCheckIp.Text = "检测IP有效性";
            checkBox_IsCheckIp.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(button2);
            groupBox5.Controls.Add(label23);
            groupBox5.Controls.Add(textBox_SmsPhone);
            groupBox5.Controls.Add(label22);
            groupBox5.Controls.Add(numericUpDown_SendSmsTimeout);
            groupBox5.Controls.Add(label21);
            groupBox5.Controls.Add(label20);
            groupBox5.Controls.Add(textBox_SmsName);
            groupBox5.Controls.Add(checkBox_SendSms);
            groupBox5.Location = new Point(827, 13);
            groupBox5.Margin = new Padding(5);
            groupBox5.Name = "groupBox5";
            groupBox5.Padding = new Padding(5);
            groupBox5.Size = new Size(276, 132);
            groupBox5.TabIndex = 45;
            groupBox5.TabStop = false;
            // 
            // button2
            // 
            button2.Location = new Point(193, 29);
            button2.Margin = new Padding(5);
            button2.Name = "button2";
            button2.Size = new Size(54, 28);
            button2.TabIndex = 51;
            button2.Text = "测试";
            button2.UseVisualStyleBackColor = true;
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(21, 65);
            label23.Margin = new Padding(5, 0, 5, 0);
            label23.Name = "label23";
            label23.Size = new Size(43, 20);
            label23.TabIndex = 50;
            label23.Text = "电话:";
            // 
            // textBox_SmsPhone
            // 
            textBox_SmsPhone.Location = new Point(71, 61);
            textBox_SmsPhone.Margin = new Padding(5);
            textBox_SmsPhone.Name = "textBox_SmsPhone";
            textBox_SmsPhone.Size = new Size(174, 27);
            textBox_SmsPhone.TabIndex = 49;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(143, 95);
            label22.Margin = new Padding(5, 0, 5, 0);
            label22.Name = "label22";
            label22.Size = new Size(103, 20);
            label22.TabIndex = 48;
            label22.Text = "分钟,发送短信";
            // 
            // numericUpDown_SendSmsTimeout
            // 
            numericUpDown_SendSmsTimeout.Location = new Point(71, 92);
            numericUpDown_SendSmsTimeout.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_SendSmsTimeout.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            numericUpDown_SendSmsTimeout.Name = "numericUpDown_SendSmsTimeout";
            numericUpDown_SendSmsTimeout.Size = new Size(67, 27);
            numericUpDown_SendSmsTimeout.TabIndex = 47;
            numericUpDown_SendSmsTimeout.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(21, 95);
            label21.Margin = new Padding(5, 0, 5, 0);
            label21.Name = "label21";
            label21.Size = new Size(43, 20);
            label21.TabIndex = 46;
            label21.Text = "超时:";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(21, 34);
            label20.Margin = new Padding(5, 0, 5, 0);
            label20.Name = "label20";
            label20.Size = new Size(43, 20);
            label20.TabIndex = 44;
            label20.Text = "名称:";
            // 
            // textBox_SmsName
            // 
            textBox_SmsName.Location = new Point(71, 31);
            textBox_SmsName.Margin = new Padding(5);
            textBox_SmsName.Name = "textBox_SmsName";
            textBox_SmsName.Size = new Size(117, 27);
            textBox_SmsName.TabIndex = 43;
            // 
            // checkBox_SendSms
            // 
            checkBox_SendSms.AutoSize = true;
            checkBox_SendSms.Location = new Point(9, -1);
            checkBox_SendSms.Margin = new Padding(5);
            checkBox_SendSms.Name = "checkBox_SendSms";
            checkBox_SendSms.Size = new Size(91, 24);
            checkBox_SendSms.TabIndex = 45;
            checkBox_SendSms.Text = "短信服务";
            checkBox_SendSms.UseVisualStyleBackColor = true;
            // 
            // checkBox_IsRealIp
            // 
            checkBox_IsRealIp.AutoSize = true;
            checkBox_IsRealIp.Location = new Point(581, 201);
            checkBox_IsRealIp.Margin = new Padding(5);
            checkBox_IsRealIp.Name = "checkBox_IsRealIp";
            checkBox_IsRealIp.Size = new Size(74, 24);
            checkBox_IsRealIp.TabIndex = 35;
            checkBox_IsRealIp.Text = "真实IP";
            checkBox_IsRealIp.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(318, 125);
            label11.Margin = new Padding(5, 0, 5, 0);
            label11.Name = "label11";
            label11.Size = new Size(73, 20);
            label11.TabIndex = 31;
            label11.Text = "任务倍速:";
            // 
            // numericUpDown_Multiple
            // 
            numericUpDown_Multiple.Location = new Point(395, 121);
            numericUpDown_Multiple.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_Multiple.Name = "numericUpDown_Multiple";
            numericUpDown_Multiple.Size = new Size(87, 27);
            numericUpDown_Multiple.TabIndex = 32;
            numericUpDown_Multiple.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // buttonClear
            // 
            buttonClear.Font = new Font("宋体", 9F);
            buttonClear.ForeColor = Color.Red;
            buttonClear.Location = new Point(492, 84);
            buttonClear.Margin = new Padding(5);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(120, 40);
            buttonClear.TabIndex = 22;
            buttonClear.Text = "清除";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
            // 
            // textBox_TaskApiUrl
            // 
            textBox_TaskApiUrl.Location = new Point(120, 55);
            textBox_TaskApiUrl.Margin = new Padding(5, 4, 5, 4);
            textBox_TaskApiUrl.Name = "textBox_TaskApiUrl";
            textBox_TaskApiUrl.Size = new Size(361, 27);
            textBox_TaskApiUrl.TabIndex = 21;
            textBox_TaskApiUrl.Text = "http://117.21.200.148/client-v5.php";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(42, 59);
            label10.Margin = new Padding(5, 0, 5, 0);
            label10.Name = "label10";
            label10.Size = new Size(73, 20);
            label10.TabIndex = 20;
            label10.Text = "任务接口:";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(212, 191);
            label13.Margin = new Padding(5, 0, 5, 0);
            label13.Name = "label13";
            label13.Size = new Size(39, 20);
            label13.TabIndex = 15;
            label13.Text = "毫秒";
            // 
            // numericUpDown_FetchTaskInterval
            // 
            numericUpDown_FetchTaskInterval.Location = new Point(120, 154);
            numericUpDown_FetchTaskInterval.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_FetchTaskInterval.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            numericUpDown_FetchTaskInterval.Name = "numericUpDown_FetchTaskInterval";
            numericUpDown_FetchTaskInterval.Size = new Size(87, 27);
            numericUpDown_FetchTaskInterval.TabIndex = 14;
            numericUpDown_FetchTaskInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // buttonStart
            // 
            buttonStart.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            buttonStart.Location = new Point(492, 18);
            buttonStart.Margin = new Padding(5);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(120, 59);
            buttonStart.TabIndex = 13;
            buttonStart.Text = "开始";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(42, 224);
            label12.Margin = new Padding(5, 0, 5, 0);
            label12.Name = "label12";
            label12.Size = new Size(73, 20);
            label12.TabIndex = 9;
            label12.Text = "并发数量:";
            // 
            // numericUpDown_MaximumConcurrency
            // 
            numericUpDown_MaximumConcurrency.Location = new Point(120, 220);
            numericUpDown_MaximumConcurrency.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_MaximumConcurrency.Name = "numericUpDown_MaximumConcurrency";
            numericUpDown_MaximumConcurrency.Size = new Size(87, 27);
            numericUpDown_MaximumConcurrency.TabIndex = 10;
            numericUpDown_MaximumConcurrency.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label90
            // 
            label90.AutoSize = true;
            label90.Location = new Point(37, 191);
            label90.Margin = new Padding(5, 0, 5, 0);
            label90.Name = "label90";
            label90.Size = new Size(79, 20);
            label90.TabIndex = 0;
            label90.Text = "单UV间隔:";
            // 
            // numericUpDown_UVInterval
            // 
            numericUpDown_UVInterval.Location = new Point(120, 187);
            numericUpDown_UVInterval.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_UVInterval.Maximum = new decimal(new int[] { 30000, 0, 0, 0 });
            numericUpDown_UVInterval.Name = "numericUpDown_UVInterval";
            numericUpDown_UVInterval.Size = new Size(87, 27);
            numericUpDown_UVInterval.TabIndex = 3;
            numericUpDown_UVInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(42, 125);
            label4.Margin = new Padding(5, 0, 5, 0);
            label4.Name = "label4";
            label4.Size = new Size(73, 20);
            label4.TabIndex = 0;
            label4.Text = "任务标识:";
            // 
            // textBox_TaskName
            // 
            textBox_TaskName.Location = new Point(120, 121);
            textBox_TaskName.Margin = new Padding(5, 4, 5, 4);
            textBox_TaskName.Name = "textBox_TaskName";
            textBox_TaskName.Size = new Size(131, 27);
            textBox_TaskName.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(212, 158);
            label3.Margin = new Padding(5, 0, 5, 0);
            label3.Name = "label3";
            label3.Size = new Size(39, 20);
            label3.TabIndex = 0;
            label3.Text = "毫秒";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(13, 158);
            label2.Margin = new Padding(5, 0, 5, 0);
            label2.Name = "label2";
            label2.Size = new Size(103, 20);
            label2.TabIndex = 0;
            label2.Text = "获取任务间隔:";
            // 
            // textBox_ProxyIpUrl
            // 
            textBox_ProxyIpUrl.Location = new Point(120, 19);
            textBox_ProxyIpUrl.Margin = new Padding(5, 4, 5, 4);
            textBox_ProxyIpUrl.Name = "textBox_ProxyIpUrl";
            textBox_ProxyIpUrl.Size = new Size(361, 27);
            textBox_ProxyIpUrl.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(30, 24);
            label1.Margin = new Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new Size(86, 20);
            label1.TabIndex = 0;
            label1.Text = "代理IP接口:";
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(radioButton_UseLocalDev);
            groupBox6.Controls.Add(radioButton_UsingRealDev);
            groupBox6.Controls.Add(radioButton_UseSystemDev);
            groupBox6.Location = new Point(827, 154);
            groupBox6.Margin = new Padding(5);
            groupBox6.Name = "groupBox6";
            groupBox6.Padding = new Padding(5);
            groupBox6.Size = new Size(276, 60);
            groupBox6.TabIndex = 52;
            groupBox6.TabStop = false;
            groupBox6.Text = "设备库";
            // 
            // radioButton_UseLocalDev
            // 
            radioButton_UseLocalDev.AutoSize = true;
            radioButton_UseLocalDev.Location = new Point(189, 25);
            radioButton_UseLocalDev.Margin = new Padding(5);
            radioButton_UseLocalDev.Name = "radioButton_UseLocalDev";
            radioButton_UseLocalDev.Size = new Size(75, 24);
            radioButton_UseLocalDev.TabIndex = 56;
            radioButton_UseLocalDev.TabStop = true;
            radioButton_UseLocalDev.Text = "本地库";
            radioButton_UseLocalDev.UseVisualStyleBackColor = true;
            // 
            // radioButton_UsingRealDev
            // 
            radioButton_UsingRealDev.AutoSize = true;
            radioButton_UsingRealDev.Location = new Point(105, 25);
            radioButton_UsingRealDev.Margin = new Padding(5);
            radioButton_UsingRealDev.Name = "radioButton_UsingRealDev";
            radioButton_UsingRealDev.Size = new Size(75, 24);
            radioButton_UsingRealDev.TabIndex = 55;
            radioButton_UsingRealDev.TabStop = true;
            radioButton_UsingRealDev.Text = "真机库";
            radioButton_UsingRealDev.UseVisualStyleBackColor = true;
            // 
            // radioButton_UseSystemDev
            // 
            radioButton_UseSystemDev.AutoSize = true;
            radioButton_UseSystemDev.Checked = true;
            radioButton_UseSystemDev.Location = new Point(21, 25);
            radioButton_UseSystemDev.Margin = new Padding(5);
            radioButton_UseSystemDev.Name = "radioButton_UseSystemDev";
            radioButton_UseSystemDev.Size = new Size(75, 24);
            radioButton_UseSystemDev.TabIndex = 54;
            radioButton_UseSystemDev.TabStop = true;
            radioButton_UseSystemDev.Text = "系统库";
            radioButton_UseSystemDev.UseVisualStyleBackColor = true;
            // 
            // LogTextBox
            // 
            LogTextBox.Dock = DockStyle.Fill;
            LogTextBox.Location = new Point(5, 24);
            LogTextBox.Margin = new Padding(5, 4, 5, 4);
            LogTextBox.MaxLength = 3000;
            LogTextBox.Multiline = true;
            LogTextBox.Name = "LogTextBox";
            LogTextBox.ScrollBars = ScrollBars.Both;
            LogTextBox.Size = new Size(610, 153);
            LogTextBox.TabIndex = 3;
            LogTextBox.WordWrap = false;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(LogTextBox);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Location = new Point(509, 377);
            groupBox3.Margin = new Padding(5, 4, 5, 4);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(5, 4, 5, 4);
            groupBox3.Size = new Size(620, 181);
            groupBox3.TabIndex = 4;
            groupBox3.TabStop = false;
            groupBox3.Text = "日志";
            // 
            // LogDetailTextBox
            // 
            LogDetailTextBox.Dock = DockStyle.Fill;
            LogDetailTextBox.Location = new Point(5, 24);
            LogDetailTextBox.Margin = new Padding(5, 4, 5, 4);
            LogDetailTextBox.MaxLength = 1000;
            LogDetailTextBox.Multiline = true;
            LogDetailTextBox.Name = "LogDetailTextBox";
            LogDetailTextBox.ScrollBars = ScrollBars.Both;
            LogDetailTextBox.Size = new Size(1119, 219);
            LogDetailTextBox.TabIndex = 3;
            LogDetailTextBox.WordWrap = false;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(LogDetailTextBox);
            groupBox4.Dock = DockStyle.Bottom;
            groupBox4.Location = new Point(0, 558);
            groupBox4.Margin = new Padding(5, 4, 5, 4);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new Padding(5, 4, 5, 4);
            groupBox4.Size = new Size(1129, 247);
            groupBox4.TabIndex = 4;
            groupBox4.TabStop = false;
            groupBox4.Text = "详细日志";
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2, toolStripStatusLabel3, toolStripStatusLabel4, toolStripStatusLabel5, toolStripStatusLabel6 });
            statusStrip1.Location = new Point(0, 805);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 18, 0);
            statusStrip1.Size = new Size(1129, 26);
            statusStrip1.TabIndex = 7;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(52, 20);
            toolStripStatusLabel1.Text = "CPU:0";
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(82, 20);
            toolStripStatusLabel2.Text = "活动进程:0";
            // 
            // toolStripStatusLabel3
            // 
            toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            toolStripStatusLabel3.Size = new Size(82, 20);
            toolStripStatusLabel3.Text = "请求总量:0";
            // 
            // toolStripStatusLabel4
            // 
            toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            toolStripStatusLabel4.Size = new Size(82, 20);
            toolStripStatusLabel4.Text = "提交总量:0";
            // 
            // toolStripStatusLabel5
            // 
            toolStripStatusLabel5.Name = "toolStripStatusLabel5";
            toolStripStatusLabel5.Size = new Size(82, 20);
            toolStripStatusLabel5.Text = "曝光总量:0";
            // 
            // toolStripStatusLabel6
            // 
            toolStripStatusLabel6.Name = "toolStripStatusLabel6";
            toolStripStatusLabel6.Size = new Size(82, 20);
            toolStripStatusLabel6.Text = "点击总量:0";
            // 
            // checkBox_PersistAdx
            // 
            checkBox_PersistAdx.AutoSize = true;
            checkBox_PersistAdx.Location = new Point(581, 309);
            checkBox_PersistAdx.Margin = new Padding(6);
            checkBox_PersistAdx.Name = "checkBox_PersistAdx";
            checkBox_PersistAdx.Size = new Size(136, 24);
            checkBox_PersistAdx.TabIndex = 151;
            checkBox_PersistAdx.Text = "存储广告到本地";
            checkBox_PersistAdx.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1129, 831);
            Controls.Add(groupBox3);
            Controls.Add(groupBox1);
            Controls.Add(groupBox2);
            Controls.Add(groupBox4);
            Controls.Add(statusStrip1);
            Margin = new Padding(5, 4, 5, 4);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "汇川广告-";
            Load += MainForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_IpTtl).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_DspBidPrice).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_PageLoadingTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumCacheCount).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SubResetTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MainResetTimeout).EndInit();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SendSmsTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_Multiple).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_FetchTaskInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumConcurrency).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_UVInterval).EndInit();
            groupBox6.ResumeLayout(false);
            groupBox6.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListView taskInfoListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBox_ProxyIpUrl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox LogTextBox;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_TaskName;
        private System.Windows.Forms.TextBox LogDetailTextBox;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label90;
        private System.Windows.Forms.NumericUpDown numericUpDown_UVInterval;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.NumericUpDown numericUpDown_MaximumConcurrency;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.NumericUpDown numericUpDown_FetchTaskInterval;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox textBox_TaskApiUrl;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown numericUpDown_Multiple;
        private System.Windows.Forms.CheckBox checkBox_IsRealIp;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.TextBox textBox_SmsPhone;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.NumericUpDown numericUpDown_SendSmsTimeout;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox textBox_SmsName;
        private System.Windows.Forms.CheckBox checkBox_SendSms;
        private System.Windows.Forms.CheckBox checkBox_IsCheckIp;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.RadioButton radioButton_UsingRealDev;
        private System.Windows.Forms.RadioButton radioButton_UseSystemDev;
        private System.Windows.Forms.CheckBox checkBox_IsProxyMode;
        private System.Windows.Forms.CheckBox checkBox_IsHiddenMode;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.NumericUpDown numericUpDown_SubResetTimeout;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.NumericUpDown numericUpDown_MainResetTimeout;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.CheckBox checkBox_DisableLoadImage;
        private System.Windows.Forms.CheckBox checkBox_DisableUserCache;
        private RadioButton radioButton_UseLocalDev;
        private TextBox textBox_DevApiUrl;
        private Label label14;
        private CheckBox checkBox_IsDetailLog;
        private LinkLabel linkLabel1;
        private Label label6;
        private Label label8;
        private Label label7;
        private Label label9;
        private Label label5;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
        private ToolStripStatusLabel toolStripStatusLabel4;
        private ToolStripStatusLabel toolStripStatusLabel5;
        private ToolStripStatusLabel toolStripStatusLabel6;
        private CheckBox checkBox_UseCacheJS;
        private CheckBox checkBox_UseCacheCss;
        private CheckBox checkBox_UseCacheVideo;
        private CheckBox checkBox_UseCacheImg;
        private Label label15;
        private NumericUpDown numericUpDown_MaximumCacheCount;
        private Label label16;
        private NumericUpDown numericUpDown_PageLoadingTimeout;
        private Label label24;
        private Label label25;
        private NumericUpDown numericUpDown_DspBidPrice;
        private Label label38;
        private Label label39;
        private NumericUpDown numericUpDown_IpTtl;
        private CheckBox checkBox_UVsTriggerOne;
        private CheckBox checkBox_PersistAdx;
    }
}

