namespace XrmToolSuite.FetchXmlPerformanceAnalyzer
{
    partial class FetchXmlPerformanceAnalyzerControl
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
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbLoadView = new System.Windows.Forms.ToolStripButton();
            this.tsbExecute = new System.Windows.Forms.ToolStripButton();
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
            this.txtFetchXml = new System.Windows.Forms.TextBox();
            this.splitResults = new System.Windows.Forms.SplitContainer();
            this.grdFindings = new System.Windows.Forms.DataGridView();
            this.colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colComponent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecommendation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pnlRight = new System.Windows.Forms.Panel();
            this.txtSuggestions = new System.Windows.Forms.TextBox();
            this.lblSuggestionsHeader = new System.Windows.Forms.Label();
            this.txtSummary = new System.Windows.Forms.TextBox();
            this.lblSummaryHeader = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitResults)).BeginInit();
            this.splitResults.Panel1.SuspendLayout();
            this.splitResults.Panel2.SuspendLayout();
            this.splitResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            this.pnlRight.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbAnalyze,
                this.tssSeparator1,
                this.tsbLoadView,
                this.tsbExecute,
                this.tssSeparator2,
                this.tsbExport,
                this.tssSeparator3,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(900, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze";
            this.tsbAnalyze.ToolTipText = "Parse and analyze the FetchXML (no connection required)";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
            //
            // tssSeparator1
            //
            this.tssSeparator1.Name = "tssSeparator1";
            //
            // tsbLoadView
            //
            this.tsbLoadView.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadView.Name = "tsbLoadView";
            this.tsbLoadView.Text = "Load from view...";
            this.tsbLoadView.ToolTipText = "Load FetchXML from a saved system or personal view";
            this.tsbLoadView.Click += new System.EventHandler(this.tsbLoadView_Click);
            //
            // tsbExecute
            //
            this.tsbExecute.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExecute.Name = "tsbExecute";
            this.tsbExecute.Text = "Execute with timing";
            this.tsbExecute.ToolTipText = "Run the query read-only and report elapsed time and row count (opt-in)";
            this.tsbExecute.Click += new System.EventHandler(this.tsbExecute_Click);
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
            this.tsmExportCsv.Text = "CSV (findings)";
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
            //
            // splitMain.Panel1 (FetchXML input)
            //
            this.splitMain.Panel1.Controls.Add(this.txtFetchXml);
            this.splitMain.Panel1MinSize = 90;
            //
            // splitMain.Panel2 (results)
            //
            this.splitMain.Panel2.Controls.Add(this.splitResults);
            this.splitMain.Size = new System.Drawing.Size(900, 575);
            this.splitMain.SplitterDistance = 180;
            this.splitMain.TabIndex = 1;
            //
            // txtFetchXml
            //
            this.txtFetchXml.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFetchXml.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.txtFetchXml.Multiline = true;
            this.txtFetchXml.Name = "txtFetchXml";
            this.txtFetchXml.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtFetchXml.WordWrap = false;
            this.txtFetchXml.TabIndex = 0;
            //
            // splitResults
            //
            this.splitResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitResults.Location = new System.Drawing.Point(0, 0);
            this.splitResults.Name = "splitResults";
            //
            // splitResults.Panel1 (findings grid)
            //
            this.splitResults.Panel1.Controls.Add(this.grdFindings);
            //
            // splitResults.Panel2 (summary + suggestions)
            //
            this.splitResults.Panel2.Controls.Add(this.pnlRight);
            this.splitResults.Panel2MinSize = 220;
            this.splitResults.Size = new System.Drawing.Size(900, 391);
            this.splitResults.SplitterDistance = 560;
            this.splitResults.TabIndex = 0;
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
            this.grdFindings.TabIndex = 0;
            //
            // colSeverity
            //
            this.colSeverity.HeaderText = "Severity";
            this.colSeverity.Name = "colSeverity";
            this.colSeverity.FillWeight = 15F;
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
            // pnlRight
            //
            this.pnlRight.Controls.Add(this.txtSuggestions);
            this.pnlRight.Controls.Add(this.lblSuggestionsHeader);
            this.pnlRight.Controls.Add(this.txtSummary);
            this.pnlRight.Controls.Add(this.lblSummaryHeader);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Padding = new System.Windows.Forms.Padding(6);
            this.pnlRight.TabIndex = 0;
            //
            // lblSummaryHeader
            //
            this.lblSummaryHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSummaryHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSummaryHeader.Name = "lblSummaryHeader";
            this.lblSummaryHeader.Height = 20;
            this.lblSummaryHeader.Text = "Query shape & cost estimate";
            //
            // txtSummary
            //
            this.txtSummary.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSummary.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtSummary.Height = 170;
            this.txtSummary.Multiline = true;
            this.txtSummary.Name = "txtSummary";
            this.txtSummary.ReadOnly = true;
            this.txtSummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSummary.TabIndex = 1;
            //
            // lblSuggestionsHeader
            //
            this.lblSuggestionsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSuggestionsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSuggestionsHeader.Name = "lblSuggestionsHeader";
            this.lblSuggestionsHeader.Height = 20;
            this.lblSuggestionsHeader.Text = "Suggestions";
            //
            // txtSuggestions
            //
            this.txtSuggestions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSuggestions.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtSuggestions.Multiline = true;
            this.txtSuggestions.Name = "txtSuggestions";
            this.txtSuggestions.ReadOnly = true;
            this.txtSuggestions.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSuggestions.TabIndex = 3;
            //
            // FetchXmlPerformanceAnalyzerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.toolStrip);
            this.Name = "FetchXmlPerformanceAnalyzerControl";
            this.Size = new System.Drawing.Size(900, 600);
            this.Load += new System.EventHandler(this.FetchXmlPerformanceAnalyzerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel1.PerformLayout();
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.splitResults.Panel1.ResumeLayout(false);
            this.splitResults.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitResults)).EndInit();
            this.splitResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.pnlRight.ResumeLayout(false);
            this.pnlRight.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbLoadView;
        private System.Windows.Forms.ToolStripButton tsbExecute;
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
        private System.Windows.Forms.TextBox txtFetchXml;
        private System.Windows.Forms.SplitContainer splitResults;
        private System.Windows.Forms.DataGridView grdFindings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colComponent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecommendation;
        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.TextBox txtSuggestions;
        private System.Windows.Forms.Label lblSuggestionsHeader;
        private System.Windows.Forms.TextBox txtSummary;
        private System.Windows.Forms.Label lblSummaryHeader;
    }
}
