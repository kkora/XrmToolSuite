namespace XrmToolSuite.AuditComplianceChecker
{
    partial class AuditComplianceCheckerControl
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
            this.tsbCheckSettings = new System.Windows.Forms.ToolStripButton();
            this.tss1 = new System.Windows.Forms.ToolStripSeparator();
            this.tslblFrom = new System.Windows.Forms.ToolStripLabel();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.tshFrom = new System.Windows.Forms.ToolStripControlHost(this.dtpFrom);
            this.tslblTo = new System.Windows.Forms.ToolStripLabel();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.tshTo = new System.Windows.Forms.ToolStripControlHost(this.dtpTo);
            this.tslblScope = new System.Windows.Forms.ToolStripLabel();
            this.tstScope = new System.Windows.Forms.ToolStripTextBox();
            this.tsbAnalyzeActivity = new System.Windows.Forms.ToolStripButton();
            this.tss2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsddExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.miExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.tss3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();

            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabCoverage = new System.Windows.Forms.TabPage();
            this.grdCoverage = new System.Windows.Forms.DataGridView();
            this.lblOrgAudit = new System.Windows.Forms.Label();
            this.tabActivity = new System.Windows.Forms.TabPage();
            this.grdActivity = new System.Windows.Forms.DataGridView();
            this.pnlActivityTop = new System.Windows.Forms.Panel();
            this.cboActivityView = new System.Windows.Forms.ComboBox();
            this.lblActivityHighlights = new System.Windows.Forms.Label();
            this.tabStorage = new System.Windows.Forms.TabPage();
            this.grdStorage = new System.Windows.Forms.DataGridView();
            this.lblStorageNote = new System.Windows.Forms.Label();
            this.tabDashboard = new System.Windows.Forms.TabPage();
            this.grdCategories = new System.Windows.Forms.DataGridView();
            this.pnlScore = new System.Windows.Forms.Panel();
            this.lblScoreValue = new System.Windows.Forms.Label();
            this.lblBand = new System.Windows.Forms.Label();
            this.lblScoreLead = new System.Windows.Forms.Label();
            this.tabRecommendations = new System.Windows.Forms.TabPage();
            this.grdFindings = new System.Windows.Forms.DataGridView();

            this.toolStrip.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabCoverage.SuspendLayout();
            this.tabActivity.SuspendLayout();
            this.pnlActivityTop.SuspendLayout();
            this.tabStorage.SuspendLayout();
            this.tabDashboard.SuspendLayout();
            this.pnlScore.SuspendLayout();
            this.tabRecommendations.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdCoverage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdActivity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdStorage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdCategories)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbCheckSettings,
                this.tss1,
                this.tslblFrom,
                this.tshFrom,
                this.tslblTo,
                this.tshTo,
                this.tslblScope,
                this.tstScope,
                this.tsbAnalyzeActivity,
                this.tss2,
                this.tsddExport,
                this.tss3,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1000, 27);
            this.toolStrip.TabIndex = 0;
            //
            // tsbCheckSettings
            //
            this.tsbCheckSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCheckSettings.Name = "tsbCheckSettings";
            this.tsbCheckSettings.Text = "Check audit settings";
            this.tsbCheckSettings.ToolTipText = "Read org/table/column audit settings and score compliance";
            this.tsbCheckSettings.Click += new System.EventHandler(this.tsbCheckSettings_Click);
            //
            // tslblFrom
            //
            this.tslblFrom.Name = "tslblFrom";
            this.tslblFrom.Text = "Activity:";
            //
            // dtpFrom
            //
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpFrom.Width = 100;
            //
            // tshFrom
            //
            this.tshFrom.Name = "tshFrom";
            //
            // tslblTo
            //
            this.tslblTo.Name = "tslblTo";
            this.tslblTo.Text = "to";
            //
            // dtpTo
            //
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpTo.Width = 100;
            //
            // tshTo
            //
            this.tshTo.Name = "tshTo";
            //
            // tslblScope
            //
            this.tslblScope.Name = "tslblScope";
            this.tslblScope.Text = "tables:";
            //
            // tstScope
            //
            this.tstScope.Name = "tstScope";
            this.tstScope.Width = 140;
            this.tstScope.ToolTipText = "Optional: comma-separated table logical names to scope activity (blank = all)";
            //
            // tsbAnalyzeActivity
            //
            this.tsbAnalyzeActivity.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyzeActivity.Name = "tsbAnalyzeActivity";
            this.tsbAnalyzeActivity.Text = "Analyze activity";
            this.tsbAnalyzeActivity.ToolTipText = "Tally audit activity over the selected date range (heavy — scoped by date/table)";
            this.tsbAnalyzeActivity.Click += new System.EventHandler(this.tsbAnalyzeActivity_Click);
            //
            // tsddExport
            //
            this.tsddExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsddExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.miExportExcel,
                this.miExportPdf,
                this.miExportJson,
                this.miExportHtml,
                this.miExportCsv});
            this.tsddExport.Name = "tsddExport";
            this.tsddExport.Text = "Export";
            //
            // miExportExcel
            //
            this.miExportExcel.Name = "miExportExcel";
            this.miExportExcel.Text = "Excel (.xlsx)";
            this.miExportExcel.Click += new System.EventHandler(this.miExportExcel_Click);
            //
            // miExportPdf
            //
            this.miExportPdf.Name = "miExportPdf";
            this.miExportPdf.Text = "PDF (.pdf)";
            this.miExportPdf.Click += new System.EventHandler(this.miExportPdf_Click);
            //
            // miExportJson
            //
            this.miExportJson.Name = "miExportJson";
            this.miExportJson.Text = "JSON (.json)";
            this.miExportJson.Click += new System.EventHandler(this.miExportJson_Click);
            //
            // miExportHtml
            //
            this.miExportHtml.Name = "miExportHtml";
            this.miExportHtml.Text = "HTML (.html)";
            this.miExportHtml.Click += new System.EventHandler(this.miExportHtml_Click);
            //
            // miExportCsv
            //
            this.miExportCsv.Name = "miExportCsv";
            this.miExportCsv.Text = "CSV (.csv)";
            this.miExportCsv.Click += new System.EventHandler(this.miExportCsv_Click);
            //
            // tsbClose
            //
            this.tsbClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Text = "Close";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            //
            // tabControl
            //
            this.tabControl.Controls.Add(this.tabCoverage);
            this.tabControl.Controls.Add(this.tabActivity);
            this.tabControl.Controls.Add(this.tabStorage);
            this.tabControl.Controls.Add(this.tabDashboard);
            this.tabControl.Controls.Add(this.tabRecommendations);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 27);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1000, 573);
            this.tabControl.TabIndex = 1;
            //
            // tabCoverage
            //
            this.tabCoverage.Controls.Add(this.grdCoverage);
            this.tabCoverage.Controls.Add(this.lblOrgAudit);
            this.tabCoverage.Name = "tabCoverage";
            this.tabCoverage.Padding = new System.Windows.Forms.Padding(3);
            this.tabCoverage.Text = "Audit settings";
            this.tabCoverage.UseVisualStyleBackColor = true;
            //
            // lblOrgAudit
            //
            this.lblOrgAudit.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblOrgAudit.Height = 40;
            this.lblOrgAudit.Padding = new System.Windows.Forms.Padding(6);
            this.lblOrgAudit.Name = "lblOrgAudit";
            this.lblOrgAudit.Text = "Click \"Check audit settings\" to read org/table/column audit configuration.";
            //
            // grdCoverage
            //
            ConfigureGrid(this.grdCoverage);
            this.grdCoverage.Name = "grdCoverage";
            //
            // tabActivity
            //
            this.tabActivity.Controls.Add(this.grdActivity);
            this.tabActivity.Controls.Add(this.pnlActivityTop);
            this.tabActivity.Name = "tabActivity";
            this.tabActivity.Padding = new System.Windows.Forms.Padding(3);
            this.tabActivity.Text = "Activity";
            this.tabActivity.UseVisualStyleBackColor = true;
            //
            // pnlActivityTop
            //
            this.pnlActivityTop.Controls.Add(this.cboActivityView);
            this.pnlActivityTop.Controls.Add(this.lblActivityHighlights);
            this.pnlActivityTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlActivityTop.Height = 64;
            this.pnlActivityTop.Name = "pnlActivityTop";
            //
            // cboActivityView
            //
            this.cboActivityView.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboActivityView.Location = new System.Drawing.Point(6, 8);
            this.cboActivityView.Width = 160;
            this.cboActivityView.Name = "cboActivityView";
            this.cboActivityView.SelectedIndexChanged += new System.EventHandler(this.cboActivityView_SelectedIndexChanged);
            //
            // lblActivityHighlights
            //
            this.lblActivityHighlights.Location = new System.Drawing.Point(180, 6);
            this.lblActivityHighlights.AutoSize = false;
            this.lblActivityHighlights.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblActivityHighlights.Width = 700;
            this.lblActivityHighlights.Padding = new System.Windows.Forms.Padding(6);
            this.lblActivityHighlights.Name = "lblActivityHighlights";
            this.lblActivityHighlights.Text = "Run \"Analyze activity\" to summarize audit records by table, user, and date.";
            //
            // grdActivity
            //
            ConfigureGrid(this.grdActivity);
            this.grdActivity.Name = "grdActivity";
            //
            // tabStorage
            //
            this.tabStorage.Controls.Add(this.grdStorage);
            this.tabStorage.Controls.Add(this.lblStorageNote);
            this.tabStorage.Name = "tabStorage";
            this.tabStorage.Padding = new System.Windows.Forms.Padding(3);
            this.tabStorage.Text = "Storage (estimate)";
            this.tabStorage.UseVisualStyleBackColor = true;
            //
            // lblStorageNote
            //
            this.lblStorageNote.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStorageNote.Height = 48;
            this.lblStorageNote.Padding = new System.Windows.Forms.Padding(6);
            this.lblStorageNote.Name = "lblStorageNote";
            this.lblStorageNote.Text = "ESTIMATE ONLY — audit storage growth is inferred from audit record volume (~2 KB/record), " +
                "not billed Dataverse storage. Run \"Analyze activity\" to populate.";
            //
            // grdStorage
            //
            ConfigureGrid(this.grdStorage);
            this.grdStorage.Name = "grdStorage";
            //
            // tabDashboard
            //
            this.tabDashboard.Controls.Add(this.grdCategories);
            this.tabDashboard.Controls.Add(this.pnlScore);
            this.tabDashboard.Name = "tabDashboard";
            this.tabDashboard.Padding = new System.Windows.Forms.Padding(3);
            this.tabDashboard.Text = "Compliance score";
            this.tabDashboard.UseVisualStyleBackColor = true;
            //
            // pnlScore
            //
            this.pnlScore.Controls.Add(this.lblScoreLead);
            this.pnlScore.Controls.Add(this.lblBand);
            this.pnlScore.Controls.Add(this.lblScoreValue);
            this.pnlScore.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlScore.Height = 120;
            this.pnlScore.Name = "pnlScore";
            //
            // lblScoreValue
            //
            this.lblScoreValue.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Bold);
            this.lblScoreValue.Location = new System.Drawing.Point(12, 8);
            this.lblScoreValue.AutoSize = true;
            this.lblScoreValue.Name = "lblScoreValue";
            this.lblScoreValue.Text = "—";
            //
            // lblBand
            //
            this.lblBand.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblBand.Location = new System.Drawing.Point(200, 24);
            this.lblBand.AutoSize = true;
            this.lblBand.Name = "lblBand";
            this.lblBand.Text = "";
            //
            // lblScoreLead
            //
            this.lblScoreLead.Location = new System.Drawing.Point(200, 56);
            this.lblScoreLead.AutoSize = false;
            this.lblScoreLead.Size = new System.Drawing.Size(760, 56);
            this.lblScoreLead.Name = "lblScoreLead";
            this.lblScoreLead.Text = "Compliance readiness score (0–100). HIGHER = MORE compliant.";
            //
            // grdCategories
            //
            ConfigureGrid(this.grdCategories);
            this.grdCategories.Name = "grdCategories";
            //
            // tabRecommendations
            //
            this.tabRecommendations.Controls.Add(this.grdFindings);
            this.tabRecommendations.Name = "tabRecommendations";
            this.tabRecommendations.Padding = new System.Windows.Forms.Padding(3);
            this.tabRecommendations.Text = "Recommendations";
            this.tabRecommendations.UseVisualStyleBackColor = true;
            //
            // grdFindings
            //
            ConfigureGrid(this.grdFindings);
            this.grdFindings.Name = "grdFindings";
            //
            // AuditComplianceCheckerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.toolStrip);
            this.Name = "AuditComplianceCheckerControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.Load += new System.EventHandler(this.AuditComplianceCheckerControl_Load);

            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.tabCoverage.ResumeLayout(false);
            this.tabActivity.ResumeLayout(false);
            this.pnlActivityTop.ResumeLayout(false);
            this.tabStorage.ResumeLayout(false);
            this.tabDashboard.ResumeLayout(false);
            this.pnlScore.ResumeLayout(false);
            this.pnlScore.PerformLayout();
            this.tabRecommendations.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdCoverage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdActivity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdStorage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdCategories)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>Shared read-only grid defaults, applied in the Designer.</summary>
        private static void ConfigureGrid(System.Windows.Forms.DataGridView g)
        {
            g.Dock = System.Windows.Forms.DockStyle.Fill;
            g.ReadOnly = true;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            g.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbCheckSettings;
        private System.Windows.Forms.ToolStripSeparator tss1;
        private System.Windows.Forms.ToolStripLabel tslblFrom;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.ToolStripControlHost tshFrom;
        private System.Windows.Forms.ToolStripLabel tslblTo;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.ToolStripControlHost tshTo;
        private System.Windows.Forms.ToolStripLabel tslblScope;
        private System.Windows.Forms.ToolStripTextBox tstScope;
        private System.Windows.Forms.ToolStripButton tsbAnalyzeActivity;
        private System.Windows.Forms.ToolStripSeparator tss2;
        private System.Windows.Forms.ToolStripDropDownButton tsddExport;
        private System.Windows.Forms.ToolStripMenuItem miExportExcel;
        private System.Windows.Forms.ToolStripMenuItem miExportPdf;
        private System.Windows.Forms.ToolStripMenuItem miExportJson;
        private System.Windows.Forms.ToolStripMenuItem miExportHtml;
        private System.Windows.Forms.ToolStripMenuItem miExportCsv;
        private System.Windows.Forms.ToolStripSeparator tss3;
        private System.Windows.Forms.ToolStripButton tsbClose;

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabCoverage;
        private System.Windows.Forms.DataGridView grdCoverage;
        private System.Windows.Forms.Label lblOrgAudit;
        private System.Windows.Forms.TabPage tabActivity;
        private System.Windows.Forms.Panel pnlActivityTop;
        private System.Windows.Forms.ComboBox cboActivityView;
        private System.Windows.Forms.Label lblActivityHighlights;
        private System.Windows.Forms.DataGridView grdActivity;
        private System.Windows.Forms.TabPage tabStorage;
        private System.Windows.Forms.DataGridView grdStorage;
        private System.Windows.Forms.Label lblStorageNote;
        private System.Windows.Forms.TabPage tabDashboard;
        private System.Windows.Forms.Panel pnlScore;
        private System.Windows.Forms.Label lblScoreValue;
        private System.Windows.Forms.Label lblBand;
        private System.Windows.Forms.Label lblScoreLead;
        private System.Windows.Forms.DataGridView grdCategories;
        private System.Windows.Forms.TabPage tabRecommendations;
        private System.Windows.Forms.DataGridView grdFindings;
    }
}
