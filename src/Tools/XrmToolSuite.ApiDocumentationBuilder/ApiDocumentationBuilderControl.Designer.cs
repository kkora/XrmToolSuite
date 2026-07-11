namespace XrmToolSuite.ApiDocumentationBuilder
{
    partial class ApiDocumentationBuilderControl
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
            this.tsbIncludeExamples = new System.Windows.Forms.ToolStripButton();
            this.tslRedact = new System.Windows.Forms.ToolStripLabel();
            this.tstRedact = new System.Windows.Forms.ToolStripTextBox();
            this.tssSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExport = new System.Windows.Forms.ToolStripDropDownButton();
            this.tslPreview = new System.Windows.Forms.ToolStripLabel();
            this.tscPreview = new System.Windows.Forms.ToolStripComboBox();
            this.tsbOpenBrowser = new System.Windows.Forms.ToolStripButton();
            this.txtPreview = new System.Windows.Forms.TextBox();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoad,
                this.tssSeparator1,
                this.tsbIncludeExamples,
                this.tslRedact,
                this.tstRedact,
                this.tssSeparator2,
                this.tslPreview,
                this.tscPreview,
                this.tsbOpenBrowser,
                this.tsbExport});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1000, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoad
            //
            this.tsbLoad.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoad.Name = "tsbLoad";
            this.tsbLoad.Text = "Load Custom APIs";
            //
            // tsbIncludeExamples
            //
            this.tsbIncludeExamples.CheckOnClick = true;
            this.tsbIncludeExamples.Checked = true;
            this.tsbIncludeExamples.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsbIncludeExamples.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbIncludeExamples.Name = "tsbIncludeExamples";
            this.tsbIncludeExamples.Text = "Include examples";
            //
            // tslRedact
            //
            this.tslRedact.Name = "tslRedact";
            this.tslRedact.Text = "Redact terms:";
            //
            // tstRedact
            //
            this.tstRedact.Name = "tstRedact";
            this.tstRedact.Size = new System.Drawing.Size(180, 25);
            this.tstRedact.ToolTipText = "Extra comma-separated name fragments to mask (added to the built-in secret list)";
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
            this.tscPreview.Size = new System.Drawing.Size(150, 25);
            //
            // tsbOpenBrowser
            //
            this.tsbOpenBrowser.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbOpenBrowser.Enabled = false;
            this.tsbOpenBrowser.Name = "tsbOpenBrowser";
            this.tsbOpenBrowser.Text = "Open in browser";
            this.tsbOpenBrowser.ToolTipText = "Render the HTML preview in your default browser";
            this.tsbOpenBrowser.Click += new System.EventHandler(this.tsbOpenBrowser_Click);
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
            // ApiDocumentationBuilderControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtPreview);
            this.Controls.Add(this.toolStrip);
            this.Name = "ApiDocumentationBuilderControl";
            this.Size = new System.Drawing.Size(1000, 600);
            this.Load += new System.EventHandler(this.ApiDocumentationBuilderControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoad;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.ToolStripButton tsbIncludeExamples;
        private System.Windows.Forms.ToolStripLabel tslRedact;
        private System.Windows.Forms.ToolStripTextBox tstRedact;
        private System.Windows.Forms.ToolStripSeparator tssSeparator2;
        private System.Windows.Forms.ToolStripDropDownButton tsbExport;
        private System.Windows.Forms.ToolStripLabel tslPreview;
        private System.Windows.Forms.ToolStripComboBox tscPreview;
        private System.Windows.Forms.ToolStripButton tsbOpenBrowser;
        private System.Windows.Forms.TextBox txtPreview;
    }
}
