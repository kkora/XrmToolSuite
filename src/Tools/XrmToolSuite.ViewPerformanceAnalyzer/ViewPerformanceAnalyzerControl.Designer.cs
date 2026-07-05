namespace XrmToolSuite.ViewPerformanceAnalyzer
{
    partial class ViewPerformanceAnalyzerControl
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
            this.tslEntity = new System.Windows.Forms.ToolStripLabel();
            this.cboEntity = new System.Windows.Forms.ToolStripComboBox();
            this.tsbRefreshTables = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbIncludePersonal = new System.Windows.Forms.ToolStripButton();
            this.tsbAnalyze = new System.Windows.Forms.ToolStripButton();
            this.tsbTime = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportMarkdown = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.tssSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.grdViews = new System.Windows.Forms.DataGridView();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colView = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEntity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAttrs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCols = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLinks = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtSummary = new System.Windows.Forms.TextBox();
            this.lblSummaryHeader = new System.Windows.Forms.Label();
            this.splitDetails = new System.Windows.Forms.SplitContainer();
            this.pnlFindings = new System.Windows.Forms.Panel();
            this.grdFindings = new System.Windows.Forms.DataGridView();
            this.colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colComponent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecommendation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblFindingsHeader = new System.Windows.Forms.Label();
            this.splitRight = new System.Windows.Forms.SplitContainer();
            this.pnlFetch = new System.Windows.Forms.Panel();
            this.txtFetchXml = new System.Windows.Forms.TextBox();
            this.lblFetchHeader = new System.Windows.Forms.Label();
            this.pnlLayout = new System.Windows.Forms.Panel();
            this.lstLayoutColumns = new System.Windows.Forms.ListBox();
            this.lblLayoutHeader = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdViews)).BeginInit();
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
            this.pnlFetch.SuspendLayout();
            this.pnlLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tslEntity,
                this.cboEntity,
                this.tsbRefreshTables,
                this.tssSeparator1,
                this.tsbIncludePersonal,
                this.tsbAnalyze,
                this.tsbTime,
                this.tssSeparator2,
                this.tsbExport,
                this.tssSeparator3,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(960, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tslEntity
            //
            this.tslEntity.Name = "tslEntity";
            this.tslEntity.Text = "Table:";
            //
            // cboEntity
            //
            this.cboEntity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboEntity.Name = "cboEntity";
            this.cboEntity.Size = new System.Drawing.Size(220, 25);
            this.cboEntity.ToolTipText = "The table whose system (and optionally personal) views will be analyzed";
            //
            // tsbRefreshTables
            //
            this.tsbRefreshTables.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRefreshTables.Name = "tsbRefreshTables";
            this.tsbRefreshTables.Text = "Refresh tables";
            this.tsbRefreshTables.ToolTipText = "Load the list of tables from the connected environment";
            this.tsbRefreshTables.Click += new System.EventHandler(this.tsbRefreshTables_Click);
            //
            // tssSeparator1
            //
            this.tssSeparator1.Name = "tssSeparator1";
            //
            // tsbIncludePersonal
            //
            this.tsbIncludePersonal.CheckOnClick = true;
            this.tsbIncludePersonal.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbIncludePersonal.Name = "tsbIncludePersonal";
            this.tsbIncludePersonal.Text = "Include personal views";
            this.tsbIncludePersonal.ToolTipText = "Also analyze users' personal views (userquery), not just system views";
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze views";
            this.tsbAnalyze.ToolTipText = "Retrieve and score every view for the selected table";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
            //
            // tsbTime
            //
            this.tsbTime.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbTime.Name = "tsbTime";
            this.tsbTime.Text = "Time selected view";
            this.tsbTime.ToolTipText = "Run the selected view's FetchXML read-only (capped) and report elapsed time (opt-in)";
            this.tsbTime.Click += new System.EventHandler(this.tsbTime_Click);
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
            this.tsmExportCsv.Text = "CSV (views)";
            this.tsmExportCsv.Click += new System.EventHandler(this.tsmExportCsv_Click);
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
            this.splitMain.Panel1MinSize = 140;
            this.splitMain.Panel2.Controls.Add(this.splitDetails);
            this.splitMain.Size = new System.Drawing.Size(960, 575);
            this.splitMain.SplitterDistance = 300;
            this.splitMain.TabIndex = 1;
            //
            // pnlTop
            //
            this.pnlTop.Controls.Add(this.grdViews);
            this.pnlTop.Controls.Add(this.txtSummary);
            this.pnlTop.Controls.Add(this.lblSummaryHeader);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTop.Name = "pnlTop";
            //
            // grdViews
            //
            this.grdViews.AllowUserToAddRows = false;
            this.grdViews.AllowUserToDeleteRows = false;
            this.grdViews.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdViews.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdViews.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colScore,
                this.colBand,
                this.colView,
                this.colType,
                this.colEntity,
                this.colAttrs,
                this.colCols,
                this.colLinks});
            this.grdViews.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdViews.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdViews.MultiSelect = false;
            this.grdViews.Name = "grdViews";
            this.grdViews.ReadOnly = true;
            this.grdViews.RowHeadersVisible = false;
            this.grdViews.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdViews.TabIndex = 2;
            this.grdViews.SelectionChanged += new System.EventHandler(this.grdViews_SelectionChanged);
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
            // colView
            //
            this.colView.HeaderText = "View";
            this.colView.Name = "colView";
            this.colView.FillWeight = 32F;
            //
            // colType
            //
            this.colType.HeaderText = "Type";
            this.colType.Name = "colType";
            this.colType.FillWeight = 12F;
            //
            // colEntity
            //
            this.colEntity.HeaderText = "Entity";
            this.colEntity.Name = "colEntity";
            this.colEntity.FillWeight = 18F;
            //
            // colAttrs
            //
            this.colAttrs.HeaderText = "#Attrs";
            this.colAttrs.Name = "colAttrs";
            this.colAttrs.FillWeight = 8F;
            //
            // colCols
            //
            this.colCols.HeaderText = "#Cols";
            this.colCols.Name = "colCols";
            this.colCols.FillWeight = 8F;
            //
            // colLinks
            //
            this.colLinks.HeaderText = "#Links";
            this.colLinks.Name = "colLinks";
            this.colLinks.FillWeight = 8F;
            //
            // txtSummary
            //
            this.txtSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSummary.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtSummary.Height = 90;
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
            this.lblSummaryHeader.Text = "Environment view summary — analyze a table to populate";
            //
            // splitDetails
            //
            this.splitDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitDetails.Location = new System.Drawing.Point(0, 0);
            this.splitDetails.Name = "splitDetails";
            this.splitDetails.Panel1.Controls.Add(this.pnlFindings);
            this.splitDetails.Panel2.Controls.Add(this.splitRight);
            this.splitDetails.Size = new System.Drawing.Size(960, 271);
            this.splitDetails.SplitterDistance = 520;
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
                this.colComponent,
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
            this.colSeverity.FillWeight = 14F;
            //
            // colTitle
            //
            this.colTitle.HeaderText = "Title";
            this.colTitle.Name = "colTitle";
            this.colTitle.FillWeight = 30F;
            //
            // colComponent
            //
            this.colComponent.HeaderText = "Component";
            this.colComponent.Name = "colComponent";
            this.colComponent.FillWeight = 20F;
            //
            // colRecommendation
            //
            this.colRecommendation.HeaderText = "Recommendation";
            this.colRecommendation.Name = "colRecommendation";
            this.colRecommendation.FillWeight = 45F;
            //
            // lblFindingsHeader
            //
            this.lblFindingsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFindingsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFindingsHeader.Height = 20;
            this.lblFindingsHeader.Name = "lblFindingsHeader";
            this.lblFindingsHeader.Text = "Findings for the selected view";
            //
            // splitRight
            //
            this.splitRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRight.Location = new System.Drawing.Point(0, 0);
            this.splitRight.Name = "splitRight";
            this.splitRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitRight.Panel1.Controls.Add(this.pnlFetch);
            this.splitRight.Panel2.Controls.Add(this.pnlLayout);
            this.splitRight.Size = new System.Drawing.Size(436, 271);
            this.splitRight.SplitterDistance = 150;
            this.splitRight.TabIndex = 0;
            //
            // pnlFetch
            //
            this.pnlFetch.Controls.Add(this.txtFetchXml);
            this.pnlFetch.Controls.Add(this.lblFetchHeader);
            this.pnlFetch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlFetch.Name = "pnlFetch";
            //
            // txtFetchXml
            //
            this.txtFetchXml.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFetchXml.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtFetchXml.Multiline = true;
            this.txtFetchXml.Name = "txtFetchXml";
            this.txtFetchXml.ReadOnly = true;
            this.txtFetchXml.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtFetchXml.WordWrap = false;
            this.txtFetchXml.TabIndex = 1;
            //
            // lblFetchHeader
            //
            this.lblFetchHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFetchHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFetchHeader.Height = 20;
            this.lblFetchHeader.Name = "lblFetchHeader";
            this.lblFetchHeader.Text = "FetchXML";
            //
            // pnlLayout
            //
            this.pnlLayout.Controls.Add(this.lstLayoutColumns);
            this.pnlLayout.Controls.Add(this.lblLayoutHeader);
            this.pnlLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlLayout.Name = "pnlLayout";
            //
            // lstLayoutColumns
            //
            this.lstLayoutColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstLayoutColumns.IntegralHeight = false;
            this.lstLayoutColumns.Name = "lstLayoutColumns";
            this.lstLayoutColumns.TabIndex = 1;
            //
            // lblLayoutHeader
            //
            this.lblLayoutHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLayoutHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblLayoutHeader.Height = 20;
            this.lblLayoutHeader.Name = "lblLayoutHeader";
            this.lblLayoutHeader.Text = "Layout columns";
            //
            // ViewPerformanceAnalyzerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.toolStrip);
            this.Name = "ViewPerformanceAnalyzerControl";
            this.Size = new System.Drawing.Size(960, 600);
            this.Load += new System.EventHandler(this.ViewPerformanceAnalyzerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdViews)).EndInit();
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
            this.pnlFetch.ResumeLayout(false);
            this.pnlFetch.PerformLayout();
            this.pnlLayout.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripLabel tslEntity;
        private System.Windows.Forms.ToolStripComboBox cboEntity;
        private System.Windows.Forms.ToolStripButton tsbRefreshTables;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbIncludePersonal;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripButton tsbTime;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem tsmExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmExportPdf;
        private System.Windows.Forms.ToolStripMenuItem tsmExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmExportHtml;
        private System.Windows.Forms.ToolStripMenuItem tsmExportMarkdown;
        private System.Windows.Forms.ToolStripMenuItem tsmExportCsv;
        private System.Windows.Forms.ToolStripSeparator tssSeparator3;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.DataGridView grdViews;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBand;
        private System.Windows.Forms.DataGridViewTextBoxColumn colView;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEntity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAttrs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCols;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLinks;
        private System.Windows.Forms.TextBox txtSummary;
        private System.Windows.Forms.Label lblSummaryHeader;
        private System.Windows.Forms.SplitContainer splitDetails;
        private System.Windows.Forms.Panel pnlFindings;
        private System.Windows.Forms.DataGridView grdFindings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colComponent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecommendation;
        private System.Windows.Forms.Label lblFindingsHeader;
        private System.Windows.Forms.SplitContainer splitRight;
        private System.Windows.Forms.Panel pnlFetch;
        private System.Windows.Forms.TextBox txtFetchXml;
        private System.Windows.Forms.Label lblFetchHeader;
        private System.Windows.Forms.Panel pnlLayout;
        private System.Windows.Forms.ListBox lstLayoutColumns;
        private System.Windows.Forms.Label lblLayoutHeader;
    }
}
