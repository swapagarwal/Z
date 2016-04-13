namespace Z
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.volume_mode = new System.Windows.Forms.CheckBox();
            this.brightness_mode = new System.Windows.Forms.CheckBox();
            this.application_searcher = new System.Windows.Forms.TextBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Applications = new System.Windows.Forms.DataGridViewLinkColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // volume_mode
            // 
            this.volume_mode.AutoSize = true;
            this.volume_mode.Location = new System.Drawing.Point(12, 12);
            this.volume_mode.Name = "volume_mode";
            this.volume_mode.Size = new System.Drawing.Size(111, 17);
            this.volume_mode.TabIndex = 3;
            this.volume_mode.Text = "Automatic Volume";
            this.volume_mode.UseVisualStyleBackColor = true;
            // 
            // brightness_mode
            // 
            this.brightness_mode.AutoSize = true;
            this.brightness_mode.Location = new System.Drawing.Point(147, 12);
            this.brightness_mode.Name = "brightness_mode";
            this.brightness_mode.Size = new System.Drawing.Size(125, 17);
            this.brightness_mode.TabIndex = 4;
            this.brightness_mode.Text = "Automatic Brightness";
            this.brightness_mode.UseVisualStyleBackColor = true;
            // 
            // application_searcher
            // 
            this.application_searcher.Location = new System.Drawing.Point(12, 35);
            this.application_searcher.Name = "application_searcher";
            this.application_searcher.Size = new System.Drawing.Size(260, 20);
            this.application_searcher.TabIndex = 6;
            this.application_searcher.TextChanged += new System.EventHandler(this.application_searcher_TextChanged);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Applications});
            this.dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dataGridView1.Location = new System.Drawing.Point(12, 61);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowTemplate.ReadOnly = true;
            this.dataGridView1.ShowEditingIcon = false;
            this.dataGridView1.Size = new System.Drawing.Size(260, 267);
            this.dataGridView1.TabIndex = 8;
            // 
            // Applications
            // 
            this.Applications.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Applications.HeaderText = "Applications";
            this.Applications.Name = "Applications";
            this.Applications.Text = "";
            this.Applications.TrackVisitedState = false;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 336);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.application_searcher);
            this.Controls.Add(this.brightness_mode);
            this.Controls.Add(this.volume_mode);
            this.Name = "MainWindow";
            this.Text = "PC Assistant";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox volume_mode;
        private System.Windows.Forms.CheckBox brightness_mode;
        private System.Windows.Forms.TextBox application_searcher;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewLinkColumn Applications;
    }
}

