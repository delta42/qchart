
namespace QChart
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.lblSelectMVD = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblSelectMVD
            // 
            this.lblSelectMVD.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectMVD.Font = new System.Drawing.Font("Segoe UI", 36F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblSelectMVD.ForeColor = System.Drawing.Color.Silver;
            this.lblSelectMVD.Location = new System.Drawing.Point(0, 0);
            this.lblSelectMVD.Name = "lblSelectMVD";
            this.lblSelectMVD.Size = new System.Drawing.Size(1584, 961);
            this.lblSelectMVD.TabIndex = 0;
            this.lblSelectMVD.Text = "Drag and Drop an MVD file here\r\nor Double-Click to Browse to a file";
            this.lblSelectMVD.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblSelectMVD.DoubleClick += new System.EventHandler(this.lblSelectMVD_DoubleClick);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(1584, 961);
            this.Controls.Add(this.lblSelectMVD);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormMain";
            this.Text = "QChart";
            this.Shown += new System.EventHandler(this.FormMain_Shown);
            this.Resize += new System.EventHandler(this.FormMain_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblSelectMVD;
    }
}

