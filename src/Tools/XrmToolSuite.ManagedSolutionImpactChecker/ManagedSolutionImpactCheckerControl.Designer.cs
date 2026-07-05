namespace XrmToolSuite.ManagedSolutionImpactChecker
{
    partial class ManagedSolutionImpactCheckerControl
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
            this.tslSolution = new System.Windows.Forms.ToolStripLabel();
            this.cboSolution = new System.Windows.Forms.ToolStripComboBox();
            this.tsbRefreshSolutions = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tslPath = new System.Windows.Forms.ToolStripLabel();
            this.cboPath = new System.Windows.Forms.ToolStripComboBox();
            this.tsbAnalyze = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tssSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.grdFindings = new System.Windows.Forms.DataGridView();
            this.colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colComponent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecommendation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtSummary = new System.Windows.Forms.TextBox();
            this.lblSummaryHeader = new System.Windows.Forms.Label();
            this.splitBottom = new System.Windows.Forms.SplitContainer();
            this.pnlChecklist = new System.Windows.Forms.Panel();
            this.lstChecklist = new System.Windows.Forms.ListBox();
            this.lblChecklistHeader = new System.Windows.Forms.Label();
            this.pnlRollback = new System.Windows.Forms.Panel();
            this.lstRollback = new System.Windows.Forms.ListBox();
            this.lblRollbackHeader = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).BeginInit();
            this.splitBottom.Panel1.SuspendLayout();
            this.splitBottom.Panel2.SuspendLayout();
            this.splitBottom.SuspendLayout();
            this.pnlChecklist.SuspendLayout();
            this.pnlRollback.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tslSolution,
                this.cboSolution,
                this.tsbRefreshSolutions,
                this.tssSeparator1,
                this.tslPath,
                this.cboPath,
                this.tsbAnalyze,
                this.tssSeparator2,
                this.tsbExport,
                this.tssSeparator3,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(960, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tslSolution
            //
            this.tslSolution.Name = "tslSolution";
            this.tslSolution.Text = "Managed solution:";
            //
            // cboSolution
            //
            this.cboSolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSolution.Name = "cboSolution";
            this.cboSolution.Size = new System.Drawing.Size(260, 25);
            this.cboSolution.ToolTipText = "The managed solution whose import/upgrade impact will be analyzed";
            //
            // tsbRefreshSolutions
            //
            this.tsbRefreshSolutions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRefreshSolutions.Name = "tsbRefreshSolutions";
            this.tsbRefreshSolutions.Text = "Refresh solutions";
            this.tsbRefreshSolutions.ToolTipText = "Load the managed solutions from the connected environment";
            this.tsbRefreshSolutions.Click += new System.EventHandler(this.tsbRefreshSolutions_Click);
            //
            // tssSeparator1
            //
            this.tssSeparator1.Name = "tssSeparator1";
            //
            // tslPath
            //
            this.tslPath.Name = "tslPath";
            this.tslPath.Text = "Deployment path:";
            //
            // cboPath
            //
            this.cboPath.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPath.Name = "cboPath";
            this.cboPath.Size = new System.Drawing.Size(110, 25);
            this.cboPath.ToolTipText = "Upgrade deletes missing components; Update/Patch do not; Holding is a staged upgrade";
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze impact";
            this.tsbAnalyze.ToolTipText = "Analyze the selected solution's layering / upgrade / patch / delete impact (read-only)";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
            //
            // tssSeparator2
            //
            this.tssSeparator2.Name = "tssSeparator2";
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsmExportExcel,
                this.tsmExportPdf,
                this.tsmExportJson,
                this.tsmExportHtml});
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export";
            this.tsbExport.Enabled = false;
            //
            // tsmExportExcel
            //
            this.tsmExportExcel.Name = "tsmExportExcel";
            this.tsmExportExcel.Text = "Excel (*.xlsx)";
            this.tsmExportExcel.Click += new System.EventHandler(this.tsmExportExcel_Click);
            //
            // tsmExportPdf
            //
            this.tsmExportPdf.Name = "tsmExportPdf";
            this.tsmExportPdf.Text = "PDF (*.pdf)";
            this.tsmExportPdf.Click += new System.EventHandler(this.tsmExportPdf_Click);
            //
            // tsmExportJson
            //
            this.tsmExportJson.Name = "tsmExportJson";
            this.tsmExportJson.Text = "JSON (CI-friendly)";
            this.tsmExportJson.Click += new System.EventHandler(this.tsmExportJson_Click);
            //
            // tsmExportHtml
            //
            this.tsmExportHtml.Name = "tsmExportHtml";
            this.tsmExportHtml.Text = "HTML (CAB report)";
            this.tsmExportHtml.Click += new System.EventHandler(this.tsmExportHtml_Click);
            //
            // tssSeparator3
            //
            this.tssSeparator3.Name = "tssSeparator3";
            //
            // tsbClose
            //
            this.tsbClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Text = "Close";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            //
            // splitMain
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 25);
            this.splitMain.Name = "splitMain";
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitMain.Panel1.Controls.Add(this.pnlTop);
            this.splitMain.Panel1MinSize = 160;
            this.splitMain.Panel2.Controls.Add(this.splitBottom);
            this.splitMain.Panel2MinSize = 120;
            this.splitMain.Size = new System.Drawing.Size(960, 575);
            this.splitMain.SplitterDistance = 360;
            this.splitMain.TabIndex = 1;
            //
            // pnlTop
            //
            this.pnlTop.Controls.Add(this.grdFindings);
            this.pnlTop.Controls.Add(this.txtSummary);
            this.pnlTop.Controls.Add(this.lblSummaryHeader);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTop.Name = "pnlTop";
            //
            // grdFindings
            //
            this.grdFindings.AllowUserToAddRows = false;
            this.grdFindings.AllowUserToDeleteRows = false;
            this.grdFindings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdFindings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdFindings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colSeverity,
                this.colCategory,
                this.colComponent,
                this.colRecommendation});
            this.grdFindings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdFindings.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdFindings.MultiSelect = false;
            this.grdFindings.Name = "grdFindings";
            this.grdFindings.ReadOnly = true;
            this.grdFindings.RowHeadersVisible = false;
            this.grdFindings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdFindings.TabIndex = 2;
            //
            // colSeverity
            //
            this.colSeverity.HeaderText = "Severity";
            this.colSeverity.Name = "colSeverity";
            this.colSeverity.FillWeight = 12F;
            //
            // colCategory
            //
            this.colCategory.HeaderText = "Category";
            this.colCategory.Name = "colCategory";
            this.colCategory.FillWeight = 14F;
            //
            // colComponent
            //
            this.colComponent.HeaderText = "Component";
            this.colComponent.Name = "colComponent";
            this.colComponent.FillWeight = 26F;
            //
            // colRecommendation
            //
            this.colRecommendation.HeaderText = "Recommendation";
            this.colRecommendation.Name = "colRecommendation";
            this.colRecommendation.FillWeight = 48F;
            //
            // txtSummary
            //
            this.txtSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSummary.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtSummary.Height = 96;
            this.txtSummary.Multiline = true;
            this.txtSummary.Name = "txtSummary";
            this.txtSummary.ReadOnly = true;
            this.txtSummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSummary.TabIndex = 1;
            //
            // lblSummaryHeader
            //
            this.lblSummaryHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSummaryHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSummaryHeader.Height = 20;
            this.lblSummaryHeader.Name = "lblSummaryHeader";
            this.lblSummaryHeader.Text = "Impact summary — pick a managed solution and a deployment path, then Analyze impact";
            //
            // splitBottom
            //
            this.splitBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitBottom.Location = new System.Drawing.Point(0, 0);
            this.splitBottom.Name = "splitBottom";
            this.splitBottom.Panel1.Controls.Add(this.pnlChecklist);
            this.splitBottom.Panel2.Controls.Add(this.pnlRollback);
            this.splitBottom.Size = new System.Drawing.Size(960, 211);
            this.splitBottom.SplitterDistance = 480;
            this.splitBottom.TabIndex = 0;
            //
            // pnlChecklist
            //
            this.pnlChecklist.Controls.Add(this.lstChecklist);
            this.pnlChecklist.Controls.Add(this.lblChecklistHeader);
            this.pnlChecklist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlChecklist.Name = "pnlChecklist";
            //
            // lstChecklist
            //
            this.lstChecklist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstChecklist.HorizontalScrollbar = true;
            this.lstChecklist.IntegralHeight = false;
            this.lstChecklist.Name = "lstChecklist";
            this.lstChecklist.TabIndex = 1;
            //
            // lblChecklistHeader
            //
            this.lblChecklistHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblChecklistHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblChecklistHeader.Height = 20;
            this.lblChecklistHeader.Name = "lblChecklistHeader";
            this.lblChecklistHeader.Text = "Pre-upgrade checklist";
            //
            // pnlRollback
            //
            this.pnlRollback.Controls.Add(this.lstRollback);
            this.pnlRollback.Controls.Add(this.lblRollbackHeader);
            this.pnlRollback.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRollback.Name = "pnlRollback";
            //
            // lstRollback
            //
            this.lstRollback.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstRollback.HorizontalScrollbar = true;
            this.lstRollback.IntegralHeight = false;
            this.lstRollback.Name = "lstRollback";
            this.lstRollback.TabIndex = 1;
            //
            // lblRollbackHeader
            //
            this.lblRollbackHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRollbackHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRollbackHeader.Height = 20;
            this.lblRollbackHeader.Name = "lblRollbackHeader";
            this.lblRollbackHeader.Text = "Rollback guidance";
            //
            // ManagedSolutionImpactCheckerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.toolStrip);
            this.Name = "ManagedSolutionImpactCheckerControl";
            this.Size = new System.Drawing.Size(960, 600);
            this.Load += new System.EventHandler(this.ManagedSolutionImpactCheckerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.splitBottom.Panel1.ResumeLayout(false);
            this.splitBottom.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).EndInit();
            this.splitBottom.ResumeLayout(false);
            this.pnlChecklist.ResumeLayout(false);
            this.pnlRollback.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripLabel tslSolution;
        private System.Windows.Forms.ToolStripComboBox cboSolution;
        private System.Windows.Forms.ToolStripButton tsbRefreshSolutions;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripLabel tslPath;
        private System.Windows.Forms.ToolStripComboBox cboPath;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem tsmExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmExportPdf;
        private System.Windows.Forms.ToolStripMenuItem tsmExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmExportHtml;
        private System.Windows.Forms.ToolStripSeparator tssSeparator3;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.DataGridView grdFindings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCategory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colComponent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecommendation;
        private System.Windows.Forms.TextBox txtSummary;
        private System.Windows.Forms.Label lblSummaryHeader;
        private System.Windows.Forms.SplitContainer splitBottom;
        private System.Windows.Forms.Panel pnlChecklist;
        private System.Windows.Forms.ListBox lstChecklist;
        private System.Windows.Forms.Label lblChecklistHeader;
        private System.Windows.Forms.Panel pnlRollback;
        private System.Windows.Forms.ListBox lstRollback;
        private System.Windows.Forms.Label lblRollbackHeader;
    }
}
