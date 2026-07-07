namespace XrmToolSuite.PluginDependencyGraph
{
    partial class PluginDependencyGraphControl
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
            this.tss2 = new System.Windows.Forms.ToolStripSeparator();
            this.tslTable = new System.Windows.Forms.ToolStripLabel();
            this.cboTable = new System.Windows.Forms.ToolStripComboBox();
            this.tslMessage = new System.Windows.Forms.ToolStripLabel();
            this.cboMessage = new System.Windows.Forms.ToolStripComboBox();
            this.tslStage = new System.Windows.Forms.ToolStripLabel();
            this.cboStage = new System.Windows.Forms.ToolStripComboBox();
            this.tslMode = new System.Windows.Forms.ToolStripLabel();
            this.cboMode = new System.Windows.Forms.ToolStripComboBox();
            this.tslSolution = new System.Windows.Forms.ToolStripLabel();
            this.cboSolution = new System.Windows.Forms.ToolStripComboBox();
            this.tss3 = new System.Windows.Forms.ToolStripSeparator();
            this.tslFocus = new System.Windows.Forms.ToolStripLabel();
            this.cboFocus = new System.Windows.Forms.ToolStripComboBox();
            this.tss4 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.splitLeft = new System.Windows.Forms.SplitContainer();
            this.lvNodes = new System.Windows.Forms.ListView();
            this.colNodeType = new System.Windows.Forms.ColumnHeader();
            this.colNodeLabel = new System.Windows.Forms.ColumnHeader();
            this.lblNodes = new System.Windows.Forms.Label();
            this.lvDetails = new System.Windows.Forms.ListView();
            this.colDetProp = new System.Windows.Forms.ColumnHeader();
            this.colDetVal = new System.Windows.Forms.ColumnHeader();
            this.lblDetails = new System.Windows.Forms.Label();
            this.splitRight = new System.Windows.Forms.SplitContainer();
            this.txtPreview = new System.Windows.Forms.TextBox();
            this.splitRB = new System.Windows.Forms.SplitContainer();
            this.lvDependencies = new System.Windows.Forms.ListView();
            this.colDepDir = new System.Windows.Forms.ColumnHeader();
            this.colDepKind = new System.Windows.Forms.ColumnHeader();
            this.colDepNode = new System.Windows.Forms.ColumnHeader();
            this.colDepType = new System.Windows.Forms.ColumnHeader();
            this.lblDeps = new System.Windows.Forms.Label();
            this.lvFindings = new System.Windows.Forms.ListView();
            this.colFindSev = new System.Windows.Forms.ColumnHeader();
            this.colFindTitle = new System.Windows.Forms.ColumnHeader();
            this.colFindComp = new System.Windows.Forms.ColumnHeader();
            this.lblFindings = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitLeft)).BeginInit();
            this.splitLeft.Panel1.SuspendLayout();
            this.splitLeft.Panel2.SuspendLayout();
            this.splitLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).BeginInit();
            this.splitRight.Panel1.SuspendLayout();
            this.splitRight.Panel2.SuspendLayout();
            this.splitRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRB)).BeginInit();
            this.splitRB.Panel1.SuspendLayout();
            this.splitRB.Panel2.SuspendLayout();
            this.splitRB.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoad,
                this.tss2,
                this.tslTable,
                this.cboTable,
                this.tslMessage,
                this.cboMessage,
                this.tslStage,
                this.cboStage,
                this.tslMode,
                this.cboMode,
                this.tslSolution,
                this.cboSolution,
                this.tss3,
                this.tslFocus,
                this.cboFocus,
                this.tss4,
                this.tsbExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1000, 27);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoad
            //
            this.tsbLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoad.Name = "tsbLoad";
            this.tsbLoad.Text = "▶ Load pipeline";
            //
            // tslTable
            //
            this.tslTable.Name = "tslTable";
            this.tslTable.Text = "Table:";
            //
            // cboTable
            //
            this.cboTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTable.Name = "cboTable";
            this.cboTable.Size = new System.Drawing.Size(120, 27);
            //
            // tslMessage
            //
            this.tslMessage.Name = "tslMessage";
            this.tslMessage.Text = "Message:";
            //
            // cboMessage
            //
            this.cboMessage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMessage.Name = "cboMessage";
            this.cboMessage.Size = new System.Drawing.Size(110, 27);
            //
            // tslStage
            //
            this.tslStage.Name = "tslStage";
            this.tslStage.Text = "Stage:";
            //
            // cboStage
            //
            this.cboStage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStage.Name = "cboStage";
            this.cboStage.Size = new System.Drawing.Size(110, 27);
            //
            // tslMode
            //
            this.tslMode.Name = "tslMode";
            this.tslMode.Text = "Mode:";
            //
            // cboMode
            //
            this.cboMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMode.Name = "cboMode";
            this.cboMode.Size = new System.Drawing.Size(110, 27);
            //
            // tslSolution
            //
            this.tslSolution.Name = "tslSolution";
            this.tslSolution.Text = "Solution:";
            //
            // cboSolution
            //
            this.cboSolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSolution.Name = "cboSolution";
            this.cboSolution.Size = new System.Drawing.Size(130, 27);
            //
            // tslFocus
            //
            this.tslFocus.Name = "tslFocus";
            this.tslFocus.Text = "Focus:";
            //
            // cboFocus
            //
            this.cboFocus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFocus.Name = "cboFocus";
            this.cboFocus.Size = new System.Drawing.Size(160, 27);
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export ▼";
            this.tsbExport.Enabled = false;
            //
            // splitMain
            //
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 27);
            this.splitMain.Name = "splitMain";
            this.splitMain.Panel1.Controls.Add(this.splitLeft);
            this.splitMain.Panel2.Controls.Add(this.splitRight);
            this.splitMain.Size = new System.Drawing.Size(1000, 551);
            this.splitMain.SplitterDistance = 320;
            this.splitMain.TabIndex = 1;
            //
            // splitLeft
            //
            this.splitLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitLeft.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitLeft.Name = "splitLeft";
            this.splitLeft.Panel1.Controls.Add(this.lvNodes);
            this.splitLeft.Panel1.Controls.Add(this.lblNodes);
            this.splitLeft.Panel2.Controls.Add(this.lvDetails);
            this.splitLeft.Panel2.Controls.Add(this.lblDetails);
            this.splitLeft.Size = new System.Drawing.Size(320, 551);
            this.splitLeft.SplitterDistance = 300;
            this.splitLeft.TabIndex = 0;
            //
            // lvNodes
            //
            this.lvNodes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.colNodeType, this.colNodeLabel });
            this.lvNodes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvNodes.FullRowSelect = true;
            this.lvNodes.HideSelection = false;
            this.lvNodes.MultiSelect = false;
            this.lvNodes.Name = "lvNodes";
            this.lvNodes.Size = new System.Drawing.Size(320, 278);
            this.lvNodes.TabIndex = 1;
            this.lvNodes.UseCompatibleStateImageBehavior = false;
            this.lvNodes.View = System.Windows.Forms.View.Details;
            this.colNodeType.Text = "Type";
            this.colNodeType.Width = 90;
            this.colNodeLabel.Text = "Node";
            this.colNodeLabel.Width = 210;
            //
            // lblNodes
            //
            this.lblNodes.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblNodes.Name = "lblNodes";
            this.lblNodes.Height = 22;
            this.lblNodes.Padding = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.lblNodes.Text = "Nodes";
            //
            // lvDetails
            //
            this.lvDetails.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.colDetProp, this.colDetVal });
            this.lvDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvDetails.FullRowSelect = true;
            this.lvDetails.Name = "lvDetails";
            this.lvDetails.Size = new System.Drawing.Size(320, 225);
            this.lvDetails.TabIndex = 1;
            this.lvDetails.UseCompatibleStateImageBehavior = false;
            this.lvDetails.View = System.Windows.Forms.View.Details;
            this.colDetProp.Text = "Property";
            this.colDetProp.Width = 120;
            this.colDetVal.Text = "Value";
            this.colDetVal.Width = 190;
            //
            // lblDetails
            //
            this.lblDetails.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDetails.Name = "lblDetails";
            this.lblDetails.Height = 22;
            this.lblDetails.Padding = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.lblDetails.Text = "Node details";
            //
            // splitRight
            //
            this.splitRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitRight.Name = "splitRight";
            this.splitRight.Panel1.Controls.Add(this.txtPreview);
            this.splitRight.Panel2.Controls.Add(this.splitRB);
            this.splitRight.Size = new System.Drawing.Size(676, 551);
            this.splitRight.SplitterDistance = 300;
            this.splitRight.TabIndex = 0;
            //
            // txtPreview
            //
            this.txtPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPreview.Multiline = true;
            this.txtPreview.Name = "txtPreview";
            this.txtPreview.ReadOnly = true;
            this.txtPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtPreview.WordWrap = false;
            this.txtPreview.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtPreview.Text = "Click \"Load pipeline\" to build the plugin dependency graph.";
            //
            // splitRB
            //
            this.splitRB.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitRB.Name = "splitRB";
            this.splitRB.Panel1.Controls.Add(this.lvDependencies);
            this.splitRB.Panel1.Controls.Add(this.lblDeps);
            this.splitRB.Panel2.Controls.Add(this.lvFindings);
            this.splitRB.Panel2.Controls.Add(this.lblFindings);
            this.splitRB.Size = new System.Drawing.Size(676, 247);
            this.splitRB.SplitterDistance = 338;
            this.splitRB.TabIndex = 0;
            //
            // lvDependencies
            //
            this.lvDependencies.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.colDepDir, this.colDepKind, this.colDepNode, this.colDepType });
            this.lvDependencies.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvDependencies.FullRowSelect = true;
            this.lvDependencies.Name = "lvDependencies";
            this.lvDependencies.UseCompatibleStateImageBehavior = false;
            this.lvDependencies.View = System.Windows.Forms.View.Details;
            this.colDepDir.Text = "Dir";
            this.colDepDir.Width = 60;
            this.colDepKind.Text = "Kind";
            this.colDepKind.Width = 90;
            this.colDepNode.Text = "Connected node";
            this.colDepNode.Width = 130;
            this.colDepType.Text = "Type";
            this.colDepType.Width = 70;
            //
            // lblDeps
            //
            this.lblDeps.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDeps.Name = "lblDeps";
            this.lblDeps.Height = 22;
            this.lblDeps.Padding = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.lblDeps.Text = "Dependencies";
            //
            // lvFindings
            //
            this.lvFindings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.colFindSev, this.colFindTitle, this.colFindComp });
            this.lvFindings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvFindings.FullRowSelect = true;
            this.lvFindings.Name = "lvFindings";
            this.lvFindings.UseCompatibleStateImageBehavior = false;
            this.lvFindings.View = System.Windows.Forms.View.Details;
            this.colFindSev.Text = "Severity";
            this.colFindSev.Width = 70;
            this.colFindTitle.Text = "Finding";
            this.colFindTitle.Width = 180;
            this.colFindComp.Text = "Component";
            this.colFindComp.Width = 120;
            //
            // lblFindings
            //
            this.lblFindings.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFindings.Name = "lblFindings";
            this.lblFindings.Height = 22;
            this.lblFindings.Padding = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.lblFindings.Text = "Risk findings";
            //
            // lblStatus
            //
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Height = 22;
            this.lblStatus.Padding = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.lblStatus.Text = "Ready.";
            //
            // PluginDependencyGraphControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.toolStrip);
            this.Name = "PluginDependencyGraphControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.Load += new System.EventHandler(this.PluginDependencyGraphControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.splitLeft.Panel1.ResumeLayout(false);
            this.splitLeft.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitLeft)).EndInit();
            this.splitLeft.ResumeLayout(false);
            this.splitRight.Panel1.ResumeLayout(false);
            this.splitRight.Panel1.PerformLayout();
            this.splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).EndInit();
            this.splitRight.ResumeLayout(false);
            this.splitRB.Panel1.ResumeLayout(false);
            this.splitRB.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRB)).EndInit();
            this.splitRB.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoad;
        private System.Windows.Forms.ToolStripSeparator tss2;
        private System.Windows.Forms.ToolStripLabel tslTable;
        private System.Windows.Forms.ToolStripComboBox cboTable;
        private System.Windows.Forms.ToolStripLabel tslMessage;
        private System.Windows.Forms.ToolStripComboBox cboMessage;
        private System.Windows.Forms.ToolStripLabel tslStage;
        private System.Windows.Forms.ToolStripComboBox cboStage;
        private System.Windows.Forms.ToolStripLabel tslMode;
        private System.Windows.Forms.ToolStripComboBox cboMode;
        private System.Windows.Forms.ToolStripLabel tslSolution;
        private System.Windows.Forms.ToolStripComboBox cboSolution;
        private System.Windows.Forms.ToolStripSeparator tss3;
        private System.Windows.Forms.ToolStripLabel tslFocus;
        private System.Windows.Forms.ToolStripComboBox cboFocus;
        private System.Windows.Forms.ToolStripSeparator tss4;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.SplitContainer splitLeft;
        private System.Windows.Forms.ListView lvNodes;
        private System.Windows.Forms.ColumnHeader colNodeType;
        private System.Windows.Forms.ColumnHeader colNodeLabel;
        private System.Windows.Forms.Label lblNodes;
        private System.Windows.Forms.ListView lvDetails;
        private System.Windows.Forms.ColumnHeader colDetProp;
        private System.Windows.Forms.ColumnHeader colDetVal;
        private System.Windows.Forms.Label lblDetails;
        private System.Windows.Forms.SplitContainer splitRight;
        private System.Windows.Forms.TextBox txtPreview;
        private System.Windows.Forms.SplitContainer splitRB;
        private System.Windows.Forms.ListView lvDependencies;
        private System.Windows.Forms.ColumnHeader colDepDir;
        private System.Windows.Forms.ColumnHeader colDepKind;
        private System.Windows.Forms.ColumnHeader colDepNode;
        private System.Windows.Forms.ColumnHeader colDepType;
        private System.Windows.Forms.Label lblDeps;
        private System.Windows.Forms.ListView lvFindings;
        private System.Windows.Forms.ColumnHeader colFindSev;
        private System.Windows.Forms.ColumnHeader colFindTitle;
        private System.Windows.Forms.ColumnHeader colFindComp;
        private System.Windows.Forms.Label lblFindings;
        private System.Windows.Forms.Label lblStatus;
    }
}
