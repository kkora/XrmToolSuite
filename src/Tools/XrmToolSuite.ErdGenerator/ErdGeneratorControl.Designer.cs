namespace XrmToolSuite.ErdGenerator
{
    partial class ErdGeneratorControl
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
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.tss1 = new System.Windows.Forms.ToolStripSeparator();
            this.tslScope = new System.Windows.Forms.ToolStripLabel();
            this.cboScope = new System.Windows.Forms.ToolStripComboBox();
            this.cboScopeValue = new System.Windows.Forms.ToolStripComboBox();
            this.tsbLoadTables = new System.Windows.Forms.ToolStripButton();
            this.tss2 = new System.Windows.Forms.ToolStripSeparator();
            this.tslColumns = new System.Windows.Forms.ToolStripLabel();
            this.cboColumns = new System.Windows.Forms.ToolStripComboBox();
            this.tsbCustomOnly = new System.Windows.Forms.ToolStripButton();
            this.tsbManagedOnly = new System.Windows.Forms.ToolStripButton();
            this.tss3 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbGenerate = new System.Windows.Forms.ToolStripButton();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.split = new System.Windows.Forms.SplitContainer();
            this.txtTableFilter = new System.Windows.Forms.TextBox();
            this.clbTables = new System.Windows.Forms.CheckedListBox();
            this.lblTables = new System.Windows.Forms.Label();
            this.txtPreview = new System.Windows.Forms.TextBox();
            this.lblStats = new System.Windows.Forms.Label();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).BeginInit();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbClose,
                this.tss1,
                this.tslScope,
                this.cboScope,
                this.cboScopeValue,
                this.tsbLoadTables,
                this.tss2,
                this.tslColumns,
                this.cboColumns,
                this.tsbCustomOnly,
                this.tsbManagedOnly,
                this.tss3,
                this.tsbGenerate,
                this.tsbExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(900, 27);
            this.toolStrip.TabIndex = 0;
            //
            // tsbClose
            //
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Text = "Close";
            //
            // tslScope
            //
            this.tslScope.Name = "tslScope";
            this.tslScope.Text = "Scope:";
            //
            // cboScope
            //
            this.cboScope.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboScope.Name = "cboScope";
            this.cboScope.Size = new System.Drawing.Size(120, 27);
            //
            // cboScopeValue
            //
            this.cboScopeValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboScopeValue.Name = "cboScopeValue";
            this.cboScopeValue.Size = new System.Drawing.Size(200, 27);
            this.cboScopeValue.Enabled = false;
            //
            // tsbLoadTables
            //
            this.tsbLoadTables.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadTables.Name = "tsbLoadTables";
            this.tsbLoadTables.Text = "Load tables";
            //
            // tslColumns
            //
            this.tslColumns.Name = "tslColumns";
            this.tslColumns.Text = "Columns:";
            //
            // cboColumns
            //
            this.cboColumns.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboColumns.Name = "cboColumns";
            this.cboColumns.Size = new System.Drawing.Size(150, 27);
            //
            // tsbCustomOnly
            //
            this.tsbCustomOnly.CheckOnClick = true;
            this.tsbCustomOnly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbCustomOnly.Name = "tsbCustomOnly";
            this.tsbCustomOnly.Text = "Custom only";
            //
            // tsbManagedOnly
            //
            this.tsbManagedOnly.CheckOnClick = true;
            this.tsbManagedOnly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbManagedOnly.Name = "tsbManagedOnly";
            this.tsbManagedOnly.Text = "Managed only";
            //
            // tsbGenerate
            //
            this.tsbGenerate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbGenerate.Name = "tsbGenerate";
            this.tsbGenerate.Text = "▶ Generate";
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export";
            this.tsbExport.Enabled = false;
            //
            // split
            //
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Location = new System.Drawing.Point(0, 27);
            this.split.Name = "split";
            this.split.Size = new System.Drawing.Size(900, 551);
            this.split.SplitterDistance = 280;
            this.split.TabIndex = 1;
            //
            // split.Panel1 (table picker)
            //
            this.split.Panel1.Controls.Add(this.clbTables);
            this.split.Panel1.Controls.Add(this.txtTableFilter);
            this.split.Panel1.Controls.Add(this.lblTables);
            //
            // split.Panel2 (preview)
            //
            this.split.Panel2.Controls.Add(this.txtPreview);
            //
            // lblTables
            //
            this.lblTables.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTables.Name = "lblTables";
            this.lblTables.Height = 22;
            this.lblTables.Padding = new System.Windows.Forms.Padding(4, 4, 0, 0);
            this.lblTables.Text = "Tables (check to include)";
            //
            // txtTableFilter
            //
            this.txtTableFilter.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtTableFilter.Name = "txtTableFilter";
            //
            // clbTables
            //
            this.clbTables.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clbTables.CheckOnClick = true;
            this.clbTables.IntegralHeight = false;
            this.clbTables.Name = "clbTables";
            //
            // txtPreview
            //
            this.txtPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPreview.Multiline = true;
            this.txtPreview.ReadOnly = true;
            this.txtPreview.Name = "txtPreview";
            this.txtPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtPreview.WordWrap = false;
            this.txtPreview.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtPreview.Text = "Load tables, check the ones to diagram, then Generate.";
            //
            // lblStats
            //
            this.lblStats.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStats.Name = "lblStats";
            this.lblStats.Height = 22;
            this.lblStats.Padding = new System.Windows.Forms.Padding(6, 4, 0, 0);
            this.lblStats.Text = "Ready.";
            //
            // ErdGeneratorControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.split);
            this.Controls.Add(this.lblStats);
            this.Controls.Add(this.toolStrip);
            this.Name = "ErdGeneratorControl";
            this.Size = new System.Drawing.Size(900, 600);
            this.Load += new System.EventHandler(this.ErdGeneratorControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel1.PerformLayout();
            this.split.Panel2.ResumeLayout(false);
            this.split.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split)).EndInit();
            this.split.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.ToolStripSeparator tss1;
        private System.Windows.Forms.ToolStripLabel tslScope;
        private System.Windows.Forms.ToolStripComboBox cboScope;
        private System.Windows.Forms.ToolStripComboBox cboScopeValue;
        private System.Windows.Forms.ToolStripButton tsbLoadTables;
        private System.Windows.Forms.ToolStripSeparator tss2;
        private System.Windows.Forms.ToolStripLabel tslColumns;
        private System.Windows.Forms.ToolStripComboBox cboColumns;
        private System.Windows.Forms.ToolStripButton tsbCustomOnly;
        private System.Windows.Forms.ToolStripButton tsbManagedOnly;
        private System.Windows.Forms.ToolStripSeparator tss3;
        private System.Windows.Forms.ToolStripButton tsbGenerate;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.TextBox txtTableFilter;
        private System.Windows.Forms.CheckedListBox clbTables;
        private System.Windows.Forms.Label lblTables;
        private System.Windows.Forms.TextBox txtPreview;
        private System.Windows.Forms.Label lblStats;
    }
}
