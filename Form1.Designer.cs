namespace AWSGlaicer
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.outDir = new System.Windows.Forms.FolderBrowserDialog();
            this.Copy = new System.Windows.Forms.Button();
            this.fileLogFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.SetSlogFile = new System.Windows.Forms.Button();
            this.doCompress = new System.Windows.Forms.CheckBox();
            this.Restore = new System.Windows.Forms.Button();
            this.GlaiceirUpload = new System.Windows.Forms.Button();
            this.RetryGalcier = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxAWSVaultName = new System.Windows.Forms.TextBox();
            this.tarSizeUpDouwnControl = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ArchiveDirectory = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.textBoxAWSDynamoTableName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.tarSizeUpDouwnControl)).BeginInit();
            this.SuspendLayout();
            // 
            // Copy
            // 
            this.Copy.Location = new System.Drawing.Point(107, 192);
            this.Copy.Name = "Copy";
            this.Copy.Size = new System.Drawing.Size(143, 23);
            this.Copy.TabIndex = 1;
            this.Copy.Text = "Initiate Bulk Transfer";
            this.Copy.UseVisualStyleBackColor = true;
            this.Copy.Click += new System.EventHandler(this.Copy_Click);
            // 
            // fileLogFileDialog
            // 
            this.fileLogFileDialog.FileName = "slogFileDialog";
            this.fileLogFileDialog.Filter = "files|*.csv";
            // 
            // SetSlogFile
            // 
            this.SetSlogFile.Location = new System.Drawing.Point(13, 21);
            this.SetSlogFile.Name = "SetSlogFile";
            this.SetSlogFile.Size = new System.Drawing.Size(104, 23);
            this.SetSlogFile.TabIndex = 2;
            this.SetSlogFile.Text = "Set FileLog";
            this.SetSlogFile.UseVisualStyleBackColor = true;
            this.SetSlogFile.Click += new System.EventHandler(this.SetSlogFile_Click);
            // 
            // doCompress
            // 
            this.doCompress.AutoSize = true;
            this.doCompress.Location = new System.Drawing.Point(20, 65);
            this.doCompress.Name = "doCompress";
            this.doCompress.Size = new System.Drawing.Size(117, 17);
            this.doCompress.TabIndex = 3;
            this.doCompress.Text = "Use TPL Compress";
            this.doCompress.UseVisualStyleBackColor = true;
            // 
            // Restore
            // 
            this.Restore.Location = new System.Drawing.Point(107, 254);
            this.Restore.Name = "Restore";
            this.Restore.Size = new System.Drawing.Size(143, 23);
            this.Restore.TabIndex = 5;
            this.Restore.Text = "Restore";
            this.Restore.UseVisualStyleBackColor = true;
            this.Restore.Click += new System.EventHandler(this.Restore_Click);
            // 
            // GlaiceirUpload
            // 
            this.GlaiceirUpload.Location = new System.Drawing.Point(109, 221);
            this.GlaiceirUpload.Name = "GlaiceirUpload";
            this.GlaiceirUpload.Size = new System.Drawing.Size(143, 23);
            this.GlaiceirUpload.TabIndex = 6;
            this.GlaiceirUpload.Text = "GlacierUpoad Single File";
            this.GlaiceirUpload.UseVisualStyleBackColor = true;
            this.GlaiceirUpload.Click += new System.EventHandler(this.GlaiceirUpload_Click);
            // 
            // RetryGalcier
            // 
            this.RetryGalcier.Location = new System.Drawing.Point(20, 461);
            this.RetryGalcier.Name = "RetryGalcier";
            this.RetryGalcier.Size = new System.Drawing.Size(75, 23);
            this.RetryGalcier.TabIndex = 7;
            this.RetryGalcier.Text = "RetryGalcier";
            this.RetryGalcier.UseVisualStyleBackColor = true;
            this.RetryGalcier.Click += new System.EventHandler(this.RetryGalcier_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(258, 114);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "AWS Vault Name";
            // 
            // textBoxAWSVaultName
            // 
            this.textBoxAWSVaultName.Location = new System.Drawing.Point(107, 109);
            this.textBoxAWSVaultName.Name = "textBoxAWSVaultName";
            this.textBoxAWSVaultName.Size = new System.Drawing.Size(145, 20);
            this.textBoxAWSVaultName.TabIndex = 10;
            this.textBoxAWSVaultName.TextChanged += new System.EventHandler(this.textBoxAWSVaultName_TextChanged);
            // 
            // tarSizeUpDouwnControl
            // 
            this.tarSizeUpDouwnControl.Location = new System.Drawing.Point(183, 64);
            this.tarSizeUpDouwnControl.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.tarSizeUpDouwnControl.Name = "tarSizeUpDouwnControl";
            this.tarSizeUpDouwnControl.Size = new System.Drawing.Size(35, 20);
            this.tarSizeUpDouwnControl.TabIndex = 11;
            this.tarSizeUpDouwnControl.ThousandsSeparator = true;
            this.tarSizeUpDouwnControl.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            this.tarSizeUpDouwnControl.ValueChanged += new System.EventHandler(this.tarSizeUpDouwnControl_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(226, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(189, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "TAR Size (GB) For Incremental Upload";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(106, 341);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(213, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "Tars And P-Zips Directory Before Uploading";
            // 
            // ArchiveDirectory
            // 
            this.ArchiveDirectory.Location = new System.Drawing.Point(109, 368);
            this.ArchiveDirectory.Name = "ArchiveDirectory";
            this.ArchiveDirectory.Size = new System.Drawing.Size(111, 23);
            this.ArchiveDirectory.TabIndex = 14;
            this.ArchiveDirectory.Text = "Archive Directory";
            this.ArchiveDirectory.UseVisualStyleBackColor = true;
            this.ArchiveDirectory.Click += new System.EventHandler(this.ArchiveDirectory_Click);
            // 
            // textBoxAWSDynamoTableName
            // 
            this.textBoxAWSDynamoTableName.Location = new System.Drawing.Point(105, 151);
            this.textBoxAWSDynamoTableName.Name = "textBoxAWSDynamoTableName";
            this.textBoxAWSDynamoTableName.Size = new System.Drawing.Size(145, 20);
            this.textBoxAWSDynamoTableName.TabIndex = 15;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(256, 154);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(135, 13);
            this.label4.TabIndex = 16;
            this.label4.Text = "AWS Dynamo Table Name";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(122, 27);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(166, 13);
            this.label5.TabIndex = 17;
            this.label5.Text = "csv file :  server,directory,filename";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(519, 562);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxAWSDynamoTableName);
            this.Controls.Add(this.ArchiveDirectory);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tarSizeUpDouwnControl);
            this.Controls.Add(this.textBoxAWSVaultName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.RetryGalcier);
            this.Controls.Add(this.GlaiceirUpload);
            this.Controls.Add(this.Restore);
            this.Controls.Add(this.doCompress);
            this.Controls.Add(this.SetSlogFile);
            this.Controls.Add(this.Copy);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.tarSizeUpDouwnControl)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog outDir;
        private System.Windows.Forms.Button Copy;
        private System.Windows.Forms.OpenFileDialog fileLogFileDialog;
        private System.Windows.Forms.Button SetSlogFile;
        private System.Windows.Forms.CheckBox doCompress;
        private System.Windows.Forms.Button Restore;
        private System.Windows.Forms.Button GlaiceirUpload;
        private System.Windows.Forms.Button RetryGalcier;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxAWSVaultName;
        private System.Windows.Forms.NumericUpDown tarSizeUpDouwnControl;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button ArchiveDirectory;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TextBox textBoxAWSDynamoTableName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
    }
}

