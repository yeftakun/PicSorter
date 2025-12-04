namespace PicSorter
{
    partial class Form1
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
            btnStartSorting = new Button();
            cmbMode = new ComboBox();
            btnClearDestination = new Button();
            btnAddDestination = new Button();
            dgvDestinations = new DataGridView();
            lblDestinations = new Label();
            btnBrowseSource = new Button();
            txtSourceFolder = new TextBox();
            lblSourceFolder = new Label();
            folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialogDest = new FolderBrowserDialog();
            picPreview = new PictureBox();
            lblFileName = new Label();
            lblIndex = new Label();
            lblStatus = new Label();
            progressBar1 = new ProgressBar();
            ShortcutCol = new DataGridViewTextBoxColumn();
            FolderCol = new DataGridViewTextBoxColumn();
            btnContinueFromLog = new Button();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDestinations).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picPreview).BeginInit();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnContinueFromLog);
            groupBox1.Controls.Add(btnStartSorting);
            groupBox1.Controls.Add(cmbMode);
            groupBox1.Controls.Add(btnClearDestination);
            groupBox1.Controls.Add(btnAddDestination);
            groupBox1.Controls.Add(dgvDestinations);
            groupBox1.Controls.Add(lblDestinations);
            groupBox1.Controls.Add(btnBrowseSource);
            groupBox1.Controls.Add(txtSourceFolder);
            groupBox1.Controls.Add(lblSourceFolder);
            groupBox1.Dock = DockStyle.Top;
            groupBox1.Location = new Point(0, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(982, 180);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Setup Sorting";
            // 
            // btnStartSorting
            // 
            btnStartSorting.Location = new Point(830, 105);
            btnStartSorting.Name = "btnStartSorting";
            btnStartSorting.Size = new Size(140, 29);
            btnStartSorting.TabIndex = 8;
            btnStartSorting.Text = "Start Sorting";
            btnStartSorting.UseVisualStyleBackColor = true;
            btnStartSorting.Click += btnStartSorting_Click;
            // 
            // cmbMode
            // 
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.FormattingEnabled = true;
            cmbMode.Items.AddRange(new object[] { "Copy", "Move" });
            cmbMode.Location = new Point(830, 71);
            cmbMode.Name = "cmbMode";
            cmbMode.Size = new Size(140, 28);
            cmbMode.TabIndex = 7;
            // 
            // btnClearDestination
            // 
            btnClearDestination.Location = new Point(527, 105);
            btnClearDestination.Name = "btnClearDestination";
            btnClearDestination.Size = new Size(94, 29);
            btnClearDestination.TabIndex = 6;
            btnClearDestination.Text = "Clear";
            btnClearDestination.UseVisualStyleBackColor = true;
            btnClearDestination.Click += btnClearDestination_Click;
            // 
            // btnAddDestination
            // 
            btnAddDestination.Location = new Point(527, 70);
            btnAddDestination.Name = "btnAddDestination";
            btnAddDestination.Size = new Size(94, 29);
            btnAddDestination.TabIndex = 5;
            btnAddDestination.Text = "Add Folder";
            btnAddDestination.UseVisualStyleBackColor = true;
            btnAddDestination.Click += btnAddDestination_Click;
            // 
            // dgvDestinations
            // 
            dgvDestinations.AllowUserToAddRows = false;
            dgvDestinations.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDestinations.Columns.AddRange(new DataGridViewColumn[] { ShortcutCol, FolderCol });
            dgvDestinations.Location = new Point(158, 70);
            dgvDestinations.Name = "dgvDestinations";
            dgvDestinations.ReadOnly = true;
            dgvDestinations.RowHeadersWidth = 51;
            dgvDestinations.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDestinations.Size = new Size(350, 90);
            dgvDestinations.TabIndex = 4;
            // 
            // lblDestinations
            // 
            lblDestinations.AutoSize = true;
            lblDestinations.Location = new Point(12, 70);
            lblDestinations.Name = "lblDestinations";
            lblDestinations.Size = new Size(140, 20);
            lblDestinations.TabIndex = 3;
            lblDestinations.Text = "Destination Folders:";
            lblDestinations.Click += label1_Click;
            // 
            // btnBrowseSource
            // 
            btnBrowseSource.Location = new Point(635, 31);
            btnBrowseSource.Name = "btnBrowseSource";
            btnBrowseSource.Size = new Size(94, 29);
            btnBrowseSource.TabIndex = 2;
            btnBrowseSource.Text = "Browse...";
            btnBrowseSource.UseVisualStyleBackColor = true;
            btnBrowseSource.Click += btnBrowseSource_Click;
            // 
            // txtSourceFolder
            // 
            txtSourceFolder.Location = new Point(121, 31);
            txtSourceFolder.Name = "txtSourceFolder";
            txtSourceFolder.ReadOnly = true;
            txtSourceFolder.Size = new Size(500, 27);
            txtSourceFolder.TabIndex = 1;
            // 
            // lblSourceFolder
            // 
            lblSourceFolder.AutoSize = true;
            lblSourceFolder.Location = new Point(12, 34);
            lblSourceFolder.Name = "lblSourceFolder";
            lblSourceFolder.Size = new Size(103, 20);
            lblSourceFolder.TabIndex = 0;
            lblSourceFolder.Text = "Source Folder:";
            // 
            // folderBrowserDialog1
            // 
            folderBrowserDialog1.Description = "Pilih folder sumber yang berisi foto/video";
            // 
            // folderBrowserDialogDest
            // 
            folderBrowserDialogDest.Description = "Pilih folder tujuan";
            // 
            // picPreview
            // 
            picPreview.Dock = DockStyle.Top;
            picPreview.Location = new Point(0, 180);
            picPreview.Name = "picPreview";
            picPreview.Size = new Size(982, 350);
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.TabIndex = 1;
            picPreview.TabStop = false;
            // 
            // lblFileName
            // 
            lblFileName.AutoSize = true;
            lblFileName.Location = new Point(65, 540);
            lblFileName.Name = "lblFileName";
            lblFileName.Size = new Size(15, 20);
            lblFileName.TabIndex = 2;
            lblFileName.Text = "-";
            // 
            // lblIndex
            // 
            lblIndex.AutoSize = true;
            lblIndex.Location = new Point(12, 540);
            lblIndex.Name = "lblIndex";
            lblIndex.Size = new Size(39, 20);
            lblIndex.TabIndex = 3;
            lblIndex.Text = "0 / 0";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(12, 571);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(81, 20);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Status: Idle";
            // 
            // progressBar1
            // 
            progressBar1.Dock = DockStyle.Bottom;
            progressBar1.Location = new Point(0, 643);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(982, 10);
            progressBar1.TabIndex = 5;
            // 
            // ShortcutCol
            // 
            ShortcutCol.HeaderText = "Shortcut";
            ShortcutCol.MinimumWidth = 6;
            ShortcutCol.Name = "ShortcutCol";
            ShortcutCol.ReadOnly = true;
            ShortcutCol.Width = 76;
            // 
            // FolderCol
            // 
            FolderCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            FolderCol.HeaderText = "Folder Path";
            FolderCol.MinimumWidth = 6;
            FolderCol.Name = "FolderCol";
            FolderCol.ReadOnly = true;
            // 
            // btnContinueFromLog
            // 
            btnContinueFromLog.Location = new Point(735, 30);
            btnContinueFromLog.Name = "btnContinueFromLog";
            btnContinueFromLog.Size = new Size(153, 29);
            btnContinueFromLog.TabIndex = 9;
            btnContinueFromLog.Text = "Continue from log...";
            btnContinueFromLog.UseVisualStyleBackColor = true;
            btnContinueFromLog.Click += btnContinueFromLog_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(982, 653);
            Controls.Add(progressBar1);
            Controls.Add(lblStatus);
            Controls.Add(lblIndex);
            Controls.Add(lblFileName);
            Controls.Add(picPreview);
            Controls.Add(groupBox1);
            KeyPreview = true;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Photo Sorter";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDestinations).EndInit();
            ((System.ComponentModel.ISupportInitialize)picPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private Button btnBrowseSource;
        private TextBox txtSourceFolder;
        private Label lblSourceFolder;
        private FolderBrowserDialog folderBrowserDialog1;
        private Label lblDestinations;
        private Button btnClearDestination;
        private Button btnAddDestination;
        private DataGridView dgvDestinations;
        private FolderBrowserDialog folderBrowserDialogDest;
        private PictureBox picPreview;
        private Label lblFileName;
        private Label lblIndex;
        private Label lblStatus;
        private ProgressBar progressBar1;
        private ComboBox cmbMode;
        private Button btnStartSorting;
        private DataGridViewTextBoxColumn ShortcutCol;
        private DataGridViewTextBoxColumn FolderCol;
        private Button btnContinueFromLog;
    }
}
