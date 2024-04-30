namespace WindowsFormsLinphoneV
{
    partial class Form2
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.richTextBoxLinPhoneState = new System.Windows.Forms.RichTextBox();
            this.btnCallPhone = new System.Windows.Forms.Button();
            this.tbPhone = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxLoginInfo = new System.Windows.Forms.TextBox();
            this.btnUnregistration = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(35, 137);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(60, 34);
            this.button1.TabIndex = 0;
            this.button1.Text = "注册";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(440, 135);
            this.button3.Margin = new System.Windows.Forms.Padding(2);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(50, 36);
            this.button3.TabIndex = 2;
            this.button3.Text = "挂断";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(364, 135);
            this.button4.Margin = new System.Windows.Forms.Padding(2);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(54, 36);
            this.button4.TabIndex = 3;
            this.button4.Text = "接听";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(40, 481);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(468, 172);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(285, 135);
            this.button5.Margin = new System.Windows.Forms.Padding(2);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(65, 36);
            this.button5.TabIndex = 5;
            this.button5.Text = "视频呼叫";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(35, 180);
            this.button6.Margin = new System.Windows.Forms.Padding(2);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(64, 30);
            this.button6.TabIndex = 6;
            this.button6.Text = "退出";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.richTextBoxLinPhoneState);
            this.groupBox1.Location = new System.Drawing.Point(37, 225);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(471, 251);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "消息列表";
            // 
            // richTextBoxLinPhoneState
            // 
            this.richTextBoxLinPhoneState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxLinPhoneState.Location = new System.Drawing.Point(3, 17);
            this.richTextBoxLinPhoneState.Name = "richTextBoxLinPhoneState";
            this.richTextBoxLinPhoneState.Size = new System.Drawing.Size(465, 231);
            this.richTextBoxLinPhoneState.TabIndex = 0;
            this.richTextBoxLinPhoneState.Text = "";
            // 
            // btnCallPhone
            // 
            this.btnCallPhone.Location = new System.Drawing.Point(199, 135);
            this.btnCallPhone.Margin = new System.Windows.Forms.Padding(2);
            this.btnCallPhone.Name = "btnCallPhone";
            this.btnCallPhone.Size = new System.Drawing.Size(64, 36);
            this.btnCallPhone.TabIndex = 8;
            this.btnCallPhone.Text = "拨打电话";
            this.btnCallPhone.UseVisualStyleBackColor = true;
            this.btnCallPhone.Click += new System.EventHandler(this.btnCallPhone_Click);
            // 
            // tbPhone
            // 
            this.tbPhone.Location = new System.Drawing.Point(35, 104);
            this.tbPhone.Name = "tbPhone";
            this.tbPhone.Size = new System.Drawing.Size(473, 21);
            this.tbPhone.TabIndex = 9;
            this.tbPhone.Text = "1008";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(35, 86);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 12);
            this.label1.TabIndex = 10;
            this.label1.Text = "电话号码：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(33, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(449, 12);
            this.label2.TabIndex = 11;
            this.label2.Text = "配置参数填写顺序：CSserverIP,userName,passWord,sipPort。值之间英文逗号分隔";
            // 
            // textBoxLoginInfo
            // 
            this.textBoxLoginInfo.Location = new System.Drawing.Point(35, 51);
            this.textBoxLoginInfo.Name = "textBoxLoginInfo";
            this.textBoxLoginInfo.Size = new System.Drawing.Size(471, 21);
            this.textBoxLoginInfo.TabIndex = 12;
            this.textBoxLoginInfo.Text = "210.51.10.231,1004,1004,5060";
            // 
            // btnUnregistration
            // 
            this.btnUnregistration.Location = new System.Drawing.Point(117, 135);
            this.btnUnregistration.Margin = new System.Windows.Forms.Padding(2);
            this.btnUnregistration.Name = "btnUnregistration";
            this.btnUnregistration.Size = new System.Drawing.Size(60, 36);
            this.btnUnregistration.TabIndex = 14;
            this.btnUnregistration.Text = "注销";
            this.btnUnregistration.UseVisualStyleBackColor = true;
            this.btnUnregistration.Click += new System.EventHandler(this.btnUnregistration_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(569, 674);
            this.Controls.Add(this.btnUnregistration);
            this.Controls.Add(this.textBoxLoginInfo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbPhone);
            this.Controls.Add(this.btnCallPhone);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Linphone Test";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox richTextBoxLinPhoneState;
        private System.Windows.Forms.Button btnCallPhone;
        private System.Windows.Forms.TextBox tbPhone;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxLoginInfo;
        private System.Windows.Forms.Button btnUnregistration;
    }
}

