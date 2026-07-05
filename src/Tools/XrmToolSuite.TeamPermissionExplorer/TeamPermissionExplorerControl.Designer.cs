namespace XrmToolSuite.TeamPermissionExplorer
{
    partial class TeamPermissionExplorerControl
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
            this.tslType = new System.Windows.Forms.ToolStripLabel();
            this.tscbTeamType = new System.Windows.Forms.ToolStripComboBox();
            this.tslSearch = new System.Windows.Forms.ToolStripLabel();
            this.tstSearch = new System.Windows.Forms.ToolStripTextBox();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbCompare = new System.Windows.Forms.ToolStripButton();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.miExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportCsv = new System.Windows.Forms.ToolStripMenuItem();
            this.miExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tssSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();

            this.scMain = new System.Windows.Forms.SplitContainer();
            this.grdTeams = new System.Windows.Forms.DataGridView();
            this.colTeamName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeamType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeamBu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeamMembers = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeamRoles = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTeamRisk = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.pnlRight = new System.Windows.Forms.Panel();
            this.tabDetails = new System.Windows.Forms.TabControl();
            this.tabMembers = new System.Windows.Forms.TabPage();
            this.grdMembers = new System.Windows.Forms.DataGridView();
            this.colMemberName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabRoles = new System.Windows.Forms.TabPage();
            this.grdRoles = new System.Windows.Forms.DataGridView();
            this.colRoleName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabEffective = new System.Windows.Forms.TabPage();
            this.grdEffective = new System.Windows.Forms.DataGridView();
            this.colPrivilege = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPrivScope = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPrivRole = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabOwned = new System.Windows.Forms.TabPage();
            this.grdOwned = new System.Windows.Forms.DataGridView();
            this.colOwnedTable = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOwnedCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabFindings = new System.Windows.Forms.TabPage();
            this.grdFindings = new System.Windows.Forms.DataGridView();
            this.colFindingSeverity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFindingTitle = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFindingDetail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblHeader = new System.Windows.Forms.Label();

            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).BeginInit();
            this.scMain.Panel1.SuspendLayout();
            this.scMain.Panel2.SuspendLayout();
            this.scMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdTeams)).BeginInit();
            this.pnlRight.SuspendLayout();
            this.tabDetails.SuspendLayout();
            this.tabMembers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdMembers)).BeginInit();
            this.tabRoles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdRoles)).BeginInit();
            this.tabEffective.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdEffective)).BeginInit();
            this.tabOwned.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdOwned)).BeginInit();
            this.tabFindings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoad,
                this.tssSeparator1,
                this.tslType,
                this.tscbTeamType,
                this.tslSearch,
                this.tstSearch,
                this.tssSeparator2,
                this.tsbCompare,
                this.tsbExport,
                this.tssSeparator3,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1000, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoad
            //
            this.tsbLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoad.Name = "tsbLoad";
            this.tsbLoad.Text = "Load teams";
            this.tsbLoad.Click += new System.EventHandler(this.tsbLoad_Click);
            //
            // tslType
            //
            this.tslType.Name = "tslType";
            this.tslType.Text = "Type:";
            //
            // tscbTeamType
            //
            this.tscbTeamType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscbTeamType.Name = "tscbTeamType";
            this.tscbTeamType.Size = new System.Drawing.Size(150, 25);
            this.tscbTeamType.SelectedIndexChanged += new System.EventHandler(this.tscbTeamType_SelectedIndexChanged);
            //
            // tslSearch
            //
            this.tslSearch.Name = "tslSearch";
            this.tslSearch.Text = "Search:";
            //
            // tstSearch
            //
            this.tstSearch.Name = "tstSearch";
            this.tstSearch.Size = new System.Drawing.Size(160, 25);
            this.tstSearch.TextChanged += new System.EventHandler(this.tstSearch_TextChanged);
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
                this.miExportHtml});
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export";
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
            // miExportCsv
            //
            this.miExportCsv.Name = "miExportCsv";
            this.miExportCsv.Text = "CSV (.csv)";
            this.miExportCsv.Click += new System.EventHandler(this.miExportCsv_Click);
            //
            // miExportHtml
            //
            this.miExportHtml.Name = "miExportHtml";
            this.miExportHtml.Text = "HTML (.html)";
            this.miExportHtml.Click += new System.EventHandler(this.miExportHtml_Click);
            //
            // tsbClose
            //
            this.tsbClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Text = "Close";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            //
            // scMain
            //
            this.scMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scMain.Location = new System.Drawing.Point(0, 25);
            this.scMain.Name = "scMain";
            this.scMain.Panel1.Controls.Add(this.grdTeams);
            this.scMain.Panel2.Controls.Add(this.pnlRight);
            this.scMain.Size = new System.Drawing.Size(1000, 575);
            this.scMain.SplitterDistance = 420;
            this.scMain.TabIndex = 1;
            //
            // grdTeams
            //
            this.grdTeams.AllowUserToAddRows = false;
            this.grdTeams.AllowUserToDeleteRows = false;
            this.grdTeams.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdTeams.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colTeamName,
                this.colTeamType,
                this.colTeamBu,
                this.colTeamMembers,
                this.colTeamRoles,
                this.colTeamRisk});
            this.grdTeams.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdTeams.MultiSelect = false;
            this.grdTeams.Name = "grdTeams";
            this.grdTeams.ReadOnly = true;
            this.grdTeams.RowHeadersVisible = false;
            this.grdTeams.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdTeams.SelectionChanged += new System.EventHandler(this.grdTeams_SelectionChanged);
            //
            this.colTeamName.HeaderText = "Team";
            this.colTeamName.Name = "colTeamName";
            this.colTeamName.FillWeight = 30;
            this.colTeamType.HeaderText = "Type";
            this.colTeamType.Name = "colTeamType";
            this.colTeamType.FillWeight = 18;
            this.colTeamBu.HeaderText = "Business Unit";
            this.colTeamBu.Name = "colTeamBu";
            this.colTeamBu.FillWeight = 22;
            this.colTeamMembers.HeaderText = "Members";
            this.colTeamMembers.Name = "colTeamMembers";
            this.colTeamMembers.FillWeight = 10;
            this.colTeamRoles.HeaderText = "Roles";
            this.colTeamRoles.Name = "colTeamRoles";
            this.colTeamRoles.FillWeight = 8;
            this.colTeamRisk.HeaderText = "Top risk";
            this.colTeamRisk.Name = "colTeamRisk";
            this.colTeamRisk.FillWeight = 15;
            //
            // pnlRight
            //
            this.pnlRight.Controls.Add(this.tabDetails);
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
            this.lblHeader.Text = "Select a team to see members, roles, effective privileges, and risks.";
            this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tabDetails
            //
            this.tabDetails.Controls.Add(this.tabMembers);
            this.tabDetails.Controls.Add(this.tabRoles);
            this.tabDetails.Controls.Add(this.tabEffective);
            this.tabDetails.Controls.Add(this.tabOwned);
            this.tabDetails.Controls.Add(this.tabFindings);
            this.tabDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabDetails.Name = "tabDetails";
            //
            // tabMembers
            //
            this.tabMembers.Controls.Add(this.grdMembers);
            this.tabMembers.Name = "tabMembers";
            this.tabMembers.Padding = new System.Windows.Forms.Padding(3);
            this.tabMembers.Text = "Members / Inheriting users";
            this.tabMembers.UseVisualStyleBackColor = true;
            //
            this.grdMembers.AllowUserToAddRows = false;
            this.grdMembers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdMembers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.colMemberName });
            this.grdMembers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdMembers.Name = "grdMembers";
            this.grdMembers.ReadOnly = true;
            this.grdMembers.RowHeadersVisible = false;
            this.colMemberName.HeaderText = "User (inherits this team's privileges)";
            this.colMemberName.Name = "colMemberName";
            //
            // tabRoles
            //
            this.tabRoles.Controls.Add(this.grdRoles);
            this.tabRoles.Name = "tabRoles";
            this.tabRoles.Padding = new System.Windows.Forms.Padding(3);
            this.tabRoles.Text = "Roles";
            this.tabRoles.UseVisualStyleBackColor = true;
            //
            this.grdRoles.AllowUserToAddRows = false;
            this.grdRoles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdRoles.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.colRoleName });
            this.grdRoles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdRoles.Name = "grdRoles";
            this.grdRoles.ReadOnly = true;
            this.grdRoles.RowHeadersVisible = false;
            this.colRoleName.HeaderText = "Assigned security role";
            this.colRoleName.Name = "colRoleName";
            //
            // tabEffective
            //
            this.tabEffective.Controls.Add(this.grdEffective);
            this.tabEffective.Name = "tabEffective";
            this.tabEffective.Padding = new System.Windows.Forms.Padding(3);
            this.tabEffective.Text = "Effective privileges";
            this.tabEffective.UseVisualStyleBackColor = true;
            //
            this.grdEffective.AllowUserToAddRows = false;
            this.grdEffective.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdEffective.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colPrivilege, this.colPrivScope, this.colPrivRole });
            this.grdEffective.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdEffective.Name = "grdEffective";
            this.grdEffective.ReadOnly = true;
            this.grdEffective.RowHeadersVisible = false;
            this.colPrivilege.HeaderText = "Privilege";
            this.colPrivilege.Name = "colPrivilege";
            this.colPrivilege.FillWeight = 50;
            this.colPrivScope.HeaderText = "Scope";
            this.colPrivScope.Name = "colPrivScope";
            this.colPrivScope.FillWeight = 25;
            this.colPrivRole.HeaderText = "Deepest via role";
            this.colPrivRole.Name = "colPrivRole";
            this.colPrivRole.FillWeight = 25;
            //
            // tabOwned
            //
            this.tabOwned.Controls.Add(this.grdOwned);
            this.tabOwned.Name = "tabOwned";
            this.tabOwned.Padding = new System.Windows.Forms.Padding(3);
            this.tabOwned.Text = "Owned records";
            this.tabOwned.UseVisualStyleBackColor = true;
            //
            this.grdOwned.AllowUserToAddRows = false;
            this.grdOwned.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdOwned.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colOwnedTable, this.colOwnedCount });
            this.grdOwned.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdOwned.Name = "grdOwned";
            this.grdOwned.ReadOnly = true;
            this.grdOwned.RowHeadersVisible = false;
            this.colOwnedTable.HeaderText = "Table";
            this.colOwnedTable.Name = "colOwnedTable";
            this.colOwnedTable.FillWeight = 70;
            this.colOwnedCount.HeaderText = "Owned records";
            this.colOwnedCount.Name = "colOwnedCount";
            this.colOwnedCount.FillWeight = 30;
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
            // TeamPermissionExplorerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.scMain);
            this.Controls.Add(this.toolStrip);
            this.Name = "TeamPermissionExplorerControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.Load += new System.EventHandler(this.TeamPermissionExplorerControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.scMain.Panel1.ResumeLayout(false);
            this.scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scMain)).EndInit();
            this.scMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdTeams)).EndInit();
            this.pnlRight.ResumeLayout(false);
            this.tabDetails.ResumeLayout(false);
            this.tabMembers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdMembers)).EndInit();
            this.tabRoles.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdRoles)).EndInit();
            this.tabEffective.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdEffective)).EndInit();
            this.tabOwned.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdOwned)).EndInit();
            this.tabFindings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoad;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripLabel tslType;
        private System.Windows.Forms.ToolStripComboBox tscbTeamType;
        private System.Windows.Forms.ToolStripLabel tslSearch;
        private System.Windows.Forms.ToolStripTextBox tstSearch;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripButton tsbCompare;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem miExportExcel;
        private System.Windows.Forms.ToolStripMenuItem miExportPdf;
        private System.Windows.Forms.ToolStripMenuItem miExportCsv;
        private System.Windows.Forms.ToolStripMenuItem miExportHtml;
        private System.Windows.Forms.ToolStripSeparator tssSeparator3;
        private System.Windows.Forms.ToolStripButton tsbClose;

        private System.Windows.Forms.SplitContainer scMain;
        private System.Windows.Forms.DataGridView grdTeams;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeamName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeamType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeamBu;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeamMembers;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeamRoles;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTeamRisk;

        private System.Windows.Forms.Panel pnlRight;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.TabControl tabDetails;
        private System.Windows.Forms.TabPage tabMembers;
        private System.Windows.Forms.DataGridView grdMembers;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMemberName;
        private System.Windows.Forms.TabPage tabRoles;
        private System.Windows.Forms.DataGridView grdRoles;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRoleName;
        private System.Windows.Forms.TabPage tabEffective;
        private System.Windows.Forms.DataGridView grdEffective;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPrivilege;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPrivScope;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPrivRole;
        private System.Windows.Forms.TabPage tabOwned;
        private System.Windows.Forms.DataGridView grdOwned;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOwnedTable;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOwnedCount;
        private System.Windows.Forms.TabPage tabFindings;
        private System.Windows.Forms.DataGridView grdFindings;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFindingSeverity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFindingTitle;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFindingDetail;
    }
}
