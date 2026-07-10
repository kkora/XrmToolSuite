namespace XrmToolSuite.AttributeAuditor
{
    partial class AttributeAuditorControl
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
            this.tsbRun = new System.Windows.Forms.ToolStripButton();
            this.tsbCustomOnly = new System.Windows.Forms.ToolStripButton();
            this.tsbCandidatesOnly = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSettings = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExportCsv = new System.Windows.Forms.ToolStripButton();
            this.tsbExportHtml = new System.Windows.Forms.ToolStripButton();
            this.lvResults = new System.Windows.Forms.ListView();
            this.colTable = new System.Windows.Forms.ColumnHeader();
            this.colColumn = new System.Windows.Forms.ColumnHeader();
            this.colDisplay = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.colManaged = new System.Windows.Forms.ColumnHeader();
            this.colUsed = new System.Windows.Forms.ColumnHeader();
            this.colUsage = new System.Windows.Forms.ColumnHeader();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbRun,
                this.tsbCustomOnly,
                this.tsbCandidatesOnly,
                this.tssSeparator1,
                this.tsbSettings,
                this.tssSeparator2,
                this.tsbExportCsv,
                this.tsbExportHtml});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(900, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbRun
            //
            this.tsbRun.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRun.Name = "tsbRun";
            this.tsbRun.Text = "Run audit";
            this.tsbRun.ToolTipText = "Audit custom columns and detect usage across forms, views, processes and field security";
            this.tsbRun.Click += new System.EventHandler(this.tsbRun_Click);
            //
            // tsbCustomOnly
            //
            this.tsbCustomOnly.CheckOnClick = true;
            this.tsbCustomOnly.Checked = true;
            this.tsbCustomOnly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCustomOnly.Name = "tsbCustomOnly";
            this.tsbCustomOnly.Text = "Custom tables only";
            this.tsbCustomOnly.ToolTipText = "Limit the scan to custom tables (custom columns on system tables are excluded)";
            //
            // tsbCandidatesOnly
            //
            this.tsbCandidatesOnly.CheckOnClick = true;
            this.tsbCandidatesOnly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCandidatesOnly.Name = "tsbCandidatesOnly";
            this.tsbCandidatesOnly.Text = "Candidates only";
            this.tsbCandidatesOnly.ToolTipText = "Show only unused custom columns (retirement candidates)";
            this.tsbCandidatesOnly.CheckedChanged += new System.EventHandler(this.tsbCandidatesOnly_CheckedChanged);
            //
            // tssSeparator1
            //
            this.tssSeparator1.Name = "tssSeparator1";
            //
            // tsbSettings
            //
            this.tsbSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbSettings.Name = "tsbSettings";
            this.tsbSettings.Text = "Exclusions…";
            this.tsbSettings.ToolTipText = "Exclude tables/columns whose logical name starts with the given prefixes";
            this.tsbSettings.Click += new System.EventHandler(this.tsbSettings_Click);
            //
            // tssSeparator2
            //
            this.tssSeparator2.Name = "tssSeparator2";
            //
            // tsbExportCsv
            //
            this.tsbExportCsv.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExportCsv.Enabled = false;
            this.tsbExportCsv.Name = "tsbExportCsv";
            this.tsbExportCsv.Text = "Export CSV";
            this.tsbExportCsv.Click += new System.EventHandler(this.tsbExportCsv_Click);
            //
            // tsbExportHtml
            //
            this.tsbExportHtml.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExportHtml.Enabled = false;
            this.tsbExportHtml.Name = "tsbExportHtml";
            this.tsbExportHtml.Text = "Export report (HTML)";
            this.tsbExportHtml.Click += new System.EventHandler(this.tsbExportHtml_Click);
            //
            // lvResults
            //
            this.lvResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colTable,
                this.colColumn,
                this.colDisplay,
                this.colType,
                this.colManaged,
                this.colUsed,
                this.colUsage});
            this.lvResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvResults.FullRowSelect = true;
            this.lvResults.GridLines = true;
            this.lvResults.Location = new System.Drawing.Point(0, 25);
            this.lvResults.Name = "lvResults";
            this.lvResults.Size = new System.Drawing.Size(900, 575);
            this.lvResults.TabIndex = 1;
            this.lvResults.UseCompatibleStateImageBehavior = false;
            this.lvResults.View = System.Windows.Forms.View.Details;
            this.lvResults.VirtualMode = true; // render only visible rows — keeps large audits responsive
            this.lvResults.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvResults_ColumnClick);
            this.lvResults.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.lvResults_RetrieveVirtualItem);
            //
            // columns
            //
            this.colTable.Text = "Table";
            this.colTable.Width = 160;
            this.colColumn.Text = "Column";
            this.colColumn.Width = 180;
            this.colDisplay.Text = "Display name";
            this.colDisplay.Width = 180;
            this.colType.Text = "Type";
            this.colType.Width = 90;
            this.colManaged.Text = "Managed";
            this.colManaged.Width = 70;
            this.colUsed.Text = "Used";
            this.colUsed.Width = 50;
            this.colUsage.Text = "Usage evidence";
            this.colUsage.Width = 320;
            //
            // AttributeAuditorControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lvResults);
            this.Controls.Add(this.toolStrip);
            this.Name = "AttributeAuditorControl";
            this.Size = new System.Drawing.Size(900, 600);
            this.Load += new System.EventHandler(this.AttributeAuditorControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbRun;
        private System.Windows.Forms.ToolStripButton tsbCustomOnly;
        private System.Windows.Forms.ToolStripButton tsbCandidatesOnly;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbSettings;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripButton tsbExportCsv;
        private System.Windows.Forms.ToolStripButton tsbExportHtml;
        private System.Windows.Forms.ListView lvResults;
        private System.Windows.Forms.ColumnHeader colTable;
        private System.Windows.Forms.ColumnHeader colColumn;
        private System.Windows.Forms.ColumnHeader colDisplay;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colManaged;
        private System.Windows.Forms.ColumnHeader colUsed;
        private System.Windows.Forms.ColumnHeader colUsage;
    }
}
