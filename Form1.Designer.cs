using System.Windows.Forms;

namespace ContextPicker
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private Panel panelFolders;

        private Panel panelBottom;
        private Button btnExportPath;
        private Label lblExportPath;
        private Button btnAddFolder;
        private Button btnGenerateContext;
        private Button btnExportCsv;
        private ProgressBar progressBar;
        private Label lblStatus;
        private TextBox txtOutputFile;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelFolders = new Panel();
            panelBottom = new Panel();
            importSection_Button = new Button();
            btnExportPath = new Button();
            lblExportPath = new Label();
            btnAddFolder = new Button();
            btnGenerateContext = new Button();
            btnExportCsv = new Button();
            progressBar = new ProgressBar();
            lblStatus = new Label();
            txtOutputFile = new TextBox();
            panelBottom.SuspendLayout();
            SuspendLayout();
            // 
            // panelFolders
            // 
            panelFolders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelFolders.AutoScroll = true;
            panelFolders.Location = new Point(12, 12);
            panelFolders.Name = "panelFolders";
            panelFolders.Size = new Size(630, 341);
            panelFolders.TabIndex = 0;
            // 
            // panelBottom
            // 
            panelBottom.Controls.Add(importSection_Button);
            panelBottom.Controls.Add(btnExportPath);
            panelBottom.Controls.Add(lblExportPath);
            panelBottom.Controls.Add(btnAddFolder);
            panelBottom.Controls.Add(btnGenerateContext);
            panelBottom.Controls.Add(btnExportCsv);
            panelBottom.Controls.Add(progressBar);
            panelBottom.Controls.Add(lblStatus);
            panelBottom.Controls.Add(txtOutputFile);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Location = new Point(0, 420);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new Size(654, 154);
            panelBottom.TabIndex = 2;
            // 
            // importSection_Button
            // 
            importSection_Button.Location = new Point(10, 81);
            importSection_Button.Name = "importSection_Button";
            importSection_Button.Size = new Size(140, 35);
            importSection_Button.TabIndex = 12;
            importSection_Button.Text = "Import section";
            importSection_Button.UseVisualStyleBackColor = true;
            importSection_Button.Click += importSection_Button_Click;
            // 
            // btnExportPath
            // 
            btnExportPath.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExportPath.Location = new Point(500, 4);
            btnExportPath.Name = "btnExportPath";
            btnExportPath.Size = new Size(140, 37);
            btnExportPath.TabIndex = 2;
            btnExportPath.Text = "Change Export Folder";
            btnExportPath.UseVisualStyleBackColor = true;
            btnExportPath.Click += btnExportPath_Click;
            // 
            // lblExportPath
            // 
            lblExportPath.Location = new Point(9, 10);
            lblExportPath.Name = "lblExportPath";
            lblExportPath.Size = new Size(309, 20);
            lblExportPath.TabIndex = 3;
            lblExportPath.Text = "Export Folder: (none)";
            // 
            // btnAddFolder
            // 
            btnAddFolder.Location = new Point(320, 4);
            btnAddFolder.Name = "btnAddFolder";
            btnAddFolder.Size = new Size(174, 37);
            btnAddFolder.TabIndex = 10;
            btnAddFolder.Text = "Add Another Folder (5 left)";
            btnAddFolder.UseVisualStyleBackColor = true;
            btnAddFolder.Click += btnAddFolder_Click;
            // 
            // btnGenerateContext
            // 
            btnGenerateContext.Location = new Point(10, 40);
            btnGenerateContext.Name = "btnGenerateContext";
            btnGenerateContext.Size = new Size(140, 35);
            btnGenerateContext.TabIndex = 4;
            btnGenerateContext.Text = "Generate Context";
            btnGenerateContext.UseVisualStyleBackColor = true;
            btnGenerateContext.Click += btnGenerateContext_Click;
            // 
            // btnExportCsv
            // 
            btnExportCsv.Location = new Point(156, 80);
            btnExportCsv.Name = "btnExportCsv";
            btnExportCsv.Size = new Size(140, 36);
            btnExportCsv.TabIndex = 5;
            btnExportCsv.Text = "Export Selection";
            btnExportCsv.UseVisualStyleBackColor = true;
            btnExportCsv.Click += btnExportCsv_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(10, 122);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(286, 20);
            progressBar.TabIndex = 6;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(302, 127);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(39, 15);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "Ready";
            // 
            // txtOutputFile
            // 
            txtOutputFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            txtOutputFile.Location = new Point(320, 47);
            txtOutputFile.Name = "txtOutputFile";
            txtOutputFile.ReadOnly = true;
            txtOutputFile.Size = new Size(320, 23);
            txtOutputFile.TabIndex = 8;
            // 
            // Form1
            // 
            ClientSize = new Size(654, 574);
            Controls.Add(panelFolders);
            Controls.Add(panelBottom);
            MinimumSize = new Size(670, 540);
            Name = "Form1";
            Text = "ContextPicker - Multi-Folder Context Exporter";
            panelBottom.ResumeLayout(false);
            panelBottom.PerformLayout();
            ResumeLayout(false);

        }
        private Button importSection_Button;
    }
}
