namespace XrmToolSuite.ArchitectureDiagramGenerator
{
    partial class ArchitectureDiagramGeneratorControl
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
            this.tscSolution = new System.Windows.Forms.ToolStripComboBox();
            this.tsbGenerate = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tslLayout = new System.Windows.Forms.ToolStripLabel();
            this.tscLayout = new System.Windows.Forms.ToolStripComboBox();
            this.tscDirection = new System.Windows.Forms.ToolStripComboBox();
            this.tsbHideOrphans = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tslPreview = new System.Windows.Forms.ToolStripLabel();
            this.tscPreview = new System.Windows.Forms.ToolStripComboBox();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.txtPreview = new System.Windows.Forms.TextBox();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoadSolutions,
                this.tscSolution,
                this.tsbGenerate,
                this.tssSeparator1,
                this.tslLayout,
                this.tscLayout,
                this.tscDirection,
                this.tsbHideOrphans,
                this.tssSeparator2,
                this.tsbExport,
                this.tslPreview,
                this.tscPreview,
                this.tsbClose});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1000, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoadSolutions
            //
            this.tsbLoadSolutions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadSolutions.Name = "tsbLoadSolutions";
            this.tsbLoadSolutions.Text = "Load solutions";
            //
            // tscSolution
            //
            this.tscSolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscSolution.Name = "tscSolution";
            this.tscSolution.Size = new System.Drawing.Size(260, 25);
            //
            // tsbGenerate
            //
            this.tsbGenerate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbGenerate.Name = "tsbGenerate";
            this.tsbGenerate.Text = "Generate";
            //
            // tslLayout
            //
            this.tslLayout.Name = "tslLayout";
            this.tslLayout.Text = "Layout:";
            //
            // tscLayout
            //
            this.tscLayout.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscLayout.Name = "tscLayout";
            this.tscLayout.Size = new System.Drawing.Size(150, 25);
            //
            // tscDirection
            //
            this.tscDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscDirection.Name = "tscDirection";
            this.tscDirection.Size = new System.Drawing.Size(120, 25);
            //
            // tsbHideOrphans
            //
            this.tsbHideOrphans.CheckOnClick = true;
            this.tsbHideOrphans.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbHideOrphans.Name = "tsbHideOrphans";
            this.tsbHideOrphans.Text = "Hide orphans";
            //
            // tsbExport
            //
            this.tsbExport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbExport.Enabled = false;
            this.tsbExport.Name = "tsbExport";
            this.tsbExport.Text = "Export";
            //
            // tslPreview
            //
            this.tslPreview.Name = "tslPreview";
            this.tslPreview.Text = "Preview:";
            //
            // tscPreview
            //
            this.tscPreview.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscPreview.Name = "tscPreview";
            this.tscPreview.Size = new System.Drawing.Size(120, 25);
            //
            // tsbClose
            //
            this.tsbClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Text = "Close";
            //
            // txtPreview
            //
            this.txtPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPreview.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.txtPreview.Location = new System.Drawing.Point(0, 25);
            this.txtPreview.Multiline = true;
            this.txtPreview.Name = "txtPreview";
            this.txtPreview.ReadOnly = true;
            this.txtPreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtPreview.WordWrap = false;
            this.txtPreview.Size = new System.Drawing.Size(1000, 575);
            this.txtPreview.TabIndex = 1;
            //
            // ArchitectureDiagramGeneratorControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtPreview);
            this.Controls.Add(this.toolStrip);
            this.Name = "ArchitectureDiagramGeneratorControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.Load += new System.EventHandler(this.ArchitectureDiagramGeneratorControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoadSolutions;
        private System.Windows.Forms.ToolStripComboBox tscSolution;
        private System.Windows.Forms.ToolStripButton tsbGenerate;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripLabel tslLayout;
        private System.Windows.Forms.ToolStripComboBox tscLayout;
        private System.Windows.Forms.ToolStripComboBox tscDirection;
        private System.Windows.Forms.ToolStripButton tsbHideOrphans;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripLabel tslPreview;
        private System.Windows.Forms.ToolStripComboBox tscPreview;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.TextBox txtPreview;
    }
}
