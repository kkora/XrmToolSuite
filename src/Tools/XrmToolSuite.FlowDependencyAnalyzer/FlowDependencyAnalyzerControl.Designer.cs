namespace XrmToolSuite.FlowDependencyAnalyzer
{
    partial class FlowDependencyAnalyzerControl
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
            this.tsbAnalyze = new System.Windows.Forms.ToolStripButton();
            this.tssSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsmExportExcel = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportPdf = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportJson = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmExportHtml = new System.Windows.Forms.ToolStripMenuItem();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();

            this.filterStrip = new System.Windows.Forms.ToolStrip();
            this.tslStatus = new System.Windows.Forms.ToolStripLabel();
            this.cboStatus = new System.Windows.Forms.ToolStripComboBox();
            this.tslOwner = new System.Windows.Forms.ToolStripLabel();
            this.cboOwner = new System.Windows.Forms.ToolStripComboBox();
            this.tslConnector = new System.Windows.Forms.ToolStripLabel();
            this.cboConnector = new System.Windows.Forms.ToolStripComboBox();
            this.tslTrigger = new System.Windows.Forms.ToolStripLabel();
            this.cboTrigger = new System.Windows.Forms.ToolStripComboBox();
            this.tslTable = new System.Windows.Forms.ToolStripLabel();
            this.cboTable = new System.Windows.Forms.ToolStripComboBox();
            this.tslSolution = new System.Windows.Forms.ToolStripLabel();
            this.cboSolution = new System.Windows.Forms.ToolStripComboBox();

            this.tabs = new System.Windows.Forms.TabControl();
            this.tabFlows = new System.Windows.Forms.TabPage();
            this.splitFlows = new System.Windows.Forms.SplitContainer();
            this.grdFlows = new System.Windows.Forms.DataGridView();
            this.panelDetail = new System.Windows.Forms.Panel();
            this.tvDependencies = new System.Windows.Forms.TreeView();
            this.lblDetail = new System.Windows.Forms.Label();

            this.tabImpact = new System.Windows.Forms.TabPage();
            this.tlpImpact = new System.Windows.Forms.TableLayoutPanel();
            this.lblImpactIntro = new System.Windows.Forms.Label();
            this.pnlImpactPickers = new System.Windows.Forms.Panel();
            this.lblImpactKind = new System.Windows.Forms.Label();
            this.cboImpactKind = new System.Windows.Forms.ComboBox();
            this.lblImpactComponent = new System.Windows.Forms.Label();
            this.cboImpactComponent = new System.Windows.Forms.ComboBox();
            this.lblImpactedHeader = new System.Windows.Forms.Label();
            this.lstImpactedFlows = new System.Windows.Forms.ListBox();

            this.tabFindings = new System.Windows.Forms.TabPage();
            this.grdFindings = new System.Windows.Forms.DataGridView();

            this.tabReadiness = new System.Windows.Forms.TabPage();
            this.lblReadiness = new System.Windows.Forms.Label();
            this.lvReadiness = new System.Windows.Forms.ListView();
            this.colCheck = new System.Windows.Forms.ColumnHeader();
            this.colCheckDetail = new System.Windows.Forms.ColumnHeader();

            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();

            this.toolStrip.SuspendLayout();
            this.filterStrip.SuspendLayout();
            this.tabs.SuspendLayout();
            this.tabFlows.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitFlows)).BeginInit();
            this.splitFlows.Panel1.SuspendLayout();
            this.splitFlows.Panel2.SuspendLayout();
            this.splitFlows.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFlows)).BeginInit();
            this.panelDetail.SuspendLayout();
            this.tabImpact.SuspendLayout();
            this.tlpImpact.SuspendLayout();
            this.pnlImpactPickers.SuspendLayout();
            this.tabFindings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).BeginInit();
            this.tabReadiness.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbAnalyze,
                this.tssSep1,
                this.tsbExport,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1000, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbAnalyze
            //
            this.tsbAnalyze.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbAnalyze.Name = "tsbAnalyze";
            this.tsbAnalyze.Text = "Analyze flows";
            this.tsbAnalyze.ToolTipText = "Retrieve every cloud flow and map its dependencies (read-only)";
            this.tsbAnalyze.Click += new System.EventHandler(this.tsbAnalyze_Click);
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
            this.tsmExportExcel.Text = "Excel (.xlsx)";
            this.tsmExportExcel.Click += new System.EventHandler(this.tsmExportExcel_Click);
            this.tsmExportPdf.Text = "PDF (.pdf)";
            this.tsmExportPdf.Click += new System.EventHandler(this.tsmExportPdf_Click);
            this.tsmExportJson.Text = "JSON (.json)";
            this.tsmExportJson.Click += new System.EventHandler(this.tsmExportJson_Click);
            this.tsmExportHtml.Text = "HTML (.html)";
            this.tsmExportHtml.Click += new System.EventHandler(this.tsmExportHtml_Click);
            //
            // tsbClose
            //
            this.tsbClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Text = "Close";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            //
            // filterStrip
            //
            this.filterStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tslStatus, this.cboStatus,
                this.tslOwner, this.cboOwner,
                this.tslConnector, this.cboConnector,
                this.tslTrigger, this.cboTrigger,
                this.tslTable, this.cboTable,
                this.tslSolution, this.cboSolution});
            this.filterStrip.Location = new System.Drawing.Point(0, 25);
            this.filterStrip.Name = "filterStrip";
            this.filterStrip.Size = new System.Drawing.Size(1000, 25);
            this.filterStrip.TabIndex = 1;
            this.tslStatus.Text = "Status:";
            this.tslOwner.Text = "Owner:";
            this.tslConnector.Text = "Connector:";
            this.tslTrigger.Text = "Trigger:";
            this.tslTable.Text = "Table:";
            this.tslSolution.Text = "Solution:";
            this.cboStatus.Name = "cboStatus";
            this.cboStatus.AutoSize = false;
            this.cboStatus.Width = 90;
            this.cboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStatus.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);
            this.cboOwner.Name = "cboOwner";
            this.cboOwner.AutoSize = false;
            this.cboOwner.Width = 130;
            this.cboOwner.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboOwner.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);
            this.cboConnector.Name = "cboConnector";
            this.cboConnector.AutoSize = false;
            this.cboConnector.Width = 150;
            this.cboConnector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboConnector.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);
            this.cboTrigger.Name = "cboTrigger";
            this.cboTrigger.AutoSize = false;
            this.cboTrigger.Width = 120;
            this.cboTrigger.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTrigger.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);
            this.cboTable.Name = "cboTable";
            this.cboTable.AutoSize = false;
            this.cboTable.Width = 130;
            this.cboTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboTable.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);
            this.cboSolution.Name = "cboSolution";
            this.cboSolution.AutoSize = false;
            this.cboSolution.Width = 150;
            this.cboSolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSolution.SelectedIndexChanged += new System.EventHandler(this.Filter_Changed);
            //
            // tabs
            //
            this.tabs.Controls.Add(this.tabFlows);
            this.tabs.Controls.Add(this.tabImpact);
            this.tabs.Controls.Add(this.tabFindings);
            this.tabs.Controls.Add(this.tabReadiness);
            this.tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabs.Location = new System.Drawing.Point(0, 50);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new System.Drawing.Size(1000, 528);
            this.tabs.TabIndex = 2;
            //
            // tabFlows
            //
            this.tabFlows.Controls.Add(this.splitFlows);
            this.tabFlows.Name = "tabFlows";
            this.tabFlows.Text = "Flows";
            this.tabFlows.Padding = new System.Windows.Forms.Padding(3);
            this.tabFlows.UseVisualStyleBackColor = true;
            //
            // splitFlows
            //
            this.splitFlows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitFlows.Name = "splitFlows";
            this.splitFlows.Panel1.Controls.Add(this.grdFlows);
            this.splitFlows.Panel2.Controls.Add(this.panelDetail);
            this.splitFlows.Size = new System.Drawing.Size(986, 516);
            this.splitFlows.SplitterDistance = 560;
            this.splitFlows.TabIndex = 0;
            //
            // grdFlows
            //
            this.grdFlows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdFlows.Name = "grdFlows";
            this.grdFlows.AllowUserToAddRows = false;
            this.grdFlows.AllowUserToDeleteRows = false;
            this.grdFlows.ReadOnly = true;
            this.grdFlows.RowHeadersVisible = false;
            this.grdFlows.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdFlows.MultiSelect = false;
            this.grdFlows.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdFlows.TabIndex = 0;
            this.grdFlows.SelectionChanged += new System.EventHandler(this.grdFlows_SelectionChanged);
            //
            // panelDetail
            //
            this.panelDetail.Controls.Add(this.tvDependencies);
            this.panelDetail.Controls.Add(this.lblDetail);
            this.panelDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDetail.Name = "panelDetail";
            //
            // lblDetail
            //
            this.lblDetail.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDetail.Name = "lblDetail";
            this.lblDetail.Height = 22;
            this.lblDetail.Text = "Select a flow to see its dependency tree";
            this.lblDetail.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDetail.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // tvDependencies
            //
            this.tvDependencies.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvDependencies.Name = "tvDependencies";
            this.tvDependencies.HideSelection = false;
            this.tvDependencies.TabIndex = 0;
            //
            // tabImpact
            //
            this.tabImpact.Controls.Add(this.tlpImpact);
            this.tabImpact.Name = "tabImpact";
            this.tabImpact.Text = "Component impact";
            this.tabImpact.Padding = new System.Windows.Forms.Padding(8);
            this.tabImpact.UseVisualStyleBackColor = true;
            //
            // tlpImpact
            //
            this.tlpImpact.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpImpact.ColumnCount = 1;
            this.tlpImpact.RowCount = 4;
            this.tlpImpact.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpImpact.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tlpImpact.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tlpImpact.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tlpImpact.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpImpact.Controls.Add(this.lblImpactIntro, 0, 0);
            this.tlpImpact.Controls.Add(this.pnlImpactPickers, 0, 1);
            this.tlpImpact.Controls.Add(this.lblImpactedHeader, 0, 2);
            this.tlpImpact.Controls.Add(this.lstImpactedFlows, 0, 3);
            //
            // lblImpactIntro
            //
            this.lblImpactIntro.AutoSize = true;
            this.lblImpactIntro.Name = "lblImpactIntro";
            this.lblImpactIntro.Margin = new System.Windows.Forms.Padding(3, 3, 3, 8);
            this.lblImpactIntro.Text = "Pick a component to see every flow that depends on it (\"which flows break if I change this?\").";
            //
            // pnlImpactPickers
            //
            this.pnlImpactPickers.Controls.Add(this.cboImpactComponent);
            this.pnlImpactPickers.Controls.Add(this.lblImpactComponent);
            this.pnlImpactPickers.Controls.Add(this.cboImpactKind);
            this.pnlImpactPickers.Controls.Add(this.lblImpactKind);
            this.pnlImpactPickers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlImpactPickers.Height = 32;
            this.pnlImpactPickers.Name = "pnlImpactPickers";
            //
            // lblImpactKind
            //
            this.lblImpactKind.AutoSize = true;
            this.lblImpactKind.Location = new System.Drawing.Point(3, 6);
            this.lblImpactKind.Text = "Kind:";
            //
            // cboImpactKind
            //
            this.cboImpactKind.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboImpactKind.Location = new System.Drawing.Point(48, 3);
            this.cboImpactKind.Width = 170;
            this.cboImpactKind.Name = "cboImpactKind";
            this.cboImpactKind.SelectedIndexChanged += new System.EventHandler(this.cboImpactKind_SelectedIndexChanged);
            //
            // lblImpactComponent
            //
            this.lblImpactComponent.AutoSize = true;
            this.lblImpactComponent.Location = new System.Drawing.Point(232, 6);
            this.lblImpactComponent.Text = "Component:";
            //
            // cboImpactComponent
            //
            this.cboImpactComponent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboImpactComponent.Location = new System.Drawing.Point(310, 3);
            this.cboImpactComponent.Width = 320;
            this.cboImpactComponent.Name = "cboImpactComponent";
            this.cboImpactComponent.SelectedIndexChanged += new System.EventHandler(this.cboImpactComponent_SelectedIndexChanged);
            //
            // lblImpactedHeader
            //
            this.lblImpactedHeader.AutoSize = true;
            this.lblImpactedHeader.Name = "lblImpactedHeader";
            this.lblImpactedHeader.Margin = new System.Windows.Forms.Padding(3, 8, 3, 3);
            this.lblImpactedHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblImpactedHeader.Text = "Impacted flows";
            //
            // lstImpactedFlows
            //
            this.lstImpactedFlows.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstImpactedFlows.IntegralHeight = false;
            this.lstImpactedFlows.Name = "lstImpactedFlows";
            //
            // tabFindings
            //
            this.tabFindings.Controls.Add(this.grdFindings);
            this.tabFindings.Name = "tabFindings";
            this.tabFindings.Text = "Findings";
            this.tabFindings.Padding = new System.Windows.Forms.Padding(3);
            this.tabFindings.UseVisualStyleBackColor = true;
            //
            // grdFindings
            //
            this.grdFindings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdFindings.Name = "grdFindings";
            this.grdFindings.AllowUserToAddRows = false;
            this.grdFindings.AllowUserToDeleteRows = false;
            this.grdFindings.ReadOnly = true;
            this.grdFindings.RowHeadersVisible = false;
            this.grdFindings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdFindings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grdFindings.TabIndex = 0;
            //
            // tabReadiness
            //
            this.tabReadiness.Controls.Add(this.lvReadiness);
            this.tabReadiness.Controls.Add(this.lblReadiness);
            this.tabReadiness.Name = "tabReadiness";
            this.tabReadiness.Text = "Deployment readiness";
            this.tabReadiness.Padding = new System.Windows.Forms.Padding(8);
            this.tabReadiness.UseVisualStyleBackColor = true;
            //
            // lblReadiness
            //
            this.lblReadiness.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblReadiness.Height = 48;
            this.lblReadiness.Name = "lblReadiness";
            this.lblReadiness.Text = "Analyze flows to build the deployment-readiness checklist.";
            this.lblReadiness.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            //
            // lvReadiness
            //
            this.lvReadiness.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvReadiness.Name = "lvReadiness";
            this.lvReadiness.View = System.Windows.Forms.View.Details;
            this.lvReadiness.FullRowSelect = true;
            this.lvReadiness.UseCompatibleStateImageBehavior = false;
            this.lvReadiness.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colCheck, this.colCheckDetail });
            this.colCheck.Text = "Check";
            this.colCheck.Width = 320;
            this.colCheckDetail.Text = "Detail";
            this.colCheckDetail.Width = 620;
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.lblStatus });
            this.statusStrip.Location = new System.Drawing.Point(0, 578);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1000, 22);
            this.statusStrip.TabIndex = 3;
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "Ready. Click 'Analyze flows'.";
            //
            // FlowDependencyAnalyzerControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.filterStrip);
            this.Controls.Add(this.toolStrip);
            this.Name = "FlowDependencyAnalyzerControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.Load += new System.EventHandler(this.FlowDependencyAnalyzerControl_Load);

            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.filterStrip.ResumeLayout(false);
            this.filterStrip.PerformLayout();
            this.tabs.ResumeLayout(false);
            this.tabFlows.ResumeLayout(false);
            this.splitFlows.Panel1.ResumeLayout(false);
            this.splitFlows.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitFlows)).EndInit();
            this.splitFlows.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdFlows)).EndInit();
            this.panelDetail.ResumeLayout(false);
            this.tabImpact.ResumeLayout(false);
            this.tlpImpact.ResumeLayout(false);
            this.tlpImpact.PerformLayout();
            this.pnlImpactPickers.ResumeLayout(false);
            this.pnlImpactPickers.PerformLayout();
            this.tabFindings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdFindings)).EndInit();
            this.tabReadiness.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbAnalyze;
        private System.Windows.Forms.ToolStripSeparator tssSep1;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripMenuItem tsmExportExcel;
        private System.Windows.Forms.ToolStripMenuItem tsmExportPdf;
        private System.Windows.Forms.ToolStripMenuItem tsmExportJson;
        private System.Windows.Forms.ToolStripMenuItem tsmExportHtml;
        private System.Windows.Forms.ToolStripButton tsbClose;

        private System.Windows.Forms.ToolStrip filterStrip;
        private System.Windows.Forms.ToolStripLabel tslStatus;
        private System.Windows.Forms.ToolStripComboBox cboStatus;
        private System.Windows.Forms.ToolStripLabel tslOwner;
        private System.Windows.Forms.ToolStripComboBox cboOwner;
        private System.Windows.Forms.ToolStripLabel tslConnector;
        private System.Windows.Forms.ToolStripComboBox cboConnector;
        private System.Windows.Forms.ToolStripLabel tslTrigger;
        private System.Windows.Forms.ToolStripComboBox cboTrigger;
        private System.Windows.Forms.ToolStripLabel tslTable;
        private System.Windows.Forms.ToolStripComboBox cboTable;
        private System.Windows.Forms.ToolStripLabel tslSolution;
        private System.Windows.Forms.ToolStripComboBox cboSolution;

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabFlows;
        private System.Windows.Forms.SplitContainer splitFlows;
        private System.Windows.Forms.DataGridView grdFlows;
        private System.Windows.Forms.Panel panelDetail;
        private System.Windows.Forms.TreeView tvDependencies;
        private System.Windows.Forms.Label lblDetail;

        private System.Windows.Forms.TabPage tabImpact;
        private System.Windows.Forms.TableLayoutPanel tlpImpact;
        private System.Windows.Forms.Label lblImpactIntro;
        private System.Windows.Forms.Panel pnlImpactPickers;
        private System.Windows.Forms.Label lblImpactKind;
        private System.Windows.Forms.ComboBox cboImpactKind;
        private System.Windows.Forms.Label lblImpactComponent;
        private System.Windows.Forms.ComboBox cboImpactComponent;
        private System.Windows.Forms.Label lblImpactedHeader;
        private System.Windows.Forms.ListBox lstImpactedFlows;

        private System.Windows.Forms.TabPage tabFindings;
        private System.Windows.Forms.DataGridView grdFindings;

        private System.Windows.Forms.TabPage tabReadiness;
        private System.Windows.Forms.Label lblReadiness;
        private System.Windows.Forms.ListView lvReadiness;
        private System.Windows.Forms.ColumnHeader colCheck;
        private System.Windows.Forms.ColumnHeader colCheckDetail;

        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}
