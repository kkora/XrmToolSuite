namespace XrmToolSuite.EnvironmentInventory
{
    partial class EnvironmentInventoryControl
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
            this.tsbCollect = new System.Windows.Forms.ToolStripButton();
            this.tsdSources = new System.Windows.Forms.ToolStripDropDownButton();
            this.tss1 = new System.Windows.Forms.ToolStripSeparator();
            this.tslCategory = new System.Windows.Forms.ToolStripLabel();
            this.cboCategory = new System.Windows.Forms.ToolStripComboBox();
            this.cboManaged = new System.Windows.Forms.ToolStripComboBox();
            this.tslSearch = new System.Windows.Forms.ToolStripLabel();
            this.txtSearch = new System.Windows.Forms.ToolStripTextBox();
            this.tss2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsdExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmiExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportMarkdown = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportWord = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.split = new System.Windows.Forms.SplitContainer();
            this.grdInventory = new System.Windows.Forms.DataGridView();
            this.colCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSchema = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colManaged = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colModified = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtDetail = new System.Windows.Forms.TextBox();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdInventory)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbCollect,
                this.tsdSources,
                this.tss1,
                this.tslCategory,
                this.cboCategory,
                this.cboManaged,
                this.tslSearch,
                this.txtSearch,
                this.tss2,
                this.tsdExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(900, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbCollect
            //
            this.tsbCollect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCollect.Name = "tsbCollect";
            this.tsbCollect.Text = "Load inventory";
            this.tsbCollect.Click += new System.EventHandler(this.tsbCollect_Click);
            //
            // tsdSources
            //
            this.tsdSources.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsdSources.Name = "tsdSources";
            this.tsdSources.Text = "Sources";
            //
            // tslCategory
            //
            this.tslCategory.Name = "tslCategory";
            this.tslCategory.Text = "Category:";
            //
            // cboCategory
            //
            this.cboCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCategory.Name = "cboCategory";
            this.cboCategory.Size = new System.Drawing.Size(140, 25);
            this.cboCategory.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);
            //
            // cboManaged
            //
            this.cboManaged.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboManaged.Name = "cboManaged";
            this.cboManaged.Size = new System.Drawing.Size(110, 25);
            this.cboManaged.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);
            //
            // tslSearch
            //
            this.tslSearch.Name = "tslSearch";
            this.tslSearch.Text = "Search:";
            //
            // txtSearch
            //
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(160, 25);
            this.txtSearch.TextChanged += new System.EventHandler(this.Filter_Changed);
            //
            // tsdExport
            //
            this.tsdExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsdExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsmiExportExcel,
                this.tsmiExportCsv,
                this.tsmiExportJson,
                this.tsmiExportMarkdown,
                this.tsmiExportHtml,
                this.tsmiExportWord,
                this.tsmiExportPdf});
            this.tsdExport.Name = "tsdExport";
            this.tsdExport.Text = "Export";
            this.tsdExport.Enabled = false;
            //
            // tsmiExportCsv
            //
            this.tsmiExportCsv.Name = "tsmiExportCsv";
            this.tsmiExportCsv.Text = "CSV…";
            this.tsmiExportCsv.Click += new System.EventHandler(this.tsmiExportCsv_Click);
            //
            // tsmiExportJson
            //
            this.tsmiExportJson.Name = "tsmiExportJson";
            this.tsmiExportJson.Text = "JSON…";
            this.tsmiExportJson.Click += new System.EventHandler(this.tsmiExportJson_Click);
            //
            // tsmiExportMarkdown
            //
            this.tsmiExportMarkdown.Name = "tsmiExportMarkdown";
            this.tsmiExportMarkdown.Text = "Markdown…";
            this.tsmiExportMarkdown.Click += new System.EventHandler(this.tsmiExportMarkdown_Click);
            //
            // tsmiExportHtml
            //
            this.tsmiExportHtml.Name = "tsmiExportHtml";
            this.tsmiExportHtml.Text = "HTML…";
            this.tsmiExportHtml.Click += new System.EventHandler(this.tsmiExportHtml_Click);
            //
            // tsmiExportExcel
            //
            this.tsmiExportExcel.Name = "tsmiExportExcel";
            this.tsmiExportExcel.Text = "Excel (*.xlsx)…";
            this.tsmiExportExcel.Click += new System.EventHandler(this.tsmiExportExcel_Click);
            //
            // tsmiExportWord
            //
            this.tsmiExportWord.Name = "tsmiExportWord";
            this.tsmiExportWord.Text = "Word (*.docx)…";
            this.tsmiExportWord.Click += new System.EventHandler(this.tsmiExportWord_Click);
            //
            // tsmiExportPdf
            //
            this.tsmiExportPdf.Name = "tsmiExportPdf";
            this.tsmiExportPdf.Text = "PDF (*.pdf)…";
            this.tsmiExportPdf.Click += new System.EventHandler(this.tsmiExportPdf_Click);
            //
            // split
            //
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Location = new System.Drawing.Point(0, 25);
            this.split.Name = "split";
            this.split.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.split.Panel1.Controls.Add(this.grdInventory);
            this.split.Panel2.Controls.Add(this.txtDetail);
            this.split.Size = new System.Drawing.Size(900, 575);
            this.split.SplitterDistance = 420;
            this.split.TabIndex = 1;
            //
            // grdInventory
            //
            this.grdInventory.AllowUserToAddRows = false;
            this.grdInventory.AllowUserToDeleteRows = false;
            this.grdInventory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdInventory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdInventory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colCategory,
                this.colType,
                this.colName,
                this.colSchema,
                this.colManaged,
                this.colModified});
            this.grdInventory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdInventory.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdInventory.MultiSelect = false;
            this.grdInventory.Name = "grdInventory";
            this.grdInventory.ReadOnly = true;
            this.grdInventory.RowHeadersVisible = false;
            this.grdInventory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdInventory.TabIndex = 0;
            this.grdInventory.SelectionChanged += new System.EventHandler(this.grdInventory_SelectionChanged);
            //
            // columns
            //
            this.colCategory.HeaderText = "Category";
            this.colCategory.Name = "colCategory";
            this.colCategory.FillWeight = 60F;
            this.colType.HeaderText = "Type";
            this.colType.Name = "colType";
            this.colType.FillWeight = 70F;
            this.colName.HeaderText = "Name";
            this.colName.Name = "colName";
            this.colName.FillWeight = 110F;
            this.colSchema.HeaderText = "Schema";
            this.colSchema.Name = "colSchema";
            this.colSchema.FillWeight = 110F;
            this.colManaged.HeaderText = "Managed";
            this.colManaged.Name = "colManaged";
            this.colManaged.FillWeight = 50F;
            this.colModified.HeaderText = "Modified";
            this.colModified.Name = "colModified";
            this.colModified.FillWeight = 70F;
            //
            // txtDetail
            //
            this.txtDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDetail.Multiline = true;
            this.txtDetail.Name = "txtDetail";
            this.txtDetail.ReadOnly = true;
            this.txtDetail.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDetail.TabIndex = 0;
            this.txtDetail.Font = new System.Drawing.Font("Consolas", 9F);
            //
            // EnvironmentInventoryControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.split);
            this.Controls.Add(this.toolStrip);
            this.Name = "EnvironmentInventoryControl";
            this.Size = new System.Drawing.Size(900, 600);
            this.Load += new System.EventHandler(this.EnvironmentInventoryControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            this.split.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdInventory)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbCollect;
        private System.Windows.Forms.ToolStripDropDownButton tsdSources;
        private System.Windows.Forms.ToolStripSeparator tss1;
        private System.Windows.Forms.ToolStripLabel tslCategory;
        private System.Windows.Forms.ToolStripComboBox cboCategory;
        private System.Windows.Forms.ToolStripComboBox cboManaged;
        private System.Windows.Forms.ToolStripLabel tslSearch;
        private System.Windows.Forms.ToolStripTextBox txtSearch;
        private System.Windows.Forms.ToolStripSeparator tss2;
        private System.Windows.Forms.ToolStripDropDownButton tsdExport;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportCsv;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportMarkdown;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportHtml;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportWord;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportPdf;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.DataGridView grdInventory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCategory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSchema;
        private System.Windows.Forms.DataGridViewTextBoxColumn colManaged;
        private System.Windows.Forms.DataGridViewTextBoxColumn colModified;
        private System.Windows.Forms.TextBox txtDetail;
    }
}
