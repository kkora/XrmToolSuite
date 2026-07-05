namespace XrmToolSuite.FormPerformanceAnalyzer
{
    partial class FormPerformanceAnalyzerControl
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
            this.tsbSelectTables = new System.Windows.Forms.ToolStripButton();
            this.tslScope = new System.Windows.Forms.ToolStripLabel();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbAnalyze = new System.Windows.Forms.ToolStripButton();
            this.tsbCompare = new System.Windows.Forms.ToolStripButton();
            this.tsbSettings = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tssSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.grdForms = new System.Windows.Forms.DataGridView();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colForm = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEntity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colState = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFields = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTabs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSubgrids = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colControls = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScripts = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRules = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtSummary = new System.Windows.Forms.TextBox();
            this.lblSummaryHeader = new System.Windows.Forms.Label();
            this.splitDetails = new System.Windows.Forms.SplitContainer();
            this.pnlMetrics = new System.Windows.Forms.Panel();
            this.grdMetrics = new System.Windows.Forms.DataGridView();
            this.colMetric = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colContribution = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblMetricsHeader = new System.Windows.Forms.Label();
            this.pnlRecs = new System.Windows.Forms.Panel();
            this.grdRecs = new System.Windows.Forms.DataGridView();
            this.colImpact = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEffort = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecommendation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTriggeredBy = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblRecsHeader = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdForms)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitDetails)).BeginInit();
            this.splitDetails.Panel1.SuspendLayout();
            this.splitDetails.Panel2.SuspendLayout();
            this.splitDetails.SuspendLayout();
            this.pnlMetrics.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdMetrics)).BeginInit();
            this.pnlRecs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdRecs)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbSelectTables,
                this.tslScope,
                this.tssSeparator1,
                this.tsbAnalyze,
                this.tsbCompare,
                this.tsbSettings,
                this.tssSeparator2,
                this.tsbExport,
                this.tssSeparator3,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(980, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbSelectTables
            //
            this.tsbSelectTables.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSelectTables.Name = "tsbSelectTables";
            this.tsbSelectTables.Text = "Select tables…";
            this.tsbSelectTables.ToolTipText = "Pick the tables to scope the scan (leave empty to analyze every main form)";
            this.tsbSelectTables.Click += new System.EventHandler(this.tsbSelectTables_Click);
            //
            // tslScope
            //
            this.tslScope.Name = "tslScope";
            this.tslScope.Text = "Scope: all tables";
            //
            // tssSeparator1
            //
            this.tssSeparator1.Name = "tssSeparator1";
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze forms";
            this.tsbAnalyze.ToolTipText = "Retrieve and score every main form in scope";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
            //
            // tsbCompare
            //
            this.tsbCompare.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCompare.Name = "tsbCompare";
            this.tsbCompare.Text = "Compare…";
            this.tsbCompare.ToolTipText = "Select exactly two forms in the grid, then compare their metrics side by side";
            this.tsbCompare.Click += new System.EventHandler(this.tsbCompare_Click);
            //
            // tsbSettings
            //
            this.tsbSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSettings.Name = "tsbSettings";
            this.tsbSettings.Text = "Score settings…";
            this.tsbSettings.ToolTipText = "Edit the scoring weights and band thresholds (reset to defaults available)";
            this.tsbSettings.Click += new System.EventHandler(this.tsbSettings_Click);
            //
            // tssSeparator2
            //
            this.tssSeparator2.Name = "tssSeparator2";
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsmExportCsv,
                this.tsmExportHtml});
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export";
            this.tsbExport.Enabled = false;
            //
            // tsmExportCsv
            //
            this.tsmExportCsv.Name = "tsmExportCsv";
            this.tsmExportCsv.Text = "CSV (forms + metrics)";
            this.tsmExportCsv.Click += new System.EventHandler(this.tsmExportCsv_Click);
            //
            // tsmExportHtml
            //
            this.tsmExportHtml.Name = "tsmExportHtml";
            this.tsmExportHtml.Text = "HTML report";
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
            this.splitMain.Panel2.Controls.Add(this.splitDetails);
            this.splitMain.Size = new System.Drawing.Size(980, 575);
            this.splitMain.SplitterDistance = 320;
            this.splitMain.TabIndex = 1;
            //
            // pnlTop
            //
            this.pnlTop.Controls.Add(this.grdForms);
            this.pnlTop.Controls.Add(this.txtSummary);
            this.pnlTop.Controls.Add(this.lblSummaryHeader);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTop.Name = "pnlTop";
            //
            // grdForms
            //
            this.grdForms.AllowUserToAddRows = false;
            this.grdForms.AllowUserToDeleteRows = false;
            this.grdForms.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdForms.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdForms.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colScore,
                this.colBand,
                this.colForm,
                this.colEntity,
                this.colState,
                this.colFields,
                this.colTabs,
                this.colSubgrids,
                this.colControls,
                this.colScripts,
                this.colRules});
            this.grdForms.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdForms.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdForms.Name = "grdForms";
            this.grdForms.ReadOnly = true;
            this.grdForms.RowHeadersVisible = false;
            this.grdForms.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdForms.TabIndex = 2;
            this.grdForms.SelectionChanged += new System.EventHandler(this.grdForms_SelectionChanged);
            //
            // colScore
            //
            this.colScore.HeaderText = "Score";
            this.colScore.Name = "colScore";
            this.colScore.FillWeight = 8F;
            //
            // colBand
            //
            this.colBand.HeaderText = "Band";
            this.colBand.Name = "colBand";
            this.colBand.FillWeight = 11F;
            //
            // colForm
            //
            this.colForm.HeaderText = "Form";
            this.colForm.Name = "colForm";
            this.colForm.FillWeight = 26F;
            //
            // colEntity
            //
            this.colEntity.HeaderText = "Table";
            this.colEntity.Name = "colEntity";
            this.colEntity.FillWeight = 16F;
            //
            // colState
            //
            this.colState.HeaderText = "State";
            this.colState.Name = "colState";
            this.colState.FillWeight = 10F;
            //
            // colFields
            //
            this.colFields.HeaderText = "Fields";
            this.colFields.Name = "colFields";
            this.colFields.FillWeight = 8F;
            //
            // colTabs
            //
            this.colTabs.HeaderText = "Tabs";
            this.colTabs.Name = "colTabs";
            this.colTabs.FillWeight = 7F;
            //
            // colSubgrids
            //
            this.colSubgrids.HeaderText = "Subgrids";
            this.colSubgrids.Name = "colSubgrids";
            this.colSubgrids.FillWeight = 9F;
            //
            // colControls
            //
            this.colControls.HeaderText = "Custom";
            this.colControls.Name = "colControls";
            this.colControls.FillWeight = 8F;
            //
            // colScripts
            //
            this.colScripts.HeaderText = "Scripts";
            this.colScripts.Name = "colScripts";
            this.colScripts.FillWeight = 8F;
            //
            // colRules
            //
            this.colRules.HeaderText = "Rules";
            this.colRules.Name = "colRules";
            this.colRules.FillWeight = 8F;
            //
            // txtSummary
            //
            this.txtSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSummary.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtSummary.Height = 92;
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
            this.lblSummaryHeader.Text = "Band distribution & top-10 heaviest — analyze forms to populate";
            //
            // splitDetails
            //
            this.splitDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitDetails.Location = new System.Drawing.Point(0, 0);
            this.splitDetails.Name = "splitDetails";
            this.splitDetails.Panel1.Controls.Add(this.pnlMetrics);
            this.splitDetails.Panel2.Controls.Add(this.pnlRecs);
            this.splitDetails.Size = new System.Drawing.Size(980, 251);
            this.splitDetails.SplitterDistance = 420;
            this.splitDetails.TabIndex = 0;
            //
            // pnlMetrics
            //
            this.pnlMetrics.Controls.Add(this.grdMetrics);
            this.pnlMetrics.Controls.Add(this.lblMetricsHeader);
            this.pnlMetrics.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMetrics.Name = "pnlMetrics";
            //
            // grdMetrics
            //
            this.grdMetrics.AllowUserToAddRows = false;
            this.grdMetrics.AllowUserToDeleteRows = false;
            this.grdMetrics.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdMetrics.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdMetrics.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colMetric,
                this.colValue,
                this.colContribution});
            this.grdMetrics.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdMetrics.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdMetrics.MultiSelect = false;
            this.grdMetrics.Name = "grdMetrics";
            this.grdMetrics.ReadOnly = true;
            this.grdMetrics.RowHeadersVisible = false;
            this.grdMetrics.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdMetrics.TabIndex = 1;
            //
            // colMetric
            //
            this.colMetric.HeaderText = "Metric";
            this.colMetric.Name = "colMetric";
            this.colMetric.FillWeight = 55F;
            //
            // colValue
            //
            this.colValue.HeaderText = "Value";
            this.colValue.Name = "colValue";
            this.colValue.FillWeight = 20F;
            //
            // colContribution
            //
            this.colContribution.HeaderText = "Contribution";
            this.colContribution.Name = "colContribution";
            this.colContribution.FillWeight = 25F;
            //
            // lblMetricsHeader
            //
            this.lblMetricsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMetricsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblMetricsHeader.Height = 20;
            this.lblMetricsHeader.Name = "lblMetricsHeader";
            this.lblMetricsHeader.Text = "Metric breakdown for the selected form";
            //
            // pnlRecs
            //
            this.pnlRecs.Controls.Add(this.grdRecs);
            this.pnlRecs.Controls.Add(this.lblRecsHeader);
            this.pnlRecs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRecs.Name = "pnlRecs";
            //
            // grdRecs
            //
            this.grdRecs.AllowUserToAddRows = false;
            this.grdRecs.AllowUserToDeleteRows = false;
            this.grdRecs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdRecs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdRecs.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colImpact,
                this.colEffort,
                this.colRecommendation,
                this.colTriggeredBy});
            this.grdRecs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdRecs.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdRecs.MultiSelect = false;
            this.grdRecs.Name = "grdRecs";
            this.grdRecs.ReadOnly = true;
            this.grdRecs.RowHeadersVisible = false;
            this.grdRecs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdRecs.TabIndex = 1;
            //
            // colImpact
            //
            this.colImpact.HeaderText = "Impact";
            this.colImpact.Name = "colImpact";
            this.colImpact.FillWeight = 15F;
            //
            // colEffort
            //
            this.colEffort.HeaderText = "Effort";
            this.colEffort.Name = "colEffort";
            this.colEffort.FillWeight = 12F;
            //
            // colRecommendation
            //
            this.colRecommendation.HeaderText = "Recommendation";
            this.colRecommendation.Name = "colRecommendation";
            this.colRecommendation.FillWeight = 58F;
            //
            // colTriggeredBy
            //
            this.colTriggeredBy.HeaderText = "Triggered by";
            this.colTriggeredBy.Name = "colTriggeredBy";
            this.colTriggeredBy.FillWeight = 20F;
            //
            // lblRecsHeader
            //
            this.lblRecsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRecsHeader.Height = 20;
            this.lblRecsHeader.Name = "lblRecsHeader";
            this.lblRecsHeader.Text = "Recommendations (sorted by impact)";
            //
            // FormPerformanceAnalyzerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.toolStrip);
            this.Name = "FormPerformanceAnalyzerControl";
            this.Size = new System.Drawing.Size(980, 600);
            this.Load += new System.EventHandler(this.FormPerformanceAnalyzerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdForms)).EndInit();
            this.splitDetails.Panel1.ResumeLayout(false);
            this.splitDetails.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitDetails)).EndInit();
            this.splitDetails.ResumeLayout(false);
            this.pnlMetrics.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdMetrics)).EndInit();
            this.pnlRecs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdRecs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbSelectTables;
        private System.Windows.Forms.ToolStripLabel tslScope;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripButton tsbCompare;
        private System.Windows.Forms.ToolStripButton tsbSettings;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem tsmExportCsv;
        private System.Windows.Forms.ToolStripMenuItem tsmExportHtml;
        private System.Windows.Forms.ToolStripSeparator tssSeparator3;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.DataGridView grdForms;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBand;
        private System.Windows.Forms.DataGridViewTextBoxColumn colForm;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEntity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colState;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFields;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTabs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSubgrids;
        private System.Windows.Forms.DataGridViewTextBoxColumn colControls;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScripts;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRules;
        private System.Windows.Forms.TextBox txtSummary;
        private System.Windows.Forms.Label lblSummaryHeader;
        private System.Windows.Forms.SplitContainer splitDetails;
        private System.Windows.Forms.Panel pnlMetrics;
        private System.Windows.Forms.DataGridView grdMetrics;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMetric;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn colContribution;
        private System.Windows.Forms.Label lblMetricsHeader;
        private System.Windows.Forms.Panel pnlRecs;
        private System.Windows.Forms.DataGridView grdRecs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colImpact;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEffort;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecommendation;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTriggeredBy;
        private System.Windows.Forms.Label lblRecsHeader;
    }
}
