namespace SerialDebug
{
    partial class AboutBox
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
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
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox));
            TableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            LogoPictureBox = new System.Windows.Forms.PictureBox();
            LabelProductName = new System.Windows.Forms.Label();
            LabelVersion = new System.Windows.Forms.Label();
            LabelCopyright = new System.Windows.Forms.Label();
            LabelCompanyName = new System.Windows.Forms.Label();
            TextBoxDescription = new System.Windows.Forms.TextBox();
            OKButton = new System.Windows.Forms.Button();
            TableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)LogoPictureBox).BeginInit();
            SuspendLayout();
            // 
            // TableLayoutPanel
            // 
            TableLayoutPanel.ColumnCount = 2;
            TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9.59596F));
            TableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 90.40404F));
            TableLayoutPanel.Controls.Add(LogoPictureBox, 0, 0);
            TableLayoutPanel.Controls.Add(LabelProductName, 1, 0);
            TableLayoutPanel.Controls.Add(LabelVersion, 1, 1);
            TableLayoutPanel.Controls.Add(LabelCopyright, 1, 2);
            TableLayoutPanel.Controls.Add(LabelCompanyName, 1, 3);
            TableLayoutPanel.Controls.Add(TextBoxDescription, 1, 4);
            TableLayoutPanel.Controls.Add(OKButton, 1, 5);
            TableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            TableLayoutPanel.Location = new System.Drawing.Point(14, 13);
            TableLayoutPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            TableLayoutPanel.Name = "TableLayoutPanel";
            TableLayoutPanel.RowCount = 6;
            TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7.112971F));
            TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7.112971F));
            TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7.531381F));
            TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7.531381F));
            TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60.25105F));
            TableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            TableLayoutPanel.Size = new System.Drawing.Size(624, 409);
            TableLayoutPanel.TabIndex = 1;
            TableLayoutPanel.Paint += TableLayoutPanel_Paint;
            // 
            // LogoPictureBox
            // 
            LogoPictureBox.Image = (System.Drawing.Image)resources.GetObject("LogoPictureBox.Image");
            LogoPictureBox.Location = new System.Drawing.Point(4, 5);
            LogoPictureBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            LogoPictureBox.Name = "LogoPictureBox";
            TableLayoutPanel.SetRowSpan(LogoPictureBox, 6);
            LogoPictureBox.Size = new System.Drawing.Size(46, 60);
            LogoPictureBox.TabIndex = 0;
            LogoPictureBox.TabStop = false;
            // 
            // LabelProductName
            // 
            LabelProductName.Dock = System.Windows.Forms.DockStyle.Fill;
            LabelProductName.Location = new System.Drawing.Point(68, 0);
            LabelProductName.Margin = new System.Windows.Forms.Padding(9, 0, 4, 0);
            LabelProductName.MaximumSize = new System.Drawing.Size(0, 27);
            LabelProductName.Name = "LabelProductName";
            LabelProductName.Size = new System.Drawing.Size(552, 27);
            LabelProductName.TabIndex = 0;
            LabelProductName.Text = "产品名称";
            LabelProductName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelVersion
            // 
            LabelVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            LabelVersion.Location = new System.Drawing.Point(68, 28);
            LabelVersion.Margin = new System.Windows.Forms.Padding(9, 0, 4, 0);
            LabelVersion.MaximumSize = new System.Drawing.Size(0, 27);
            LabelVersion.Name = "LabelVersion";
            LabelVersion.Size = new System.Drawing.Size(552, 27);
            LabelVersion.TabIndex = 0;
            LabelVersion.Text = "版本";
            LabelVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelCopyright
            // 
            LabelCopyright.Dock = System.Windows.Forms.DockStyle.Fill;
            LabelCopyright.Location = new System.Drawing.Point(68, 56);
            LabelCopyright.Margin = new System.Windows.Forms.Padding(9, 0, 4, 0);
            LabelCopyright.MaximumSize = new System.Drawing.Size(0, 27);
            LabelCopyright.Name = "LabelCopyright";
            LabelCopyright.Size = new System.Drawing.Size(552, 27);
            LabelCopyright.TabIndex = 0;
            LabelCopyright.Text = "版权";
            LabelCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelCompanyName
            // 
            LabelCompanyName.Dock = System.Windows.Forms.DockStyle.Fill;
            LabelCompanyName.Location = new System.Drawing.Point(68, 86);
            LabelCompanyName.Margin = new System.Windows.Forms.Padding(9, 0, 4, 0);
            LabelCompanyName.MaximumSize = new System.Drawing.Size(0, 27);
            LabelCompanyName.Name = "LabelCompanyName";
            LabelCompanyName.Size = new System.Drawing.Size(552, 27);
            LabelCompanyName.TabIndex = 0;
            LabelCompanyName.Text = "公司名称";
            LabelCompanyName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TextBoxDescription
            // 
            TextBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            TextBoxDescription.Location = new System.Drawing.Point(68, 121);
            TextBoxDescription.Margin = new System.Windows.Forms.Padding(9, 5, 4, 5);
            TextBoxDescription.Multiline = true;
            TextBoxDescription.Name = "TextBoxDescription";
            TextBoxDescription.ReadOnly = true;
            TextBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            TextBoxDescription.Size = new System.Drawing.Size(552, 232);
            TextBoxDescription.TabIndex = 0;
            TextBoxDescription.TabStop = false;
            TextBoxDescription.Text = resources.GetString("TextBoxDescription.Text");
            // 
            // OKButton
            // 
            OKButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            OKButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            OKButton.Location = new System.Drawing.Point(508, 366);
            OKButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            OKButton.Name = "OKButton";
            OKButton.Size = new System.Drawing.Size(112, 38);
            OKButton.TabIndex = 0;
            OKButton.Text = "确定(&O)";
            // 
            // AboutBox
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(652, 435);
            Controls.Add(TableLayoutPanel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutBox";
            Padding = new System.Windows.Forms.Padding(14, 13, 14, 13);
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "AboutBox";
            TableLayoutPanel.ResumeLayout(false);
            TableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)LogoPictureBox).EndInit();
            ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.TableLayoutPanel TableLayoutPanel;
        internal System.Windows.Forms.PictureBox LogoPictureBox;
        internal System.Windows.Forms.Label LabelProductName;
        internal System.Windows.Forms.Label LabelVersion;
        internal System.Windows.Forms.Label LabelCopyright;
        internal System.Windows.Forms.Label LabelCompanyName;
        internal System.Windows.Forms.TextBox TextBoxDescription;
        internal System.Windows.Forms.Button OKButton;

    }
}
