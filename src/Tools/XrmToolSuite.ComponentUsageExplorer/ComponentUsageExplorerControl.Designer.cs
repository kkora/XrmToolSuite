namespace XrmToolSuite.ComponentUsageExplorer
{
    partial class ComponentUsageExplorerControl
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
            this.tslSearch = new System.Windows.Forms.ToolStripLabel();
            this.tstSearch = new System.Windows.Forms.ToolStripTextBox();
            this.tscbType = new System.Windows.Forms.ToolStripComboBox();
            this.tsbFind = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbAnalyze = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.pnlResults = new System.Windows.Forms.Panel();
            this.grdResults = new System.Windows.Forms.DataGridView();
            this.colResType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResSchema = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResSolutions = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colResManaged = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblResultsHeader = new System.Windows.Forms.Label();
            this.pnlDetails = new System.Windows.Forms.Panel();
            this.splitDetails = new System.Windows.Forms.SplitContainer();
            this.splitLists = new System.Windows.Forms.SplitContainer();
            this.pnlRequired = new System.Windows.Forms.Panel();
            this.grdRequired = new System.Windows.Forms.DataGridView();
            this.colReqType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colReqName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colReqSolutions = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblRequiredHeader = new System.Windows.Forms.Label();
            this.pnlDependents = new System.Windows.Forms.Panel();
            this.grdDependents = new System.Windows.Forms.DataGridView();
            this.colDepType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDepName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDepSolutions = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDepManaged = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblDependentsHeader = new System.Windows.Forms.Label();
            this.splitBottom = new System.Windows.Forms.SplitContainer();
            this.pnlUsage = new System.Windows.Forms.Panel();
            this.grdUsage = new System.Windows.Forms.DataGridView();
            this.colUsageType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colUsageCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblUsageHeader = new System.Windows.Forms.Label();
            this.pnlFindings = new System.Windows.Forms.Panel();
            this.grdFindings = new System.Windows.Forms.DataGridView();
            this.colSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtExplanation = new System.Windows.Forms.TextBox();
            this.lblFindingsHeader = new System.Windows.Forms.Label();
            this.lblVerdict = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            this.pnlResults.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdResults)).BeginInit();
            this.pnlDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitDetails)).BeginInit();
            this.splitDetails.Panel1.SuspendLayout();
            this.splitDetails.Panel2.SuspendLayout();
            this.splitDetails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitLists)).BeginInit();
            this.splitLists.Panel1.SuspendLayout();
            this.splitLists.Panel2.SuspendLayout();
            this.splitLists.SuspendLayout();
            this.pnlRequired.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdRequired)).BeginInit();
            this.pnlDependents.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdDependents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).BeginInit();
            this.splitBottom.Panel1.SuspendLayout();
            this.splitBottom.Panel2.SuspendLayout();
            this.splitBottom.SuspendLayout();
            this.pnlUsage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdUsage)).BeginInit();
            this.pnlFindings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tslSearch,
                this.tstSearch,
                this.tscbType,
                this.tsbFind,
                this.tssSeparator1,
                this.tsbAnalyze,
                this.tssSeparator2,
                this.tsbExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(960, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tslSearch
            //
            this.tslSearch.Name = "tslSearch";
            this.tslSearch.Text = "Search:";
            //
            // tstSearch
            //
            this.tstSearch.Name = "tstSearch";
            this.tstSearch.Size = new System.Drawing.Size(200, 25);
            this.tstSearch.ToolTipText = "Display name, schema name, or GUID of the component to find";
            //
            // tscbType
            //
            this.tscbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscbType.Name = "tscbType";
            this.tscbType.Size = new System.Drawing.Size(180, 25);
            this.tscbType.ToolTipText = "Narrow the search to one component type";
            //
            // tsbFind
            //
            this.tsbFind.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbFind.Name = "tsbFind";
            this.tsbFind.Text = "Find";
            this.tsbFind.ToolTipText = "Search the connected environment for matching components";
            this.tsbFind.Click += new System.EventHandler(this.tsbFind_Click);
            //
            // tssSeparator1
            //
            this.tssSeparator1.Name = "tssSeparator1";
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze usage";
            this.tsbAnalyze.ToolTipText = "Build the where-used footprint and change-safety verdict for the selected component";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
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
            this.tsmExportJson.Text = "JSON (CI-friendly)";
            this.tsmExportJson.Click += new System.EventHandler(this.tsmExportJson_Click);
            //
            // tsmExportHtml
            //
            this.tsmExportHtml.Name = "tsmExportHtml";
            this.tsmExportHtml.Text = "HTML report";
            this.tsmExportHtml.Click += new System.EventHandler(this.tsmExportHtml_Click);
            //
            // splitMain
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 25);
            this.splitMain.Name = "splitMain";
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitMain.Panel1.Controls.Add(this.pnlResults);
            this.splitMain.Panel1MinSize = 120;
            this.splitMain.Panel2.Controls.Add(this.pnlDetails);
            this.splitMain.Size = new System.Drawing.Size(960, 575);
            this.splitMain.SplitterDistance = 230;
            this.splitMain.TabIndex = 1;
            //
            // pnlResults
            //
            this.pnlResults.Controls.Add(this.grdResults);
            this.pnlResults.Controls.Add(this.lblResultsHeader);
            this.pnlResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlResults.Name = "pnlResults";
            //
            // grdResults
            //
            this.grdResults.AllowUserToAddRows = false;
            this.grdResults.AllowUserToDeleteRows = false;
            this.grdResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colResType,
                this.colResName,
                this.colResSchema,
                this.colResSolutions,
                this.colResManaged});
            this.grdResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdResults.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdResults.MultiSelect = false;
            this.grdResults.Name = "grdResults";
            this.grdResults.ReadOnly = true;
            this.grdResults.RowHeadersVisible = false;
            this.grdResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdResults.TabIndex = 1;
            //
            // colResType
            //
            this.colResType.HeaderText = "Type";
            this.colResType.Name = "colResType";
            this.colResType.FillWeight = 18F;
            //
            // colResName
            //
            this.colResName.HeaderText = "Name";
            this.colResName.Name = "colResName";
            this.colResName.FillWeight = 26F;
            //
            // colResSchema
            //
            this.colResSchema.HeaderText = "Schema name";
            this.colResSchema.Name = "colResSchema";
            this.colResSchema.FillWeight = 22F;
            //
            // colResSolutions
            //
            this.colResSolutions.HeaderText = "Owning solution(s)";
            this.colResSolutions.Name = "colResSolutions";
            this.colResSolutions.FillWeight = 26F;
            //
            // colResManaged
            //
            this.colResManaged.HeaderText = "Managed";
            this.colResManaged.Name = "colResManaged";
            this.colResManaged.FillWeight = 10F;
            //
            // lblResultsHeader
            //
            this.lblResultsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblResultsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblResultsHeader.Height = 20;
            this.lblResultsHeader.Name = "lblResultsHeader";
            this.lblResultsHeader.Text = "Search results — type a name/GUID and click Find, then select a component";
            //
            // pnlDetails
            //
            this.pnlDetails.Controls.Add(this.splitDetails);
            this.pnlDetails.Controls.Add(this.lblVerdict);
            this.pnlDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDetails.Name = "pnlDetails";
            //
            // splitDetails
            //
            this.splitDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitDetails.Name = "splitDetails";
            this.splitDetails.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitDetails.Panel1.Controls.Add(this.splitLists);
            this.splitDetails.Panel2.Controls.Add(this.splitBottom);
            this.splitDetails.Size = new System.Drawing.Size(960, 293);
            this.splitDetails.SplitterDistance = 150;
            this.splitDetails.TabIndex = 1;
            //
            // splitLists
            //
            this.splitLists.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitLists.Name = "splitLists";
            this.splitLists.Panel1.Controls.Add(this.pnlRequired);
            this.splitLists.Panel2.Controls.Add(this.pnlDependents);
            this.splitLists.Size = new System.Drawing.Size(960, 150);
            this.splitLists.SplitterDistance = 420;
            this.splitLists.TabIndex = 0;
            //
            // pnlRequired
            //
            this.pnlRequired.Controls.Add(this.grdRequired);
            this.pnlRequired.Controls.Add(this.lblRequiredHeader);
            this.pnlRequired.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRequired.Name = "pnlRequired";
            //
            // grdRequired
            //
            this.grdRequired.AllowUserToAddRows = false;
            this.grdRequired.AllowUserToDeleteRows = false;
            this.grdRequired.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdRequired.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdRequired.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colReqType,
                this.colReqName,
                this.colReqSolutions});
            this.grdRequired.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdRequired.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdRequired.MultiSelect = false;
            this.grdRequired.Name = "grdRequired";
            this.grdRequired.ReadOnly = true;
            this.grdRequired.RowHeadersVisible = false;
            this.grdRequired.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdRequired.TabIndex = 1;
            //
            // colReqType
            //
            this.colReqType.HeaderText = "Type";
            this.colReqType.Name = "colReqType";
            this.colReqType.FillWeight = 30F;
            //
            // colReqName
            //
            this.colReqName.HeaderText = "Name";
            this.colReqName.Name = "colReqName";
            this.colReqName.FillWeight = 40F;
            //
            // colReqSolutions
            //
            this.colReqSolutions.HeaderText = "Solution(s)";
            this.colReqSolutions.Name = "colReqSolutions";
            this.colReqSolutions.FillWeight = 30F;
            //
            // lblRequiredHeader
            //
            this.lblRequiredHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRequiredHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblRequiredHeader.Height = 20;
            this.lblRequiredHeader.Name = "lblRequiredHeader";
            this.lblRequiredHeader.Text = "Required components (this component depends on)";
            //
            // pnlDependents
            //
            this.pnlDependents.Controls.Add(this.grdDependents);
            this.pnlDependents.Controls.Add(this.lblDependentsHeader);
            this.pnlDependents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDependents.Name = "pnlDependents";
            //
            // grdDependents
            //
            this.grdDependents.AllowUserToAddRows = false;
            this.grdDependents.AllowUserToDeleteRows = false;
            this.grdDependents.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdDependents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdDependents.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colDepType,
                this.colDepName,
                this.colDepSolutions,
                this.colDepManaged});
            this.grdDependents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdDependents.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdDependents.MultiSelect = false;
            this.grdDependents.Name = "grdDependents";
            this.grdDependents.ReadOnly = true;
            this.grdDependents.RowHeadersVisible = false;
            this.grdDependents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdDependents.TabIndex = 1;
            //
            // colDepType
            //
            this.colDepType.HeaderText = "Type";
            this.colDepType.Name = "colDepType";
            this.colDepType.FillWeight = 26F;
            //
            // colDepName
            //
            this.colDepName.HeaderText = "Name";
            this.colDepName.Name = "colDepName";
            this.colDepName.FillWeight = 34F;
            //
            // colDepSolutions
            //
            this.colDepSolutions.HeaderText = "Solution(s)";
            this.colDepSolutions.Name = "colDepSolutions";
            this.colDepSolutions.FillWeight = 28F;
            //
            // colDepManaged
            //
            this.colDepManaged.HeaderText = "Managed";
            this.colDepManaged.Name = "colDepManaged";
            this.colDepManaged.FillWeight = 12F;
            //
            // lblDependentsHeader
            //
            this.lblDependentsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDependentsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDependentsHeader.Height = 20;
            this.lblDependentsHeader.Name = "lblDependentsHeader";
            this.lblDependentsHeader.Text = "Dependent components (depend on this component)";
            //
            // splitBottom
            //
            this.splitBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitBottom.Name = "splitBottom";
            this.splitBottom.Panel1.Controls.Add(this.pnlUsage);
            this.splitBottom.Panel2.Controls.Add(this.pnlFindings);
            this.splitBottom.Size = new System.Drawing.Size(960, 139);
            this.splitBottom.SplitterDistance = 240;
            this.splitBottom.TabIndex = 0;
            //
            // pnlUsage
            //
            this.pnlUsage.Controls.Add(this.grdUsage);
            this.pnlUsage.Controls.Add(this.lblUsageHeader);
            this.pnlUsage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlUsage.Name = "pnlUsage";
            //
            // grdUsage
            //
            this.grdUsage.AllowUserToAddRows = false;
            this.grdUsage.AllowUserToDeleteRows = false;
            this.grdUsage.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdUsage.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdUsage.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colUsageType,
                this.colUsageCount});
            this.grdUsage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdUsage.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdUsage.MultiSelect = false;
            this.grdUsage.Name = "grdUsage";
            this.grdUsage.ReadOnly = true;
            this.grdUsage.RowHeadersVisible = false;
            this.grdUsage.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdUsage.TabIndex = 1;
            //
            // colUsageType
            //
            this.colUsageType.HeaderText = "Usage by type";
            this.colUsageType.Name = "colUsageType";
            this.colUsageType.FillWeight = 70F;
            //
            // colUsageCount
            //
            this.colUsageCount.HeaderText = "Count";
            this.colUsageCount.Name = "colUsageCount";
            this.colUsageCount.FillWeight = 30F;
            //
            // lblUsageHeader
            //
            this.lblUsageHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblUsageHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblUsageHeader.Height = 20;
            this.lblUsageHeader.Name = "lblUsageHeader";
            this.lblUsageHeader.Text = "Usage summary (blast radius)";
            //
            // pnlFindings
            //
            this.pnlFindings.Controls.Add(this.grdFindings);
            this.pnlFindings.Controls.Add(this.txtExplanation);
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
                this.colDetail});
            this.grdFindings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdFindings.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdFindings.MultiSelect = false;
            this.grdFindings.Name = "grdFindings";
            this.grdFindings.ReadOnly = true;
            this.grdFindings.RowHeadersVisible = false;
            this.grdFindings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdFindings.TabIndex = 2;
            //
            // colSeverity
            //
            this.colSeverity.HeaderText = "Severity";
            this.colSeverity.Name = "colSeverity";
            this.colSeverity.FillWeight = 16F;
            //
            // colTitle
            //
            this.colTitle.HeaderText = "Finding";
            this.colTitle.Name = "colTitle";
            this.colTitle.FillWeight = 34F;
            //
            // colDetail
            //
            this.colDetail.HeaderText = "Detail";
            this.colDetail.Name = "colDetail";
            this.colDetail.FillWeight = 50F;
            //
            // txtExplanation
            //
            this.txtExplanation.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtExplanation.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtExplanation.Height = 56;
            this.txtExplanation.Multiline = true;
            this.txtExplanation.Name = "txtExplanation";
            this.txtExplanation.ReadOnly = true;
            this.txtExplanation.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtExplanation.TabIndex = 1;
            //
            // lblFindingsHeader
            //
            this.lblFindingsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFindingsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblFindingsHeader.Height = 20;
            this.lblFindingsHeader.Name = "lblFindingsHeader";
            this.lblFindingsHeader.Text = "Findings & recommendation";
            //
            // lblVerdict
            //
            this.lblVerdict.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblVerdict.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblVerdict.Height = 44;
            this.lblVerdict.Name = "lblVerdict";
            this.lblVerdict.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblVerdict.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblVerdict.Text = "Select a component and click 'Analyze usage' for a change-safety verdict.";
            //
            // ComponentUsageExplorerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.toolStrip);
            this.Name = "ComponentUsageExplorerControl";
            this.Size = new System.Drawing.Size(960, 600);
            this.Load += new System.EventHandler(this.ComponentUsageExplorerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.pnlResults.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdResults)).EndInit();
            this.pnlDetails.ResumeLayout(false);
            this.splitDetails.Panel1.ResumeLayout(false);
            this.splitDetails.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitDetails)).EndInit();
            this.splitDetails.ResumeLayout(false);
            this.splitLists.Panel1.ResumeLayout(false);
            this.splitLists.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitLists)).EndInit();
            this.splitLists.ResumeLayout(false);
            this.pnlRequired.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdRequired)).EndInit();
            this.pnlDependents.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdDependents)).EndInit();
            this.splitBottom.Panel1.ResumeLayout(false);
            this.splitBottom.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).EndInit();
            this.splitBottom.ResumeLayout(false);
            this.pnlUsage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdUsage)).EndInit();
            this.pnlFindings.ResumeLayout(false);
            this.pnlFindings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripLabel tslSearch;
        private System.Windows.Forms.ToolStripTextBox tstSearch;
        private System.Windows.Forms.ToolStripComboBox tscbType;
        private System.Windows.Forms.ToolStripButton tsbFind;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem tsmExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmExportPdf;
        private System.Windows.Forms.ToolStripMenuItem tsmExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmExportHtml;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel pnlResults;
        private System.Windows.Forms.DataGridView grdResults;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResSchema;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResSolutions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colResManaged;
        private System.Windows.Forms.Label lblResultsHeader;
        private System.Windows.Forms.Panel pnlDetails;
        private System.Windows.Forms.SplitContainer splitDetails;
        private System.Windows.Forms.SplitContainer splitLists;
        private System.Windows.Forms.Panel pnlRequired;
        private System.Windows.Forms.DataGridView grdRequired;
        private System.Windows.Forms.DataGridViewTextBoxColumn colReqType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colReqName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colReqSolutions;
        private System.Windows.Forms.Label lblRequiredHeader;
        private System.Windows.Forms.Panel pnlDependents;
        private System.Windows.Forms.DataGridView grdDependents;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDepType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDepName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDepSolutions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDepManaged;
        private System.Windows.Forms.Label lblDependentsHeader;
        private System.Windows.Forms.SplitContainer splitBottom;
        private System.Windows.Forms.Panel pnlUsage;
        private System.Windows.Forms.DataGridView grdUsage;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUsageType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUsageCount;
        private System.Windows.Forms.Label lblUsageHeader;
        private System.Windows.Forms.Panel pnlFindings;
        private System.Windows.Forms.DataGridView grdFindings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDetail;
        private System.Windows.Forms.TextBox txtExplanation;
        private System.Windows.Forms.Label lblFindingsHeader;
        private System.Windows.Forms.Label lblVerdict;
    }
}
