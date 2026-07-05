namespace XrmToolSuite.PortalHealthAnalyzer
{
    partial class PortalHealthAnalyzerControl
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
            this.tsbLoadWebsites = new System.Windows.Forms.ToolStripButton();
            this.tslWebsite = new System.Windows.Forms.ToolStripLabel();
            this.cboWebsite = new System.Windows.Forms.ToolStripComboBox();
            this.tsbAnalyze = new System.Windows.Forms.ToolStripButton();
            this.tss1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportWord = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.split = new System.Windows.Forms.SplitContainer();
            this.lblScore = new System.Windows.Forms.Label();
            this.txtSummary = new System.Windows.Forms.TextBox();
            this.grdFindings = new System.Windows.Forms.DataGridView();
            this.colCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecord = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecommendation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoadWebsites,
                this.tslWebsite,
                this.cboWebsite,
                this.tsbAnalyze,
                this.tss1,
                this.tsbExport,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(900, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoadWebsites
            //
            this.tsbLoadWebsites.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadWebsites.Name = "tsbLoadWebsites";
            this.tsbLoadWebsites.Text = "Load websites";
            this.tsbLoadWebsites.Click += new System.EventHandler(this.tsbLoadWebsites_Click);
            //
            // tslWebsite
            //
            this.tslWebsite.Name = "tslWebsite";
            this.tslWebsite.Text = "Website:";
            //
            // cboWebsite
            //
            this.cboWebsite.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboWebsite.Name = "cboWebsite";
            this.cboWebsite.Size = new System.Drawing.Size(280, 25);
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
            //
            // tss1
            //
            this.tss1.Name = "tss1";
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsmExportExcel,
                this.tsmExportPdf,
                this.tsmExportWord,
                this.tsmExportJson,
                this.tsmExportHtml,
                this.tsmExportCsv});
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export";
            this.tsbExport.Enabled = false;
            //
            // tsmExportExcel
            //
            this.tsmExportExcel.Name = "tsmExportExcel";
            this.tsmExportExcel.Text = "Excel (*.xlsx)…";
            this.tsmExportExcel.Click += new System.EventHandler(this.tsmExportExcel_Click);
            //
            // tsmExportPdf
            //
            this.tsmExportPdf.Name = "tsmExportPdf";
            this.tsmExportPdf.Text = "PDF (*.pdf)…";
            this.tsmExportPdf.Click += new System.EventHandler(this.tsmExportPdf_Click);
            //
            // tsmExportWord
            //
            this.tsmExportWord.Name = "tsmExportWord";
            this.tsmExportWord.Text = "Word (*.docx)…";
            this.tsmExportWord.Click += new System.EventHandler(this.tsmExportWord_Click);
            //
            // tsmExportJson
            //
            this.tsmExportJson.Name = "tsmExportJson";
            this.tsmExportJson.Text = "JSON (*.json)…";
            this.tsmExportJson.Click += new System.EventHandler(this.tsmExportJson_Click);
            //
            // tsmExportHtml
            //
            this.tsmExportHtml.Name = "tsmExportHtml";
            this.tsmExportHtml.Text = "HTML (*.html)…";
            this.tsmExportHtml.Click += new System.EventHandler(this.tsmExportHtml_Click);
            //
            // tsmExportCsv
            //
            this.tsmExportCsv.Name = "tsmExportCsv";
            this.tsmExportCsv.Text = "CSV (*.csv)…";
            this.tsmExportCsv.Click += new System.EventHandler(this.tsmExportCsv_Click);
            //
            // tsbClose
            //
            this.tsbClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Text = "Close";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            //
            // split
            //
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Location = new System.Drawing.Point(0, 25);
            this.split.Name = "split";
            this.split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.split.Panel1.Controls.Add(this.txtSummary);
            this.split.Panel1.Controls.Add(this.lblScore);
            this.split.Panel2.Controls.Add(this.grdFindings);
            this.split.Size = new System.Drawing.Size(900, 575);
            this.split.SplitterDistance = 210;
            this.split.TabIndex = 1;
            //
            // lblScore
            //
            this.lblScore.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblScore.Name = "lblScore";
            this.lblScore.Size = new System.Drawing.Size(900, 30);
            this.lblScore.Height = 30;
            this.lblScore.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblScore.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblScore.Text = "No analysis yet";
            this.lblScore.Padding = new System.Windows.Forms.Padding(6, 0, 0, 0);
            //
            // txtSummary
            //
            this.txtSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSummary.Multiline = true;
            this.txtSummary.Name = "txtSummary";
            this.txtSummary.ReadOnly = true;
            this.txtSummary.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtSummary.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtSummary.TabIndex = 0;
            //
            // grdFindings
            //
            this.grdFindings.AllowUserToAddRows = false;
            this.grdFindings.AllowUserToDeleteRows = false;
            this.grdFindings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdFindings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdFindings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colCategory,
                this.colSeverity,
                this.colRecord,
                this.colTitle,
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
            // columns
            //
            this.colCategory.HeaderText = "Category";
            this.colCategory.Name = "colCategory";
            this.colCategory.FillWeight = 55F;
            this.colCategory.ReadOnly = true;
            this.colSeverity.HeaderText = "Severity";
            this.colSeverity.Name = "colSeverity";
            this.colSeverity.FillWeight = 45F;
            this.colSeverity.ReadOnly = true;
            this.colRecord.HeaderText = "Record";
            this.colRecord.Name = "colRecord";
            this.colRecord.FillWeight = 90F;
            this.colRecord.ReadOnly = true;
            this.colTitle.HeaderText = "Issue";
            this.colTitle.Name = "colTitle";
            this.colTitle.FillWeight = 110F;
            this.colTitle.ReadOnly = true;
            this.colRecommendation.HeaderText = "Recommendation";
            this.colRecommendation.Name = "colRecommendation";
            this.colRecommendation.FillWeight = 160F;
            this.colRecommendation.ReadOnly = true;
            //
            // PortalHealthAnalyzerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.split);
            this.Controls.Add(this.toolStrip);
            this.Name = "PortalHealthAnalyzerControl";
            this.Size = new System.Drawing.Size(900, 600);
            this.Load += new System.EventHandler(this.PortalHealthAnalyzerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel1.PerformLayout();
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoadWebsites;
        private System.Windows.Forms.ToolStripLabel tslWebsite;
        private System.Windows.Forms.ToolStripComboBox cboWebsite;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripSeparator tss1;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem tsmExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmExportPdf;
        private System.Windows.Forms.ToolStripMenuItem tsmExportWord;
        private System.Windows.Forms.ToolStripMenuItem tsmExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmExportHtml;
        private System.Windows.Forms.ToolStripMenuItem tsmExportCsv;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.Label lblScore;
        private System.Windows.Forms.TextBox txtSummary;
        private System.Windows.Forms.DataGridView grdFindings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCategory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecord;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecommendation;
    }
}
