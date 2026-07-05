namespace XrmToolSuite.CustomApiExplorer
{
    partial class CustomApiExplorerControl
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
            this.tslSearch = new System.Windows.Forms.ToolStripLabel();
            this.tstSearch = new System.Windows.Forms.ToolStripTextBox();
            this.tssSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsddExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmiExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportMarkdown = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.tssSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.split = new System.Windows.Forms.SplitContainer();
            this.grdApis = new System.Windows.Forms.DataGridView();
            this.colApiName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colApiKind = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colApiBinding = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colApiPlugin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.split2 = new System.Windows.Forms.SplitContainer();
            this.txtDetail = new System.Windows.Forms.TextBox();
            this.pnlInvoke = new System.Windows.Forms.Panel();
            this.grdParams = new System.Windows.Forms.DataGridView();
            this.colParam = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colParamType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colParamOptional = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colParamValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.pnlInvokeTop = new System.Windows.Forms.Panel();
            this.lblConsole = new System.Windows.Forms.Label();
            this.lblTarget = new System.Windows.Forms.Label();
            this.txtTarget = new System.Windows.Forms.TextBox();
            this.btnInvoke = new System.Windows.Forms.Button();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdApis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.split2)).BeginInit();
            this.split2.Panel1.SuspendLayout();
            this.split2.Panel2.SuspendLayout();
            this.split2.SuspendLayout();
            this.pnlInvoke.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdParams)).BeginInit();
            this.pnlInvokeTop.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoad,
                this.tslSearch,
                this.tstSearch,
                this.tssSep1,
                this.tsddExport,
                this.tssSep2,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(950, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoad
            //
            this.tsbLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoad.Name = "tsbLoad";
            this.tsbLoad.Text = "Load Custom APIs";
            this.tsbLoad.Click += new System.EventHandler(this.tsbLoad_Click);
            //
            // tslSearch
            //
            this.tslSearch.Name = "tslSearch";
            this.tslSearch.Text = "Filter:";
            //
            // tstSearch
            //
            this.tstSearch.Name = "tstSearch";
            this.tstSearch.Size = new System.Drawing.Size(160, 25);
            this.tstSearch.TextChanged += new System.EventHandler(this.tstSearch_TextChanged);
            //
            // tssSep1
            //
            this.tssSep1.Name = "tssSep1";
            //
            // tsddExport
            //
            this.tsddExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsddExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsmiExportHtml,
                this.tsmiExportMarkdown,
                this.tsmiExportCsv});
            this.tsddExport.Enabled = false;
            this.tsddExport.Name = "tsddExport";
            this.tsddExport.Text = "Export catalog";
            //
            // tsmiExportHtml
            //
            this.tsmiExportHtml.Name = "tsmiExportHtml";
            this.tsmiExportHtml.Text = "HTML (.html)";
            this.tsmiExportHtml.Click += new System.EventHandler(this.tsmiExportHtml_Click);
            //
            // tsmiExportMarkdown
            //
            this.tsmiExportMarkdown.Name = "tsmiExportMarkdown";
            this.tsmiExportMarkdown.Text = "Markdown (.md)";
            this.tsmiExportMarkdown.Click += new System.EventHandler(this.tsmiExportMarkdown_Click);
            //
            // tsmiExportCsv
            //
            this.tsmiExportCsv.Name = "tsmiExportCsv";
            this.tsmiExportCsv.Text = "CSV (.csv)";
            this.tsmiExportCsv.Click += new System.EventHandler(this.tsmiExportCsv_Click);
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
            this.split.Panel1.Controls.Add(this.grdApis);
            this.split.Panel2.Controls.Add(this.split2);
            this.split.Size = new System.Drawing.Size(950, 575);
            this.split.SplitterDistance = 380;
            this.split.TabIndex = 1;
            //
            // grdApis
            //
            this.grdApis.AllowUserToAddRows = false;
            this.grdApis.AllowUserToDeleteRows = false;
            this.grdApis.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdApis.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colApiName,
                this.colApiKind,
                this.colApiBinding,
                this.colApiPlugin});
            this.grdApis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdApis.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdApis.MultiSelect = false;
            this.grdApis.Name = "grdApis";
            this.grdApis.ReadOnly = true;
            this.grdApis.RowHeadersVisible = false;
            this.grdApis.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdApis.TabIndex = 0;
            this.grdApis.SelectionChanged += new System.EventHandler(this.grdApis_SelectionChanged);
            //
            this.colApiName.HeaderText = "Unique name";
            this.colApiName.Name = "colApiName";
            this.colApiName.FillWeight = 40F;
            this.colApiKind.HeaderText = "Kind";
            this.colApiKind.Name = "colApiKind";
            this.colApiKind.FillWeight = 15F;
            this.colApiBinding.HeaderText = "Binding";
            this.colApiBinding.Name = "colApiBinding";
            this.colApiBinding.FillWeight = 20F;
            this.colApiPlugin.HeaderText = "Backing plugin";
            this.colApiPlugin.Name = "colApiPlugin";
            this.colApiPlugin.FillWeight = 25F;
            //
            // split2
            //
            this.split2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split2.Location = new System.Drawing.Point(0, 0);
            this.split2.Name = "split2";
            this.split2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.split2.Panel1.Controls.Add(this.txtDetail);
            this.split2.Panel2.Controls.Add(this.pnlInvoke);
            this.split2.Size = new System.Drawing.Size(566, 575);
            this.split2.SplitterDistance = 250;
            this.split2.TabIndex = 0;
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
            // pnlInvoke
            //
            this.pnlInvoke.Controls.Add(this.grdParams);
            this.pnlInvoke.Controls.Add(this.txtResult);
            this.pnlInvoke.Controls.Add(this.pnlInvokeTop);
            this.pnlInvoke.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInvoke.Name = "pnlInvoke";
            this.pnlInvoke.TabIndex = 0;
            //
            // grdParams
            //
            this.grdParams.AllowUserToAddRows = false;
            this.grdParams.AllowUserToDeleteRows = false;
            this.grdParams.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdParams.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colParam,
                this.colParamType,
                this.colParamOptional,
                this.colParamValue});
            this.grdParams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdParams.Name = "grdParams";
            this.grdParams.RowHeadersVisible = false;
            this.grdParams.TabIndex = 1;
            //
            this.colParam.HeaderText = "Parameter";
            this.colParam.Name = "colParam";
            this.colParam.ReadOnly = true;
            this.colParam.FillWeight = 35F;
            this.colParamType.HeaderText = "Type";
            this.colParamType.Name = "colParamType";
            this.colParamType.ReadOnly = true;
            this.colParamType.FillWeight = 20F;
            this.colParamOptional.HeaderText = "Optional";
            this.colParamOptional.Name = "colParamOptional";
            this.colParamOptional.ReadOnly = true;
            this.colParamOptional.FillWeight = 15F;
            this.colParamValue.HeaderText = "Value";
            this.colParamValue.Name = "colParamValue";
            this.colParamValue.FillWeight = 30F;
            //
            // txtResult
            //
            this.txtResult.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtResult.Height = 130;
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtResult.WordWrap = false;
            this.txtResult.TabIndex = 2;
            this.txtResult.Font = new System.Drawing.Font("Consolas", 9F);
            //
            // pnlInvokeTop
            //
            this.pnlInvokeTop.Controls.Add(this.btnInvoke);
            this.pnlInvokeTop.Controls.Add(this.txtTarget);
            this.pnlInvokeTop.Controls.Add(this.lblTarget);
            this.pnlInvokeTop.Controls.Add(this.lblConsole);
            this.pnlInvokeTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlInvokeTop.Height = 58;
            this.pnlInvokeTop.Name = "pnlInvokeTop";
            this.pnlInvokeTop.TabIndex = 0;
            //
            // lblConsole
            //
            this.lblConsole.AutoSize = true;
            this.lblConsole.Location = new System.Drawing.Point(4, 4);
            this.lblConsole.Name = "lblConsole";
            this.lblConsole.Text = "Test console — Invoke executes against the connected environment.";
            //
            // lblTarget
            //
            this.lblTarget.AutoSize = true;
            this.lblTarget.Location = new System.Drawing.Point(4, 30);
            this.lblTarget.Name = "lblTarget";
            this.lblTarget.Text = "Target (entity:guid):";
            //
            // txtTarget
            //
            this.txtTarget.Location = new System.Drawing.Point(130, 27);
            this.txtTarget.Name = "txtTarget";
            this.txtTarget.Size = new System.Drawing.Size(220, 20);
            this.txtTarget.TabIndex = 1;
            //
            // btnInvoke
            //
            this.btnInvoke.Location = new System.Drawing.Point(360, 25);
            this.btnInvoke.Name = "btnInvoke";
            this.btnInvoke.Size = new System.Drawing.Size(200, 24);
            this.btnInvoke.Text = "Invoke… (executes against org)";
            this.btnInvoke.Enabled = false;
            this.btnInvoke.UseVisualStyleBackColor = true;
            this.btnInvoke.Click += new System.EventHandler(this.btnInvoke_Click);
            //
            // CustomApiExplorerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.split);
            this.Controls.Add(this.toolStrip);
            this.Name = "CustomApiExplorerControl";
            this.Size = new System.Drawing.Size(950, 600);
            this.Load += new System.EventHandler(this.CustomApiExplorerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdApis)).EndInit();
            this.split2.Panel1.ResumeLayout(false);
            this.split2.Panel1.PerformLayout();
            this.split2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split2)).EndInit();
            this.split2.ResumeLayout(false);
            this.pnlInvoke.ResumeLayout(false);
            this.pnlInvoke.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdParams)).EndInit();
            this.pnlInvokeTop.ResumeLayout(false);
            this.pnlInvokeTop.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoad;
        private System.Windows.Forms.ToolStripLabel tslSearch;
        private System.Windows.Forms.ToolStripTextBox tstSearch;
        private System.Windows.Forms.ToolStripSeparator tssSep1;
        private System.Windows.Forms.ToolStripDropDownButton tsddExport;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportHtml;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportMarkdown;
        private System.Windows.Forms.ToolStripMenuItem tsmiExportCsv;
        private System.Windows.Forms.ToolStripSeparator tssSep2;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.DataGridView grdApis;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiKind;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiBinding;
        private System.Windows.Forms.DataGridViewTextBoxColumn colApiPlugin;
        private System.Windows.Forms.SplitContainer split2;
        private System.Windows.Forms.TextBox txtDetail;
        private System.Windows.Forms.Panel pnlInvoke;
        private System.Windows.Forms.DataGridView grdParams;
        private System.Windows.Forms.DataGridViewTextBoxColumn colParam;
        private System.Windows.Forms.DataGridViewTextBoxColumn colParamType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colParamOptional;
        private System.Windows.Forms.DataGridViewTextBoxColumn colParamValue;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.Panel pnlInvokeTop;
        private System.Windows.Forms.Label lblConsole;
        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.TextBox txtTarget;
        private System.Windows.Forms.Button btnInvoke;
    }
}
