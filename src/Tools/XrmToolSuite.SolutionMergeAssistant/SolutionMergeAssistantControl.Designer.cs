namespace XrmToolSuite.SolutionMergeAssistant
{
    partial class SolutionMergeAssistantControl
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
            this.tsbLoadSolutions = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbCompare = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tssSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.splitOuter = new System.Windows.Forms.SplitContainer();
            this.lstSolutions = new System.Windows.Forms.CheckedListBox();
            this.lblSolutions = new System.Windows.Forms.Label();
            this.pnlResults = new System.Windows.Forms.Panel();
            this.splitResults = new System.Windows.Forms.SplitContainer();
            this.grdConflicts = new System.Windows.Forms.DataGridView();
            this.colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colComponent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblConflicts = new System.Windows.Forms.Label();
            this.splitText = new System.Windows.Forms.SplitContainer();
            this.pnlStrategy = new System.Windows.Forms.Panel();
            this.txtStrategy = new System.Windows.Forms.TextBox();
            this.lblStrategy = new System.Windows.Forms.Label();
            this.pnlChecklist = new System.Windows.Forms.Panel();
            this.txtChecklist = new System.Windows.Forms.TextBox();
            this.lblChecklist = new System.Windows.Forms.Label();
            this.lblVerdict = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitOuter)).BeginInit();
            this.splitOuter.Panel1.SuspendLayout();
            this.splitOuter.Panel2.SuspendLayout();
            this.splitOuter.SuspendLayout();
            this.pnlResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitResults)).BeginInit();
            this.splitResults.Panel1.SuspendLayout();
            this.splitResults.Panel2.SuspendLayout();
            this.splitResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdConflicts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitText)).BeginInit();
            this.splitText.Panel1.SuspendLayout();
            this.splitText.Panel2.SuspendLayout();
            this.splitText.SuspendLayout();
            this.pnlStrategy.SuspendLayout();
            this.pnlChecklist.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoadSolutions,
                this.tssSeparator1,
                this.tsbCompare,
                this.tssSeparator2,
                this.tsbExport,
                this.tssSeparator3,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(960, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoadSolutions
            //
            this.tsbLoadSolutions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadSolutions.Name = "tsbLoadSolutions";
            this.tsbLoadSolutions.Text = "Load solutions";
            this.tsbLoadSolutions.ToolTipText = "Load the environment's solutions into the checklist on the left";
            this.tsbLoadSolutions.Click += new System.EventHandler(this.tsbLoadSolutions_Click);
            //
            // tssSeparator1
            //
            this.tssSeparator1.Name = "tssSeparator1";
            //
            // tsbCompare
            //
            this.tsbCompare.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCompare.Name = "tsbCompare";
            this.tsbCompare.Text = "Compare";
            this.tsbCompare.ToolTipText = "Compare the checked solutions and produce a merge verdict";
            this.tsbCompare.Click += new System.EventHandler(this.tsbCompare_Click);
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
                this.tsmExportHtml});
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
            this.tsmExportJson.Text = "JSON (verdict + conflicts)";
            this.tsmExportJson.Click += new System.EventHandler(this.tsmExportJson_Click);
            //
            // tsmExportHtml
            //
            this.tsmExportHtml.Name = "tsmExportHtml";
            this.tsmExportHtml.Text = "HTML report";
            this.tsmExportHtml.Click += new System.EventHandler(this.tsmExportHtml_Click);
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
            // splitOuter
            //
            this.splitOuter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitOuter.Location = new System.Drawing.Point(0, 25);
            this.splitOuter.Name = "splitOuter";
            this.splitOuter.Panel1.Controls.Add(this.lstSolutions);
            this.splitOuter.Panel1.Controls.Add(this.lblSolutions);
            this.splitOuter.Panel1MinSize = 180;
            this.splitOuter.Panel2.Controls.Add(this.pnlResults);
            this.splitOuter.Size = new System.Drawing.Size(960, 575);
            this.splitOuter.SplitterDistance = 280;
            this.splitOuter.TabIndex = 1;
            //
            // lstSolutions
            //
            this.lstSolutions.CheckOnClick = true;
            this.lstSolutions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstSolutions.IntegralHeight = false;
            this.lstSolutions.Name = "lstSolutions";
            this.lstSolutions.TabIndex = 1;
            //
            // lblSolutions
            //
            this.lblSolutions.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSolutions.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSolutions.Height = 40;
            this.lblSolutions.Name = "lblSolutions";
            this.lblSolutions.Padding = new System.Windows.Forms.Padding(4, 4, 4, 0);
            this.lblSolutions.Text = "Solutions — check 2 or more to compare";
            //
            // pnlResults
            //
            this.pnlResults.Controls.Add(this.splitResults);
            this.pnlResults.Controls.Add(this.lblVerdict);
            this.pnlResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlResults.Name = "pnlResults";
            //
            // splitResults
            //
            this.splitResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitResults.Location = new System.Drawing.Point(0, 48);
            this.splitResults.Name = "splitResults";
            this.splitResults.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitResults.Panel1.Controls.Add(this.grdConflicts);
            this.splitResults.Panel1.Controls.Add(this.lblConflicts);
            this.splitResults.Panel2.Controls.Add(this.splitText);
            this.splitResults.Size = new System.Drawing.Size(676, 527);
            this.splitResults.SplitterDistance = 300;
            this.splitResults.TabIndex = 1;
            //
            // grdConflicts
            //
            this.grdConflicts.AllowUserToAddRows = false;
            this.grdConflicts.AllowUserToDeleteRows = false;
            this.grdConflicts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdConflicts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdConflicts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colSeverity,
                this.colCategory,
                this.colComponent,
                this.colDetail});
            this.grdConflicts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdConflicts.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdConflicts.MultiSelect = false;
            this.grdConflicts.Name = "grdConflicts";
            this.grdConflicts.ReadOnly = true;
            this.grdConflicts.RowHeadersVisible = false;
            this.grdConflicts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdConflicts.TabIndex = 1;
            //
            // colSeverity
            //
            this.colSeverity.HeaderText = "Severity";
            this.colSeverity.Name = "colSeverity";
            this.colSeverity.FillWeight = 12F;
            //
            // colCategory
            //
            this.colCategory.HeaderText = "Category";
            this.colCategory.Name = "colCategory";
            this.colCategory.FillWeight = 18F;
            //
            // colComponent
            //
            this.colComponent.HeaderText = "Component";
            this.colComponent.Name = "colComponent";
            this.colComponent.FillWeight = 28F;
            //
            // colDetail
            //
            this.colDetail.HeaderText = "Conflict";
            this.colDetail.Name = "colDetail";
            this.colDetail.FillWeight = 55F;
            //
            // lblConflicts
            //
            this.lblConflicts.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblConflicts.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblConflicts.Height = 20;
            this.lblConflicts.Name = "lblConflicts";
            this.lblConflicts.Text = "Conflicts — compare solutions to populate";
            //
            // splitText
            //
            this.splitText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitText.Location = new System.Drawing.Point(0, 0);
            this.splitText.Name = "splitText";
            this.splitText.Panel1.Controls.Add(this.pnlStrategy);
            this.splitText.Panel2.Controls.Add(this.pnlChecklist);
            this.splitText.Size = new System.Drawing.Size(676, 223);
            this.splitText.SplitterDistance = 330;
            this.splitText.TabIndex = 0;
            //
            // pnlStrategy
            //
            this.pnlStrategy.Controls.Add(this.txtStrategy);
            this.pnlStrategy.Controls.Add(this.lblStrategy);
            this.pnlStrategy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlStrategy.Name = "pnlStrategy";
            //
            // txtStrategy
            //
            this.txtStrategy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtStrategy.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtStrategy.Multiline = true;
            this.txtStrategy.Name = "txtStrategy";
            this.txtStrategy.ReadOnly = true;
            this.txtStrategy.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStrategy.TabIndex = 1;
            //
            // lblStrategy
            //
            this.lblStrategy.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStrategy.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblStrategy.Height = 20;
            this.lblStrategy.Name = "lblStrategy";
            this.lblStrategy.Text = "Recommended merge strategy";
            //
            // pnlChecklist
            //
            this.pnlChecklist.Controls.Add(this.txtChecklist);
            this.pnlChecklist.Controls.Add(this.lblChecklist);
            this.pnlChecklist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlChecklist.Name = "pnlChecklist";
            //
            // txtChecklist
            //
            this.txtChecklist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtChecklist.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtChecklist.Multiline = true;
            this.txtChecklist.Name = "txtChecklist";
            this.txtChecklist.ReadOnly = true;
            this.txtChecklist.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtChecklist.WordWrap = false;
            this.txtChecklist.TabIndex = 1;
            //
            // lblChecklist
            //
            this.lblChecklist.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblChecklist.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblChecklist.Height = 20;
            this.lblChecklist.Name = "lblChecklist";
            this.lblChecklist.Text = "Merged-component checklist";
            //
            // lblVerdict
            //
            this.lblVerdict.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblVerdict.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblVerdict.Height = 48;
            this.lblVerdict.Name = "lblVerdict";
            this.lblVerdict.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblVerdict.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.lblVerdict.Text = "Load and check 2+ solutions, then Compare.";
            //
            // SolutionMergeAssistantControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitOuter);
            this.Controls.Add(this.toolStrip);
            this.Name = "SolutionMergeAssistantControl";
            this.Size = new System.Drawing.Size(960, 600);
            this.Load += new System.EventHandler(this.SolutionMergeAssistantControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitOuter.Panel1.ResumeLayout(false);
            this.splitOuter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitOuter)).EndInit();
            this.splitOuter.ResumeLayout(false);
            this.pnlResults.ResumeLayout(false);
            this.splitResults.Panel1.ResumeLayout(false);
            this.splitResults.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitResults)).EndInit();
            this.splitResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdConflicts)).EndInit();
            this.splitText.Panel1.ResumeLayout(false);
            this.splitText.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitText)).EndInit();
            this.splitText.ResumeLayout(false);
            this.pnlStrategy.ResumeLayout(false);
            this.pnlStrategy.PerformLayout();
            this.pnlChecklist.ResumeLayout(false);
            this.pnlChecklist.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoadSolutions;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbCompare;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem tsmExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmExportPdf;
        private System.Windows.Forms.ToolStripMenuItem tsmExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmExportHtml;
        private System.Windows.Forms.ToolStripSeparator tssSeparator3;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.SplitContainer splitOuter;
        private System.Windows.Forms.CheckedListBox lstSolutions;
        private System.Windows.Forms.Label lblSolutions;
        private System.Windows.Forms.Panel pnlResults;
        private System.Windows.Forms.SplitContainer splitResults;
        private System.Windows.Forms.DataGridView grdConflicts;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCategory;
        private System.Windows.Forms.DataGridViewTextBoxColumn colComponent;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetail;
        private System.Windows.Forms.Label lblConflicts;
        private System.Windows.Forms.SplitContainer splitText;
        private System.Windows.Forms.Panel pnlStrategy;
        private System.Windows.Forms.TextBox txtStrategy;
        private System.Windows.Forms.Label lblStrategy;
        private System.Windows.Forms.Panel pnlChecklist;
        private System.Windows.Forms.TextBox txtChecklist;
        private System.Windows.Forms.Label lblChecklist;
        private System.Windows.Forms.Label lblVerdict;
    }
}
