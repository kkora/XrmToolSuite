namespace XrmToolSuite.SharingAnalyzer
{
    partial class SharingAnalyzerControl
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
            this.tsbTables = new System.Windows.Forms.ToolStripButton();
            this.tslPrincipal = new System.Windows.Forms.ToolStripLabel();
            this.tstPrincipal = new System.Windows.Forms.ToolStripTextBox();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbFullScan = new System.Windows.Forms.ToolStripButton();
            this.tsbScan = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.miExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportCsv = new System.Windows.Forms.ToolStripMenuItem();

            this.scMain = new System.Windows.Forms.SplitContainer();
            this.grdShares = new System.Windows.Forms.DataGridView();
            this.colShareTable = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colShareObject = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSharePrincipal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colShareType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colShareActive = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colShareRights = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.pnlRight = new System.Windows.Forms.Panel();
            this.tabDetails = new System.Windows.Forms.TabControl();
            this.tabFindings = new System.Windows.Forms.TabPage();
            this.grdFindings = new System.Windows.Forms.DataGridView();
            this.colFindingSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFindingTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFindingDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabIntensity = new System.Windows.Forms.TabPage();
            this.grdIntensity = new System.Windows.Forms.DataGridView();
            this.colIntTable = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIntPrincipal = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIntType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colIntShares = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabRecommendations = new System.Windows.Forms.TabPage();
            this.grdRecommendations = new System.Windows.Forms.DataGridView();
            this.colRecSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecComponent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRecAction = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.flpCards = new System.Windows.Forms.FlowLayoutPanel();
            this.lblHeader = new System.Windows.Forms.Label();

            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).BeginInit();
            this.scMain.Panel1.SuspendLayout();
            this.scMain.Panel2.SuspendLayout();
            this.scMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdShares)).BeginInit();
            this.pnlRight.SuspendLayout();
            this.tabDetails.SuspendLayout();
            this.tabFindings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            this.tabIntensity.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdIntensity)).BeginInit();
            this.tabRecommendations.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdRecommendations)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbTables,
                this.tslPrincipal,
                this.tstPrincipal,
                this.tssSeparator1,
                this.tsbFullScan,
                this.tsbScan,
                this.tssSeparator2,
                this.tsbExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1000, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbTables
            //
            this.tsbTables.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbTables.Name = "tsbTables";
            this.tsbTables.Text = "Tables…";
            this.tsbTables.ToolTipText = "Pick the tables whose record-level sharing to scan";
            this.tsbTables.Click += new System.EventHandler(this.tsbTables_Click);
            //
            // tslPrincipal
            //
            this.tslPrincipal.Name = "tslPrincipal";
            this.tslPrincipal.Text = "Principal filter:";
            //
            // tstPrincipal
            //
            this.tstPrincipal.Name = "tstPrincipal";
            this.tstPrincipal.Size = new System.Drawing.Size(150, 25);
            this.tstPrincipal.ToolTipText = "Filter the shares grid by principal name";
            this.tstPrincipal.TextChanged += new System.EventHandler(this.tstPrincipal_TextChanged);
            //
            // tsbFullScan
            //
            this.tsbFullScan.CheckOnClick = true;
            this.tsbFullScan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbFullScan.Name = "tsbFullScan";
            this.tsbFullScan.Text = "Full-environment scan";
            this.tsbFullScan.ToolTipText = "Scan every table's sharing (can be very large). Off by default.";
            this.tsbFullScan.CheckedChanged += new System.EventHandler(this.tsbFullScan_CheckedChanged);
            //
            // tsbScan
            //
            this.tsbScan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbScan.Name = "tsbScan";
            this.tsbScan.Text = "Scan sharing";
            this.tsbScan.Click += new System.EventHandler(this.tsbScan_Click);
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.miExportExcel,
                this.miExportPdf,
                this.miExportJson,
                this.miExportHtml,
                this.miExportCsv});
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export";
            //
            this.miExportExcel.Name = "miExportExcel";
            this.miExportExcel.Text = "Excel (.xlsx)";
            this.miExportExcel.Click += new System.EventHandler(this.miExportExcel_Click);
            this.miExportPdf.Name = "miExportPdf";
            this.miExportPdf.Text = "PDF (.pdf)";
            this.miExportPdf.Click += new System.EventHandler(this.miExportPdf_Click);
            this.miExportJson.Name = "miExportJson";
            this.miExportJson.Text = "JSON (.json)";
            this.miExportJson.Click += new System.EventHandler(this.miExportJson_Click);
            this.miExportHtml.Name = "miExportHtml";
            this.miExportHtml.Text = "HTML (.html)";
            this.miExportHtml.Click += new System.EventHandler(this.miExportHtml_Click);
            this.miExportCsv.Name = "miExportCsv";
            this.miExportCsv.Text = "CSV (.csv)";
            this.miExportCsv.Click += new System.EventHandler(this.miExportCsv_Click);
            //
            // scMain
            //
            this.scMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scMain.Location = new System.Drawing.Point(0, 25);
            this.scMain.Name = "scMain";
            this.scMain.Panel1.Controls.Add(this.grdShares);
            this.scMain.Panel2.Controls.Add(this.pnlRight);
            this.scMain.Size = new System.Drawing.Size(1000, 575);
            this.scMain.SplitterDistance = 540;
            this.scMain.TabIndex = 1;
            //
            // grdShares
            //
            this.grdShares.AllowUserToAddRows = false;
            this.grdShares.AllowUserToDeleteRows = false;
            this.grdShares.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdShares.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colShareTable,
                this.colShareObject,
                this.colSharePrincipal,
                this.colShareType,
                this.colShareActive,
                this.colShareRights});
            this.grdShares.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdShares.MultiSelect = false;
            this.grdShares.Name = "grdShares";
            this.grdShares.ReadOnly = true;
            this.grdShares.RowHeadersVisible = false;
            this.grdShares.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            //
            this.colShareTable.HeaderText = "Table";
            this.colShareTable.Name = "colShareTable";
            this.colShareTable.FillWeight = 16;
            this.colShareObject.HeaderText = "Record";
            this.colShareObject.Name = "colShareObject";
            this.colShareObject.FillWeight = 22;
            this.colSharePrincipal.HeaderText = "Principal";
            this.colSharePrincipal.Name = "colSharePrincipal";
            this.colSharePrincipal.FillWeight = 24;
            this.colShareType.HeaderText = "Type";
            this.colShareType.Name = "colShareType";
            this.colShareType.FillWeight = 10;
            this.colShareActive.HeaderText = "Active";
            this.colShareActive.Name = "colShareActive";
            this.colShareActive.FillWeight = 10;
            this.colShareRights.HeaderText = "Rights";
            this.colShareRights.Name = "colShareRights";
            this.colShareRights.FillWeight = 18;
            //
            // pnlRight
            //
            this.pnlRight.Controls.Add(this.tabDetails);
            this.pnlRight.Controls.Add(this.flpCards);
            this.pnlRight.Controls.Add(this.lblHeader);
            this.pnlRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.Name = "pnlRight";
            this.pnlRight.Padding = new System.Windows.Forms.Padding(6);
            //
            // lblHeader
            //
            this.lblHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblHeader.Height = 28;
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Text = "Pick tables and click Scan sharing to analyze record-level sharing.";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // flpCards
            //
            this.flpCards.AutoScroll = true;
            this.flpCards.Dock = System.Windows.Forms.DockStyle.Top;
            this.flpCards.Height = 92;
            this.flpCards.Name = "flpCards";
            this.flpCards.Padding = new System.Windows.Forms.Padding(2);
            this.flpCards.WrapContents = true;
            //
            // tabDetails
            //
            this.tabDetails.Controls.Add(this.tabFindings);
            this.tabDetails.Controls.Add(this.tabIntensity);
            this.tabDetails.Controls.Add(this.tabRecommendations);
            this.tabDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabDetails.Name = "tabDetails";
            //
            // tabFindings
            //
            this.tabFindings.Controls.Add(this.grdFindings);
            this.tabFindings.Name = "tabFindings";
            this.tabFindings.Padding = new System.Windows.Forms.Padding(3);
            this.tabFindings.Text = "Findings";
            this.tabFindings.UseVisualStyleBackColor = true;
            //
            this.grdFindings.AllowUserToAddRows = false;
            this.grdFindings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdFindings.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colFindingSeverity, this.colFindingTitle, this.colFindingDetail });
            this.grdFindings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdFindings.Name = "grdFindings";
            this.grdFindings.ReadOnly = true;
            this.grdFindings.RowHeadersVisible = false;
            this.colFindingSeverity.HeaderText = "Severity";
            this.colFindingSeverity.Name = "colFindingSeverity";
            this.colFindingSeverity.FillWeight = 12;
            this.colFindingTitle.HeaderText = "Finding";
            this.colFindingTitle.Name = "colFindingTitle";
            this.colFindingTitle.FillWeight = 28;
            this.colFindingDetail.HeaderText = "Evidence";
            this.colFindingDetail.Name = "colFindingDetail";
            this.colFindingDetail.FillWeight = 60;
            //
            // tabIntensity
            //
            this.tabIntensity.Controls.Add(this.grdIntensity);
            this.tabIntensity.Name = "tabIntensity";
            this.tabIntensity.Padding = new System.Windows.Forms.Padding(3);
            this.tabIntensity.Text = "Intensity (table × principal)";
            this.tabIntensity.UseVisualStyleBackColor = true;
            //
            this.grdIntensity.AllowUserToAddRows = false;
            this.grdIntensity.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdIntensity.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colIntTable, this.colIntPrincipal, this.colIntType, this.colIntShares });
            this.grdIntensity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdIntensity.Name = "grdIntensity";
            this.grdIntensity.ReadOnly = true;
            this.grdIntensity.RowHeadersVisible = false;
            this.colIntTable.HeaderText = "Table";
            this.colIntTable.Name = "colIntTable";
            this.colIntTable.FillWeight = 25;
            this.colIntPrincipal.HeaderText = "Principal";
            this.colIntPrincipal.Name = "colIntPrincipal";
            this.colIntPrincipal.FillWeight = 40;
            this.colIntType.HeaderText = "Type";
            this.colIntType.Name = "colIntType";
            this.colIntType.FillWeight = 15;
            this.colIntShares.HeaderText = "Shares";
            this.colIntShares.Name = "colIntShares";
            this.colIntShares.FillWeight = 20;
            //
            // tabRecommendations
            //
            this.tabRecommendations.Controls.Add(this.grdRecommendations);
            this.tabRecommendations.Name = "tabRecommendations";
            this.tabRecommendations.Padding = new System.Windows.Forms.Padding(3);
            this.tabRecommendations.Text = "Recommendations (preview)";
            this.tabRecommendations.UseVisualStyleBackColor = true;
            //
            this.grdRecommendations.AllowUserToAddRows = false;
            this.grdRecommendations.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdRecommendations.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colRecSeverity, this.colRecComponent, this.colRecAction });
            this.grdRecommendations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdRecommendations.Name = "grdRecommendations";
            this.grdRecommendations.ReadOnly = true;
            this.grdRecommendations.RowHeadersVisible = false;
            this.colRecSeverity.HeaderText = "Severity";
            this.colRecSeverity.Name = "colRecSeverity";
            this.colRecSeverity.FillWeight = 12;
            this.colRecComponent.HeaderText = "Record / Principal";
            this.colRecComponent.Name = "colRecComponent";
            this.colRecComponent.FillWeight = 30;
            this.colRecAction.HeaderText = "Recommended cleanup (preview only — no changes made)";
            this.colRecAction.Name = "colRecAction";
            this.colRecAction.FillWeight = 58;
            //
            // SharingAnalyzerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.scMain);
            this.Controls.Add(this.toolStrip);
            this.Name = "SharingAnalyzerControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.Load += new System.EventHandler(this.SharingAnalyzerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.scMain.Panel1.ResumeLayout(false);
            this.scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).EndInit();
            this.scMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdShares)).EndInit();
            this.pnlRight.ResumeLayout(false);
            this.tabDetails.ResumeLayout(false);
            this.tabFindings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.tabIntensity.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdIntensity)).EndInit();
            this.tabRecommendations.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdRecommendations)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbTables;
        private System.Windows.Forms.ToolStripLabel tslPrincipal;
        private System.Windows.Forms.ToolStripTextBox tstPrincipal;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbFullScan;
        private System.Windows.Forms.ToolStripButton tsbScan;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem miExportExcel;
        private System.Windows.Forms.ToolStripMenuItem miExportPdf;
        private System.Windows.Forms.ToolStripMenuItem miExportJson;
        private System.Windows.Forms.ToolStripMenuItem miExportHtml;
        private System.Windows.Forms.ToolStripMenuItem miExportCsv;

        private System.Windows.Forms.SplitContainer scMain;
        private System.Windows.Forms.DataGridView grdShares;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShareTable;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShareObject;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSharePrincipal;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShareType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShareActive;
        private System.Windows.Forms.DataGridViewTextBoxColumn colShareRights;

        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.FlowLayoutPanel flpCards;
        private System.Windows.Forms.TabControl tabDetails;
        private System.Windows.Forms.TabPage tabFindings;
        private System.Windows.Forms.DataGridView grdFindings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFindingSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFindingTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFindingDetail;
        private System.Windows.Forms.TabPage tabIntensity;
        private System.Windows.Forms.DataGridView grdIntensity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIntTable;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIntPrincipal;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIntType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colIntShares;
        private System.Windows.Forms.TabPage tabRecommendations;
        private System.Windows.Forms.DataGridView grdRecommendations;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecComponent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRecAction;
    }
}
