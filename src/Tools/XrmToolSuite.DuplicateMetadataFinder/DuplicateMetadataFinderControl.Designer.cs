namespace XrmToolSuite.DuplicateMetadataFinder
{
    partial class DuplicateMetadataFinderControl
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
            this.tsbScan = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsddKinds = new System.Windows.Forms.ToolStripDropDownButton();
            this.tslThreshold = new System.Windows.Forms.ToolStripLabel();
            this.tstThreshold = new System.Windows.Forms.ToolStripTextBox();
            this.tsbCustomOnly = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsddExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmiExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.split = new System.Windows.Forms.SplitContainer();
            this.grdGroups = new System.Windows.Forms.DataGridView();
            this.colKind = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMembers = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKeep = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtDetail = new System.Windows.Forms.TextBox();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdGroups)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbScan,
                this.tssSeparator1,
                this.tsddKinds,
                this.tslThreshold,
                this.tstThreshold,
                this.tsbCustomOnly,
                this.tssSeparator2,
                this.tsddExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(900, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbScan
            //
            this.tsbScan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbScan.Name = "tsbScan";
            this.tsbScan.Text = "Scan for duplicates";
            this.tsbScan.Click += new System.EventHandler(this.tsbScan_Click);
            //
            // tsddKinds
            //
            this.tsddKinds.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsddKinds.Name = "tsddKinds";
            this.tsddKinds.Text = "Component types";
            //
            // tslThreshold
            //
            this.tslThreshold.Name = "tslThreshold";
            this.tslThreshold.Text = "Similarity ≥";
            //
            // tstThreshold
            //
            this.tstThreshold.Name = "tstThreshold";
            this.tstThreshold.Size = new System.Drawing.Size(40, 25);
            this.tstThreshold.Text = "80";
            this.tstThreshold.ToolTipText = "Similarity threshold 0-100";
            //
            // tsbCustomOnly
            //
            this.tsbCustomOnly.CheckOnClick = true;
            this.tsbCustomOnly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCustomOnly.Name = "tsbCustomOnly";
            this.tsbCustomOnly.Text = "Custom only";
            this.tsbCustomOnly.ToolTipText = "Skip managed/system components";
            //
            // tsddExport
            //
            this.tsddExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsddExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsmiExportExcel,
                this.tsmiExportPdf,
                this.tsmiExportJson,
                this.tsmiExportHtml});
            this.tsddExport.Enabled = false;
            this.tsddExport.Name = "tsddExport";
            this.tsddExport.Text = "Export";
            //
            // tsmiExportExcel
            //
            this.tsmiExportExcel.Name = "tsmiExportExcel";
            this.tsmiExportExcel.Text = "Excel (.xlsx)";
            this.tsmiExportExcel.Click += new System.EventHandler(this.tsmiExportExcel_Click);
            //
            // tsmiExportPdf
            //
            this.tsmiExportPdf.Name = "tsmiExportPdf";
            this.tsmiExportPdf.Text = "PDF (.pdf)";
            this.tsmiExportPdf.Click += new System.EventHandler(this.tsmiExportPdf_Click);
            //
            // tsmiExportJson
            //
            this.tsmiExportJson.Name = "tsmiExportJson";
            this.tsmiExportJson.Text = "JSON (.json)";
            this.tsmiExportJson.Click += new System.EventHandler(this.tsmiExportJson_Click);
            //
            // tsmiExportHtml
            //
            this.tsmiExportHtml.Name = "tsmiExportHtml";
            this.tsmiExportHtml.Text = "HTML (.html)";
            this.tsmiExportHtml.Click += new System.EventHandler(this.tsmiExportHtml_Click);
            //
            // split
            //
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Location = new System.Drawing.Point(0, 25);
            this.split.Name = "split";
            this.split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.split.Panel1.Controls.Add(this.grdGroups);
            this.split.Panel2.Controls.Add(this.txtDetail);
            this.split.Size = new System.Drawing.Size(900, 575);
            this.split.SplitterDistance = 360;
            this.split.TabIndex = 1;
            //
            // grdGroups
            //
            this.grdGroups.AllowUserToAddRows = false;
            this.grdGroups.AllowUserToDeleteRows = false;
            this.grdGroups.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdGroups.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colKind,
                this.colMembers,
                this.colScore,
                this.colKeep});
            this.grdGroups.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdGroups.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdGroups.MultiSelect = false;
            this.grdGroups.Name = "grdGroups";
            this.grdGroups.ReadOnly = true;
            this.grdGroups.RowHeadersVisible = false;
            this.grdGroups.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdGroups.TabIndex = 0;
            this.grdGroups.SelectionChanged += new System.EventHandler(this.grdGroups_SelectionChanged);
            //
            // colKind
            //
            this.colKind.HeaderText = "Type";
            this.colKind.Name = "colKind";
            this.colKind.FillWeight = 15F;
            //
            // colMembers
            //
            this.colMembers.HeaderText = "Members";
            this.colMembers.Name = "colMembers";
            this.colMembers.FillWeight = 50F;
            //
            // colScore
            //
            this.colScore.HeaderText = "Top %";
            this.colScore.Name = "colScore";
            this.colScore.FillWeight = 10F;
            //
            // colKeep
            //
            this.colKeep.HeaderText = "Recommended keep";
            this.colKeep.Name = "colKeep";
            this.colKeep.FillWeight = 25F;
            //
            // txtDetail
            //
            this.txtDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDetail.Multiline = true;
            this.txtDetail.Name = "txtDetail";
            this.txtDetail.ReadOnly = true;
            this.txtDetail.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtDetail.WordWrap = false;
            this.txtDetail.TabIndex = 0;
            this.txtDetail.Font = new System.Drawing.Font("Consolas", 9F);
            //
            // DuplicateMetadataFinderControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.split);
            this.Controls.Add(this.toolStrip);
            this.Name = "DuplicateMetadataFinderControl";
            this.Size = new System.Drawing.Size(900, 600);
            this.Load += new System.EventHandler(this.DuplicateMetadataFinderControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            this.split.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdGroups)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbScan;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripDropDownButton tsddKinds;
        private System.Windows.Forms.ToolStripLabel tslThreshold;
        private System.Windows.Forms.ToolStripTextBox tstThreshold;
        private System.Windows.Forms.ToolStripButton tsbCustomOnly;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsddExport;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportPdf;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportHtml;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.DataGridView grdGroups;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKind;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMembers;
        private System.Windows.Forms.DataGridViewTextBoxColumn colScore;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKeep;
        private System.Windows.Forms.TextBox txtDetail;
    }
}
