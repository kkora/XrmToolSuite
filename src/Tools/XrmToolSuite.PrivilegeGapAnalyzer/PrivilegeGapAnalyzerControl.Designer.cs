namespace XrmToolSuite.PrivilegeGapAnalyzer
{
    partial class PrivilegeGapAnalyzerControl
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
            this.tsbLoad = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbAnalyze = new System.Windows.Forms.ToolStripButton();
            this.tsbCompare = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.miExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportHtml = new System.Windows.Forms.ToolStripMenuItem();

            this.pnlSelectors = new System.Windows.Forms.TableLayoutPanel();
            this.lblPrincipalType = new System.Windows.Forms.Label();
            this.lblPrincipal = new System.Windows.Forms.Label();
            this.lblTable = new System.Windows.Forms.Label();
            this.lblOperation = new System.Windows.Forms.Label();
            this.lblScope = new System.Windows.Forms.Label();
            this.lblRelatedTable = new System.Windows.Forms.Label();
            this.cboPrincipalType = new System.Windows.Forms.ComboBox();
            this.cboPrincipal = new System.Windows.Forms.ComboBox();
            this.cboTable = new System.Windows.Forms.ComboBox();
            this.cboOperation = new System.Windows.Forms.ComboBox();
            this.cboScope = new System.Windows.Forms.ComboBox();
            this.cboRelatedTable = new System.Windows.Forms.ComboBox();

            this.pnlVerdict = new System.Windows.Forms.Panel();
            this.lblVerdict = new System.Windows.Forms.Label();

            this.scMain = new System.Windows.Forms.SplitContainer();
            this.grdEffective = new System.Windows.Forms.DataGridView();
            this.colPrivilege = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScope = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSourceRole = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSourceTeam = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colViaTeam = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.pnlDetails = new System.Windows.Forms.TableLayoutPanel();
            this.lblExplanationHeader = new System.Windows.Forms.Label();
            this.txtExplanation = new System.Windows.Forms.TextBox();
            this.lblRecommendationHeader = new System.Windows.Forms.Label();
            this.txtRecommendation = new System.Windows.Forms.TextBox();

            this.toolStrip.SuspendLayout();
            this.pnlSelectors.SuspendLayout();
            this.pnlVerdict.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).BeginInit();
            this.scMain.Panel1.SuspendLayout();
            this.scMain.Panel2.SuspendLayout();
            this.scMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdEffective)).BeginInit();
            this.pnlDetails.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoad,
                this.tssSeparator1,
                this.tsbAnalyze,
                this.tsbCompare,
                this.tssSeparator2,
                this.tsbExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(900, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoad
            //
            this.tsbLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoad.Name = "tsbLoad";
            this.tsbLoad.Text = "Load principals";
            this.tsbLoad.Click += new System.EventHandler(this.tsbLoad_Click);
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
            //
            // tsbCompare
            //
            this.tsbCompare.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCompare.Name = "tsbCompare";
            this.tsbCompare.Text = "Compare…";
            this.tsbCompare.Click += new System.EventHandler(this.tsbCompare_Click);
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.miExportExcel,
                this.miExportPdf,
                this.miExportCsv,
                this.miExportJson,
                this.miExportHtml});
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export";
            this.tsbExport.Enabled = false;
            //
            // miExportExcel
            //
            this.miExportExcel.Name = "miExportExcel";
            this.miExportExcel.Text = "Excel (*.xlsx)…";
            this.miExportExcel.Click += new System.EventHandler(this.miExportExcel_Click);
            //
            // miExportPdf
            //
            this.miExportPdf.Name = "miExportPdf";
            this.miExportPdf.Text = "PDF (*.pdf)…";
            this.miExportPdf.Click += new System.EventHandler(this.miExportPdf_Click);
            //
            // miExportCsv
            //
            this.miExportCsv.Name = "miExportCsv";
            this.miExportCsv.Text = "CSV…";
            this.miExportCsv.Click += new System.EventHandler(this.miExportCsv_Click);
            //
            // miExportJson
            //
            this.miExportJson.Name = "miExportJson";
            this.miExportJson.Text = "JSON…";
            this.miExportJson.Click += new System.EventHandler(this.miExportJson_Click);
            //
            // miExportHtml
            //
            this.miExportHtml.Name = "miExportHtml";
            this.miExportHtml.Text = "HTML…";
            this.miExportHtml.Click += new System.EventHandler(this.miExportHtml_Click);
            //
            // tssSeparators
            //
            this.tssSeparator1.Name = "tssSeparator1";
            this.tssSeparator2.Name = "tssSeparator2";
            //
            // pnlSelectors
            //
            this.pnlSelectors.ColumnCount = 6;
            this.pnlSelectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.pnlSelectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.pnlSelectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.pnlSelectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.pnlSelectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66F));
            this.pnlSelectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.70F));
            this.pnlSelectors.RowCount = 2;
            this.pnlSelectors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.pnlSelectors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.pnlSelectors.Controls.Add(this.lblPrincipalType, 0, 0);
            this.pnlSelectors.Controls.Add(this.lblPrincipal, 1, 0);
            this.pnlSelectors.Controls.Add(this.lblTable, 2, 0);
            this.pnlSelectors.Controls.Add(this.lblOperation, 3, 0);
            this.pnlSelectors.Controls.Add(this.lblScope, 4, 0);
            this.pnlSelectors.Controls.Add(this.lblRelatedTable, 5, 0);
            this.pnlSelectors.Controls.Add(this.cboPrincipalType, 0, 1);
            this.pnlSelectors.Controls.Add(this.cboPrincipal, 1, 1);
            this.pnlSelectors.Controls.Add(this.cboTable, 2, 1);
            this.pnlSelectors.Controls.Add(this.cboOperation, 3, 1);
            this.pnlSelectors.Controls.Add(this.cboScope, 4, 1);
            this.pnlSelectors.Controls.Add(this.cboRelatedTable, 5, 1);
            this.pnlSelectors.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSelectors.Location = new System.Drawing.Point(0, 25);
            this.pnlSelectors.Name = "pnlSelectors";
            this.pnlSelectors.Padding = new System.Windows.Forms.Padding(6, 4, 6, 4);
            this.pnlSelectors.Size = new System.Drawing.Size(900, 60);
            this.pnlSelectors.TabIndex = 1;
            //
            // labels
            //
            this.lblPrincipalType.AutoSize = true; this.lblPrincipalType.Text = "Principal type";
            this.lblPrincipal.AutoSize = true; this.lblPrincipal.Text = "Principal";
            this.lblTable.AutoSize = true; this.lblTable.Text = "Table";
            this.lblOperation.AutoSize = true; this.lblOperation.Text = "Operation";
            this.lblScope.AutoSize = true; this.lblScope.Text = "Required scope";
            this.lblRelatedTable.AutoSize = true; this.lblRelatedTable.Text = "Related table (Append)";
            //
            // combos
            //
            this.cboPrincipalType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPrincipalType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboPrincipalType.Name = "cboPrincipalType";
            this.cboPrincipalType.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
            this.cboPrincipalType.SelectedIndexChanged += new System.EventHandler(this.cboPrincipalType_SelectedIndexChanged);

            this.cboPrincipal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboPrincipal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboPrincipal.Name = "cboPrincipal";
            this.cboPrincipal.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);

            this.cboTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cboTable.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboTable.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboTable.Name = "cboTable";
            this.cboTable.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);

            this.cboOperation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboOperation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboOperation.Name = "cboOperation";
            this.cboOperation.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
            this.cboOperation.SelectedIndexChanged += new System.EventHandler(this.cboOperation_SelectedIndexChanged);

            this.cboScope.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboScope.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboScope.Name = "cboScope";
            this.cboScope.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);

            this.cboRelatedTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
            this.cboRelatedTable.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cboRelatedTable.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboRelatedTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cboRelatedTable.Name = "cboRelatedTable";
            this.cboRelatedTable.Enabled = false;
            this.cboRelatedTable.Margin = new System.Windows.Forms.Padding(3, 3, 6, 3);
            //
            // pnlVerdict
            //
            this.pnlVerdict.Controls.Add(this.lblVerdict);
            this.pnlVerdict.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlVerdict.Location = new System.Drawing.Point(0, 85);
            this.pnlVerdict.Name = "pnlVerdict";
            this.pnlVerdict.Size = new System.Drawing.Size(900, 52);
            this.pnlVerdict.BackColor = System.Drawing.SystemColors.ControlLight;
            this.pnlVerdict.TabIndex = 2;
            //
            // lblVerdict
            //
            this.lblVerdict.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblVerdict.Name = "lblVerdict";
            this.lblVerdict.Text = "No analysis yet — pick a principal, table, and operation, then click Analyze.";
            this.lblVerdict.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblVerdict.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.lblVerdict.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            //
            // scMain
            //
            this.scMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scMain.Location = new System.Drawing.Point(0, 137);
            this.scMain.Name = "scMain";
            this.scMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.scMain.Panel1.Controls.Add(this.grdEffective);
            this.scMain.Panel2.Controls.Add(this.pnlDetails);
            this.scMain.Size = new System.Drawing.Size(900, 423);
            this.scMain.SplitterDistance = 240;
            this.scMain.TabIndex = 3;
            //
            // grdEffective
            //
            this.grdEffective.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colPrivilege,
                this.colScope,
                this.colSourceRole,
                this.colSourceTeam,
                this.colViaTeam});
            this.grdEffective.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdEffective.Name = "grdEffective";
            this.grdEffective.AllowUserToAddRows = false;
            this.grdEffective.AllowUserToDeleteRows = false;
            this.grdEffective.ReadOnly = true;
            this.grdEffective.RowHeadersVisible = false;
            this.grdEffective.MultiSelect = false;
            this.grdEffective.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdEffective.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdEffective.BackgroundColor = System.Drawing.SystemColors.Window;
            this.grdEffective.TabIndex = 0;
            //
            // columns
            //
            this.colPrivilege.HeaderText = "Privilege"; this.colPrivilege.Name = "colPrivilege"; this.colPrivilege.FillWeight = 32F; this.colPrivilege.ReadOnly = true;
            this.colScope.HeaderText = "Scope"; this.colScope.Name = "colScope"; this.colScope.FillWeight = 16F; this.colScope.ReadOnly = true;
            this.colSourceRole.HeaderText = "Source role"; this.colSourceRole.Name = "colSourceRole"; this.colSourceRole.FillWeight = 24F; this.colSourceRole.ReadOnly = true;
            this.colSourceTeam.HeaderText = "Source team"; this.colSourceTeam.Name = "colSourceTeam"; this.colSourceTeam.FillWeight = 18F; this.colSourceTeam.ReadOnly = true;
            this.colViaTeam.HeaderText = "Via team"; this.colViaTeam.Name = "colViaTeam"; this.colViaTeam.FillWeight = 10F; this.colViaTeam.ReadOnly = true;
            //
            // pnlDetails
            //
            this.pnlDetails.ColumnCount = 1;
            this.pnlDetails.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlDetails.RowCount = 4;
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.pnlDetails.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.pnlDetails.Controls.Add(this.lblExplanationHeader, 0, 0);
            this.pnlDetails.Controls.Add(this.txtExplanation, 0, 1);
            this.pnlDetails.Controls.Add(this.lblRecommendationHeader, 0, 2);
            this.pnlDetails.Controls.Add(this.txtRecommendation, 0, 3);
            this.pnlDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDetails.Name = "pnlDetails";
            this.pnlDetails.Padding = new System.Windows.Forms.Padding(4);
            this.pnlDetails.TabIndex = 0;
            //
            // detail labels + textboxes
            //
            this.lblExplanationHeader.AutoSize = true; this.lblExplanationHeader.Text = "Explanation (paste into a ticket)";
            this.lblExplanationHeader.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this.lblRecommendationHeader.AutoSize = true; this.lblRecommendationHeader.Text = "Recommendation (suggestion only — the tool never edits roles)";
            this.lblRecommendationHeader.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);

            this.txtExplanation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtExplanation.Multiline = true;
            this.txtExplanation.ReadOnly = true;
            this.txtExplanation.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtExplanation.BackColor = System.Drawing.SystemColors.Window;
            this.txtExplanation.Name = "txtExplanation";

            this.txtRecommendation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRecommendation.Multiline = true;
            this.txtRecommendation.ReadOnly = true;
            this.txtRecommendation.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtRecommendation.BackColor = System.Drawing.SystemColors.Window;
            this.txtRecommendation.Name = "txtRecommendation";
            //
            // PrivilegeGapAnalyzerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.scMain);
            this.Controls.Add(this.pnlVerdict);
            this.Controls.Add(this.pnlSelectors);
            this.Controls.Add(this.toolStrip);
            this.Name = "PrivilegeGapAnalyzerControl";
            this.Size = new System.Drawing.Size(900, 560);
            this.Load += new System.EventHandler(this.PrivilegeGapAnalyzerControl_Load);

            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.pnlSelectors.ResumeLayout(false);
            this.pnlSelectors.PerformLayout();
            this.pnlVerdict.ResumeLayout(false);
            this.scMain.Panel1.ResumeLayout(false);
            this.scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).EndInit();
            this.scMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdEffective)).EndInit();
            this.pnlDetails.ResumeLayout(false);
            this.pnlDetails.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoad;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripButton tsbCompare;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem miExportExcel;
        private System.Windows.Forms.ToolStripMenuItem miExportPdf;
        private System.Windows.Forms.ToolStripMenuItem miExportCsv;
        private System.Windows.Forms.ToolStripMenuItem miExportJson;
        private System.Windows.Forms.ToolStripMenuItem miExportHtml;

        private System.Windows.Forms.TableLayoutPanel pnlSelectors;
        private System.Windows.Forms.Label lblPrincipalType;
        private System.Windows.Forms.Label lblPrincipal;
        private System.Windows.Forms.Label lblTable;
        private System.Windows.Forms.Label lblOperation;
        private System.Windows.Forms.Label lblScope;
        private System.Windows.Forms.Label lblRelatedTable;
        private System.Windows.Forms.ComboBox cboPrincipalType;
        private System.Windows.Forms.ComboBox cboPrincipal;
        private System.Windows.Forms.ComboBox cboTable;
        private System.Windows.Forms.ComboBox cboOperation;
        private System.Windows.Forms.ComboBox cboScope;
        private System.Windows.Forms.ComboBox cboRelatedTable;

        private System.Windows.Forms.Panel pnlVerdict;
        private System.Windows.Forms.Label lblVerdict;

        private System.Windows.Forms.SplitContainer scMain;
        private System.Windows.Forms.DataGridView grdEffective;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPrivilege;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScope;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSourceRole;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSourceTeam;
        private System.Windows.Forms.DataGridViewTextBoxColumn colViaTeam;

        private System.Windows.Forms.TableLayoutPanel pnlDetails;
        private System.Windows.Forms.Label lblExplanationHeader;
        private System.Windows.Forms.TextBox txtExplanation;
        private System.Windows.Forms.Label lblRecommendationHeader;
        private System.Windows.Forms.TextBox txtRecommendation;
    }
}
