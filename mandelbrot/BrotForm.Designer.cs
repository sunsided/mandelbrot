namespace widemeadows.Visualization.Mandelbrot
{
    partial class BrotForm
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
            this.glControl = new OpenTK.GLControl();
            this.SuspendLayout();
            // 
            // glControl
            // 
            this.glControl.BackColor = System.Drawing.Color.Black;
            this.glControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.glControl.Location = new System.Drawing.Point(0, 0);
            this.glControl.Name = "glControl";
            this.glControl.Size = new System.Drawing.Size(1008, 729);
            this.glControl.TabIndex = 0;
            this.glControl.VSync = true;
            this.glControl.Load += new System.EventHandler(this.GlControlLoad);
            this.glControl.Paint += new System.Windows.Forms.PaintEventHandler(this.GlControlPaint);
            this.glControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GlControlMouseDown);
            this.glControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.GlControlMouseMove);
            this.glControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.GlControlMouseUp);
            this.glControl.Resize += new System.EventHandler(this.GlControlResize);
            // 
            // BrotForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 729);
            this.Controls.Add(this.glControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MinimumSize = new System.Drawing.Size(1024, 768);
            this.Name = "BrotForm";
            this.Text = "Mandelbrot";
            this.ResumeLayout(false);

        }

        #endregion

        private OpenTK.GLControl glControl;
    }
}

