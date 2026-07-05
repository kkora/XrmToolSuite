namespace XrmToolSuite.SolutionDocumentationGenerator
{
    partial class SolutionDocumentationGeneratorControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.tsbLoadSolutions = new System.Windows.Forms.ToolStripButton();
            this.tssSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbGenerate = new System.Windows.Forms.ToolStripButton();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tssSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.tslPreview = new System.Windows.Forms.ToolStripLabel();
            this.tscPreview = new System.Windows.Forms.ToolStripComboBox();
            this.tssSep3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.split = new System.Windows.Forms.SplitContainer();
            this.pnlConfig = new System.Windows.Forms.Panel();
            this.grpBranding = new System.Windows.Forms.GroupBox();
            this.txtPublisher = new System.Windows.Forms.TextBox();
            this.lblPublisher = new System.Windows.Forms.Label();
            this.txtLogoUrl = new System.Windows.Forms.TextBox();
            this.lblLogoUrl = new System.Windows.Forms.Label();
            this.txtBrandHeader = new System.Windows.Forms.TextBox();
            this.lblBrandHeader = new System.Windows.Forms.Label();
            this.clbSections = new System.Windows.Forms.CheckedListBox();
            this.lblSections = new System.Windows.Forms.Label();
            this.cboMode = new System.Windows.Forms.ComboBox();
            this.lblMode = new System.Windows.Forms.Label();
            this.cboSolution = new System.Windows.Forms.ComboBox();
            this.lblSolution = new System.Windows.Forms.Label();
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.txtPreview = new System.Windows.Forms.TextBox();
            this.lblStats = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            this.pnlConfig.SuspendLayout();
            this.grpBranding.SuspendLayout();
            this.pnlPreview.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoadSolutions,
                this.tssSep1,
                this.tsbGenerate,
                this.tsbExport,
                this.tssSep2,
                this.tslPreview,
                this.tscPreview,
                this.tssSep3,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(960, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoadSolutions
            //
            this.tsbLoadSolutions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadSolutions.Name = "tsbLoadSolutions";
            this.tsbLoadSolutions.Size = new System.Drawing.Size(96, 22);
            this.tsbLoadSolutions.Text = "Load solutions";
            //
            // tsbGenerate
            //
            this.tsbGenerate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbGenerate.Name = "tsbGenerate";
            this.tsbGenerate.Size = new System.Drawing.Size(66, 22);
            this.tsbGenerate.Text = "Generate";
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.Enabled = false;
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Size = new System.Drawing.Size(60, 22);
            this.tsbExport.Text = "Export";
            //
            // tslPreview
            //
            this.tslPreview.Name = "tslPreview";
            this.tslPreview.Size = new System.Drawing.Size(52, 22);
            this.tslPreview.Text = "Preview:";
            //
            // tscPreview
            //
            this.tscPreview.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscPreview.Name = "tscPreview";
            this.tscPreview.Size = new System.Drawing.Size(140, 25);
            //
            // tsbClose
            //
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Size = new System.Drawing.Size(45, 22);
            this.tsbClose.Text = "Close";
            //
            // split
            //
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.split.Location = new System.Drawing.Point(0, 25);
            this.split.Name = "split";
            //
            // split.Panel1
            //
            this.split.Panel1.Controls.Add(this.pnlConfig);
            this.split.Panel1MinSize = 280;
            //
            // split.Panel2
            //
            this.split.Panel2.Controls.Add(this.pnlPreview);
            this.split.Size = new System.Drawing.Size(960, 575);
            this.split.SplitterDistance = 320;
            this.split.TabIndex = 1;
            //
            // pnlConfig
            //
            this.pnlConfig.Controls.Add(this.grpBranding);
            this.pnlConfig.Controls.Add(this.clbSections);
            this.pnlConfig.Controls.Add(this.lblSections);
            this.pnlConfig.Controls.Add(this.cboMode);
            this.pnlConfig.Controls.Add(this.lblMode);
            this.pnlConfig.Controls.Add(this.cboSolution);
            this.pnlConfig.Controls.Add(this.lblSolution);
            this.pnlConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlConfig.Name = "pnlConfig";
            this.pnlConfig.Padding = new System.Windows.Forms.Padding(10);
            this.pnlConfig.Size = new System.Drawing.Size(320, 575);
            this.pnlConfig.TabIndex = 0;
            //
            // lblSolution
            //
            this.lblSolution.AutoSize = true;
            this.lblSolution.Location = new System.Drawing.Point(10, 12);
            this.lblSolution.Name = "lblSolution";
            this.lblSolution.Size = new System.Drawing.Size(49, 13);
            this.lblSolution.Text = "Solution";
            //
            // cboSolution
            //
            this.cboSolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSolution.Location = new System.Drawing.Point(13, 28);
            this.cboSolution.Name = "cboSolution";
            this.cboSolution.Size = new System.Drawing.Size(290, 21);
            this.cboSolution.TabIndex = 0;
            //
            // lblMode
            //
            this.lblMode.AutoSize = true;
            this.lblMode.Location = new System.Drawing.Point(10, 60);
            this.lblMode.Name = "lblMode";
            this.lblMode.Size = new System.Drawing.Size(105, 13);
            this.lblMode.Text = "Documentation mode";
            //
            // cboMode
            //
            this.cboMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMode.Location = new System.Drawing.Point(13, 76);
            this.cboMode.Name = "cboMode";
            this.cboMode.Size = new System.Drawing.Size(290, 21);
            this.cboMode.TabIndex = 1;
            //
            // lblSections
            //
            this.lblSections.AutoSize = true;
            this.lblSections.Location = new System.Drawing.Point(10, 108);
            this.lblSections.Name = "lblSections";
            this.lblSections.Size = new System.Drawing.Size(96, 13);
            this.lblSections.Text = "Sections to include";
            //
            // clbSections
            //
            this.clbSections.CheckOnClick = true;
            this.clbSections.IntegralHeight = false;
            this.clbSections.Location = new System.Drawing.Point(13, 124);
            this.clbSections.Name = "clbSections";
            this.clbSections.Size = new System.Drawing.Size(290, 200);
            this.clbSections.TabIndex = 2;
            //
            // grpBranding
            //
            this.grpBranding.Controls.Add(this.txtPublisher);
            this.grpBranding.Controls.Add(this.lblPublisher);
            this.grpBranding.Controls.Add(this.txtLogoUrl);
            this.grpBranding.Controls.Add(this.lblLogoUrl);
            this.grpBranding.Controls.Add(this.txtBrandHeader);
            this.grpBranding.Controls.Add(this.lblBrandHeader);
            this.grpBranding.Location = new System.Drawing.Point(13, 334);
            this.grpBranding.Name = "grpBranding";
            this.grpBranding.Size = new System.Drawing.Size(290, 180);
            this.grpBranding.TabIndex = 3;
            this.grpBranding.TabStop = false;
            this.grpBranding.Text = "Branding";
            //
            // lblBrandHeader
            //
            this.lblBrandHeader.AutoSize = true;
            this.lblBrandHeader.Location = new System.Drawing.Point(10, 24);
            this.lblBrandHeader.Name = "lblBrandHeader";
            this.lblBrandHeader.Size = new System.Drawing.Size(72, 13);
            this.lblBrandHeader.Text = "Header line";
            //
            // txtBrandHeader
            //
            this.txtBrandHeader.Location = new System.Drawing.Point(13, 40);
            this.txtBrandHeader.Name = "txtBrandHeader";
            this.txtBrandHeader.Size = new System.Drawing.Size(264, 20);
            this.txtBrandHeader.TabIndex = 0;
            //
            // lblLogoUrl
            //
            this.lblLogoUrl.AutoSize = true;
            this.lblLogoUrl.Location = new System.Drawing.Point(10, 68);
            this.lblLogoUrl.Name = "lblLogoUrl";
            this.lblLogoUrl.Size = new System.Drawing.Size(55, 13);
            this.lblLogoUrl.Text = "Logo URL (HTML)";
            //
            // txtLogoUrl
            //
            this.txtLogoUrl.Location = new System.Drawing.Point(13, 84);
            this.txtLogoUrl.Name = "txtLogoUrl";
            this.txtLogoUrl.Size = new System.Drawing.Size(264, 20);
            this.txtLogoUrl.TabIndex = 1;
            //
            // lblPublisher
            //
            this.lblPublisher.AutoSize = true;
            this.lblPublisher.Location = new System.Drawing.Point(10, 112);
            this.lblPublisher.Name = "lblPublisher";
            this.lblPublisher.Size = new System.Drawing.Size(55, 13);
            this.lblPublisher.Text = "Publisher (override)";
            //
            // txtPublisher
            //
            this.txtPublisher.Location = new System.Drawing.Point(13, 128);
            this.txtPublisher.Name = "txtPublisher";
            this.txtPublisher.Size = new System.Drawing.Size(264, 20);
            this.txtPublisher.TabIndex = 2;
            //
            // pnlPreview
            //
            this.pnlPreview.Controls.Add(this.txtPreview);
            this.pnlPreview.Controls.Add(this.lblStats);
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new System.Drawing.Size(636, 575);
            this.pnlPreview.TabIndex = 0;
            //
            // txtPreview
            //
            this.txtPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPreview.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtPreview.Multiline = true;
            this.txtPreview.Name = "txtPreview";
            this.txtPreview.ReadOnly = true;
            this.txtPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtPreview.WordWrap = false;
            this.txtPreview.TabIndex = 1;
            //
            // lblStats
            //
            this.lblStats.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStats.Name = "lblStats";
            this.lblStats.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.lblStats.Size = new System.Drawing.Size(636, 22);
            this.lblStats.TabIndex = 0;
            this.lblStats.Text = "Load solutions, pick one, choose a mode and sections, then Generate.";
            //
            // SolutionDocumentationGeneratorControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.split);
            this.Controls.Add(this.toolStrip);
            this.Name = "SolutionDocumentationGeneratorControl";
            this.Size = new System.Drawing.Size(960, 600);
            this.Load += new System.EventHandler(this.SolutionDocumentationGeneratorControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            this.pnlConfig.ResumeLayout(false);
            this.pnlConfig.PerformLayout();
            this.grpBranding.ResumeLayout(false);
            this.grpBranding.PerformLayout();
            this.pnlPreview.ResumeLayout(false);
            this.pnlPreview.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoadSolutions;
        private System.Windows.Forms.ToolStripSeparator tssSep1;
        private System.Windows.Forms.ToolStripButton tsbGenerate;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripSeparator tssSep2;
        private System.Windows.Forms.ToolStripLabel tslPreview;
        private System.Windows.Forms.ToolStripComboBox tscPreview;
        private System.Windows.Forms.ToolStripSeparator tssSep3;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.Panel pnlConfig;
        private System.Windows.Forms.GroupBox grpBranding;
        private System.Windows.Forms.TextBox txtPublisher;
        private System.Windows.Forms.Label lblPublisher;
        private System.Windows.Forms.TextBox txtLogoUrl;
        private System.Windows.Forms.Label lblLogoUrl;
        private System.Windows.Forms.TextBox txtBrandHeader;
        private System.Windows.Forms.Label lblBrandHeader;
        private System.Windows.Forms.CheckedListBox clbSections;
        private System.Windows.Forms.Label lblSections;
        private System.Windows.Forms.ComboBox cboMode;
        private System.Windows.Forms.Label lblMode;
        private System.Windows.Forms.ComboBox cboSolution;
        private System.Windows.Forms.Label lblSolution;
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.TextBox txtPreview;
        private System.Windows.Forms.Label lblStats;
    }
}
