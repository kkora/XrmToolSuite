namespace XrmToolSuite.JavaScriptPerformanceAnalyzer
{
    partial class JavaScriptPerformanceAnalyzerControl
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
            this.tsbAnalyze = new System.Windows.Forms.ToolStripButton();
            this.tsbCustomOnly = new System.Windows.Forms.ToolStripButton();
            this.tsbExclusions = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tslSearch = new System.Windows.Forms.ToolStripLabel();
            this.txtSearch = new System.Windows.Forms.ToolStripTextBox();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportMarkdown = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.grdScripts = new System.Windows.Forms.DataGridView();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScript = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFindings = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtSummary = new System.Windows.Forms.TextBox();
            this.lblSummaryHeader = new System.Windows.Forms.Label();
            this.splitDetails = new System.Windows.Forms.SplitContainer();
            this.pnlFindings = new System.Windows.Forms.Panel();
            this.grdFindings = new System.Windows.Forms.DataGridView();
            this.colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLine = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colContext = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colConfidence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecommendation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblFindingsHeader = new System.Windows.Forms.Label();
            this.splitRight = new System.Windows.Forms.SplitContainer();
            this.pnlCode = new System.Windows.Forms.Panel();
            this.txtCode = new System.Windows.Forms.TextBox();
            this.lblCodeHeader = new System.Windows.Forms.Label();
            this.pnlUsage = new System.Windows.Forms.Panel();
            this.lstUsage = new System.Windows.Forms.ListBox();
            this.lblUsageHeader = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdScripts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitDetails)).BeginInit();
            this.splitDetails.Panel1.SuspendLayout();
            this.splitDetails.Panel2.SuspendLayout();
            this.splitDetails.SuspendLayout();
            this.pnlFindings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).BeginInit();
            this.splitRight.Panel1.SuspendLayout();
            this.splitRight.Panel2.SuspendLayout();
            this.splitRight.SuspendLayout();
            this.pnlCode.SuspendLayout();
            this.pnlUsage.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbAnalyze,
                this.tsbCustomOnly,
                this.tsbExclusions,
                this.tssSeparator1,
                this.tslSearch,
                this.txtSearch,
                this.tssSeparator2,
                this.tsbExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(960, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze web resources";
            this.tsbAnalyze.ToolTipText = "Retrieve and statically analyze every JScript web resource in the connected environment";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
            //
            // tsbCustomOnly
            //
            this.tsbCustomOnly.CheckOnClick = true;
            this.tsbCustomOnly.Checked = true;
            this.tsbCustomOnly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCustomOnly.Name = "tsbCustomOnly";
            this.tsbCustomOnly.Text = "Custom only";
            this.tsbCustomOnly.ToolTipText = "Scan only unmanaged (custom) web resources — skip Microsoft/managed system libraries. Re-run the analysis to apply.";
            //
            // tsbExclusions
            //
            this.tsbExclusions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExclusions.Name = "tsbExclusions";
            this.tsbExclusions.Text = "Exclusions…";
            this.tsbExclusions.ToolTipText = "Exclude web resources whose name starts with the given prefixes (comma-separated)";
            this.tsbExclusions.Click += new System.EventHandler(this.tsbExclusions_Click);
            //
            // tssSeparator1
            //
            this.tssSeparator1.Name = "tssSeparator1";
            //
            // tslSearch
            //
            this.tslSearch.Name = "tslSearch";
            this.tslSearch.Text = "Search code:";
            //
            // txtSearch
            //
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(180, 25);
            this.txtSearch.ToolTipText = "Filter scripts to those whose code contains this text";
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
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
                this.tsmExportHtml,
                this.tsmExportMarkdown,
                this.tsmExportCsv});
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
            this.tsmExportHtml.Text = "HTML report";
            this.tsmExportHtml.Click += new System.EventHandler(this.tsmExportHtml_Click);
            //
            // tsmExportMarkdown
            //
            this.tsmExportMarkdown.Name = "tsmExportMarkdown";
            this.tsmExportMarkdown.Text = "Markdown";
            this.tsmExportMarkdown.Click += new System.EventHandler(this.tsmExportMarkdown_Click);
            //
            // tsmExportCsv
            //
            this.tsmExportCsv.Name = "tsmExportCsv";
            this.tsmExportCsv.Text = "CSV (scripts)";
            this.tsmExportCsv.Click += new System.EventHandler(this.tsmExportCsv_Click);
            //
            // splitMain
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 25);
            this.splitMain.Name = "splitMain";
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitMain.Panel1.Controls.Add(this.pnlTop);
            this.splitMain.Panel1MinSize = 140;
            this.splitMain.Panel2.Controls.Add(this.splitDetails);
            this.splitMain.Size = new System.Drawing.Size(960, 575);
            this.splitMain.SplitterDistance = 300;
            this.splitMain.TabIndex = 1;
            //
            // pnlTop
            //
            this.pnlTop.Controls.Add(this.grdScripts);
            this.pnlTop.Controls.Add(this.txtSummary);
            this.pnlTop.Controls.Add(this.lblSummaryHeader);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTop.Name = "pnlTop";
            //
            // grdScripts
            //
            this.grdScripts.AllowUserToAddRows = false;
            this.grdScripts.AllowUserToDeleteRows = false;
            this.grdScripts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdScripts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdScripts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colScore,
                this.colBand,
                this.colScript,
                this.colSize,
                this.colFindings});
            this.grdScripts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdScripts.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdScripts.MultiSelect = false;
            this.grdScripts.Name = "grdScripts";
            this.grdScripts.ReadOnly = true;
            this.grdScripts.RowHeadersVisible = false;
            this.grdScripts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdScripts.TabIndex = 2;
            this.grdScripts.SelectionChanged += new System.EventHandler(this.grdScripts_SelectionChanged);
            //
            // colScore
            //
            this.colScore.HeaderText = "Score";
            this.colScore.Name = "colScore";
            this.colScore.FillWeight = 10F;
            //
            // colBand
            //
            this.colBand.HeaderText = "Band";
            this.colBand.Name = "colBand";
            this.colBand.FillWeight = 12F;
            //
            // colScript
            //
            this.colScript.HeaderText = "Script";
            this.colScript.Name = "colScript";
            this.colScript.FillWeight = 50F;
            //
            // colSize
            //
            this.colSize.HeaderText = "Size";
            this.colSize.Name = "colSize";
            this.colSize.FillWeight = 14F;
            //
            // colFindings
            //
            this.colFindings.HeaderText = "#Findings";
            this.colFindings.Name = "colFindings";
            this.colFindings.FillWeight = 12F;
            //
            // txtSummary
            //
            this.txtSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSummary.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtSummary.Height = 74;
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
            this.lblSummaryHeader.Text = "JavaScript dashboard — click 'Analyze web resources' to populate";
            //
            // splitDetails
            //
            this.splitDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitDetails.Location = new System.Drawing.Point(0, 0);
            this.splitDetails.Name = "splitDetails";
            this.splitDetails.Panel1.Controls.Add(this.pnlFindings);
            this.splitDetails.Panel2.Controls.Add(this.splitRight);
            this.splitDetails.Size = new System.Drawing.Size(960, 271);
            this.splitDetails.SplitterDistance = 560;
            this.splitDetails.TabIndex = 0;
            //
            // pnlFindings
            //
            this.pnlFindings.Controls.Add(this.grdFindings);
            this.pnlFindings.Controls.Add(this.lblFindingsHeader);
            this.pnlFindings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFindings.Name = "pnlFindings";
            //
            // grdFindings
            //
            this.grdFindings.AllowUserToAddRows = false;
            this.grdFindings.AllowUserToDeleteRows = false;
            this.grdFindings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdFindings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdFindings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colSeverity,
                this.colTitle,
                this.colLine,
                this.colContext,
                this.colConfidence,
                this.colRecommendation});
            this.grdFindings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdFindings.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdFindings.MultiSelect = false;
            this.grdFindings.Name = "grdFindings";
            this.grdFindings.ReadOnly = true;
            this.grdFindings.RowHeadersVisible = false;
            this.grdFindings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdFindings.TabIndex = 1;
            //
            // colSeverity
            //
            this.colSeverity.HeaderText = "Severity";
            this.colSeverity.Name = "colSeverity";
            this.colSeverity.FillWeight = 12F;
            //
            // colTitle
            //
            this.colTitle.HeaderText = "Title";
            this.colTitle.Name = "colTitle";
            this.colTitle.FillWeight = 24F;
            //
            // colLine
            //
            this.colLine.HeaderText = "Line";
            this.colLine.Name = "colLine";
            this.colLine.FillWeight = 7F;
            //
            // colContext
            //
            this.colContext.HeaderText = "Context";
            this.colContext.Name = "colContext";
            this.colContext.FillWeight = 30F;
            //
            // colConfidence
            //
            this.colConfidence.HeaderText = "Confidence";
            this.colConfidence.Name = "colConfidence";
            this.colConfidence.FillWeight = 20F;
            //
            // colRecommendation
            //
            this.colRecommendation.HeaderText = "Recommendation";
            this.colRecommendation.Name = "colRecommendation";
            this.colRecommendation.FillWeight = 34F;
            //
            // lblFindingsHeader
            //
            this.lblFindingsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFindingsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFindingsHeader.Height = 20;
            this.lblFindingsHeader.Name = "lblFindingsHeader";
            this.lblFindingsHeader.Text = "Findings for the selected script";
            //
            // splitRight
            //
            this.splitRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRight.Location = new System.Drawing.Point(0, 0);
            this.splitRight.Name = "splitRight";
            this.splitRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitRight.Panel1.Controls.Add(this.pnlCode);
            this.splitRight.Panel2.Controls.Add(this.pnlUsage);
            this.splitRight.Size = new System.Drawing.Size(396, 271);
            this.splitRight.SplitterDistance = 165;
            this.splitRight.TabIndex = 0;
            //
            // pnlCode
            //
            this.pnlCode.Controls.Add(this.txtCode);
            this.pnlCode.Controls.Add(this.lblCodeHeader);
            this.pnlCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCode.Name = "pnlCode";
            //
            // txtCode
            //
            this.txtCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtCode.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtCode.Multiline = true;
            this.txtCode.Name = "txtCode";
            this.txtCode.ReadOnly = true;
            this.txtCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtCode.WordWrap = false;
            this.txtCode.TabIndex = 1;
            //
            // lblCodeHeader
            //
            this.lblCodeHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCodeHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCodeHeader.Height = 20;
            this.lblCodeHeader.Name = "lblCodeHeader";
            this.lblCodeHeader.Text = "Code (read-only)";
            //
            // pnlUsage
            //
            this.pnlUsage.Controls.Add(this.lstUsage);
            this.pnlUsage.Controls.Add(this.lblUsageHeader);
            this.pnlUsage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlUsage.Name = "pnlUsage";
            //
            // lstUsage
            //
            this.lstUsage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstUsage.IntegralHeight = false;
            this.lstUsage.Name = "lstUsage";
            this.lstUsage.TabIndex = 1;
            //
            // lblUsageHeader
            //
            this.lblUsageHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblUsageHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUsageHeader.Height = 20;
            this.lblUsageHeader.Name = "lblUsageHeader";
            this.lblUsageHeader.Text = "Form / event usage";
            //
            // JavaScriptPerformanceAnalyzerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.toolStrip);
            this.Name = "JavaScriptPerformanceAnalyzerControl";
            this.Size = new System.Drawing.Size(960, 600);
            this.Load += new System.EventHandler(this.JavaScriptPerformanceAnalyzerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdScripts)).EndInit();
            this.splitDetails.Panel1.ResumeLayout(false);
            this.splitDetails.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitDetails)).EndInit();
            this.splitDetails.ResumeLayout(false);
            this.pnlFindings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.splitRight.Panel1.ResumeLayout(false);
            this.splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).EndInit();
            this.splitRight.ResumeLayout(false);
            this.pnlCode.ResumeLayout(false);
            this.pnlCode.PerformLayout();
            this.pnlUsage.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripButton tsbCustomOnly;
        private System.Windows.Forms.ToolStripButton tsbExclusions;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripLabel tslSearch;
        private System.Windows.Forms.ToolStripTextBox txtSearch;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem tsmExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmExportPdf;
        private System.Windows.Forms.ToolStripMenuItem tsmExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmExportHtml;
        private System.Windows.Forms.ToolStripMenuItem tsmExportMarkdown;
        private System.Windows.Forms.ToolStripMenuItem tsmExportCsv;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.DataGridView grdScripts;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBand;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScript;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFindings;
        private System.Windows.Forms.TextBox txtSummary;
        private System.Windows.Forms.Label lblSummaryHeader;
        private System.Windows.Forms.SplitContainer splitDetails;
        private System.Windows.Forms.Panel pnlFindings;
        private System.Windows.Forms.DataGridView grdFindings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLine;
        private System.Windows.Forms.DataGridViewTextBoxColumn colContext;
        private System.Windows.Forms.DataGridViewTextBoxColumn colConfidence;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecommendation;
        private System.Windows.Forms.Label lblFindingsHeader;
        private System.Windows.Forms.SplitContainer splitRight;
        private System.Windows.Forms.Panel pnlCode;
        private System.Windows.Forms.TextBox txtCode;
        private System.Windows.Forms.Label lblCodeHeader;
        private System.Windows.Forms.Panel pnlUsage;
        private System.Windows.Forms.ListBox lstUsage;
        private System.Windows.Forms.Label lblUsageHeader;
    }
}
