namespace XrmToolSuite.TemplateTool
{
    partial class TemplateToolControl
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
            this.tsbLoadSample = new System.Windows.Forms.ToolStripButton();
            this.lvResults = new System.Windows.Forms.ListView();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colCreatedOn = new System.Windows.Forms.ColumnHeader();
            this.colId = new System.Windows.Forms.ColumnHeader();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbLoadSample});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(800, 25);
            this.toolStrip.TabIndex = 0;
            //
            // tsbLoadSample
            //
            this.tsbLoadSample.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbLoadSample.Name = "tsbLoadSample";
            this.tsbLoadSample.Size = new System.Drawing.Size(120, 22);
            this.tsbLoadSample.Text = "Load sample data";
            this.tsbLoadSample.Click += new System.EventHandler(this.tsbLoadSample_Click);
            //
            // lvResults
            //
            this.lvResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colName,
                this.colCreatedOn,
                this.colId});
            this.lvResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvResults.FullRowSelect = true;
            this.lvResults.Location = new System.Drawing.Point(0, 25);
            this.lvResults.Name = "lvResults";
            this.lvResults.Size = new System.Drawing.Size(800, 575);
            this.lvResults.TabIndex = 1;
            this.lvResults.UseCompatibleStateImageBehavior = false;
            this.lvResults.View = System.Windows.Forms.View.Details;
            //
            // columns
            //
            this.colName.Text = "Name";
            this.colName.Width = 300;
            this.colCreatedOn.Text = "Created On";
            this.colCreatedOn.Width = 150;
            this.colId.Text = "Id";
            this.colId.Width = 260;
            //
            // TemplateToolControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lvResults);
            this.Controls.Add(this.toolStrip);
            this.Name = "TemplateToolControl";
            this.Size = new System.Drawing.Size(800, 600);
            this.Load += new System.EventHandler(this.TemplateToolControl_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbLoadSample;
        private System.Windows.Forms.ListView lvResults;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colCreatedOn;
        private System.Windows.Forms.ColumnHeader colId;
    }
}
