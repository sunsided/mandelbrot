namespace Widemeadows.Visualization.Mandelbrot
{
    partial class BrotForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer _components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (_components != null))
            {
                _components.Dispose();
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
            this._glControl = new OpenTK.GLControl();
            this.SuspendLayout();
            //
            // glControl
            //
            this._glControl.BackColor = System.Drawing.Color.Black;
            this._glControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._glControl.Location = new System.Drawing.Point(0, 0);
            this._glControl.Name = "_glControl";
            this._glControl.Size = new System.Drawing.Size(1008, 729);
            this._glControl.TabIndex = 0;
            this._glControl.VSync = true;
            this._glControl.Load += new System.EventHandler(GlControlLoad);
            this._glControl.Paint += new System.Windows.Forms.PaintEventHandler(GlControlPaint);
            this._glControl.MouseDown += new System.Windows.Forms.MouseEventHandler(GlControlMouseDown);
            this._glControl.MouseMove += new System.Windows.Forms.MouseEventHandler(GlControlMouseMove);
            this._glControl.MouseUp += new System.Windows.Forms.MouseEventHandler(GlControlMouseUp);
            this._glControl.Resize += new System.EventHandler(GlControlResize);
            this._glControl.MouseWheel += new System.Windows.Forms.MouseEventHandler(GlControlMouseWheel);
            //
            // BrotForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 729);
            this.Controls.Add(_glControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MinimumSize = new System.Drawing.Size(1024, 768);
            this.Name = "BrotForm";
            this.Text = "Mandelbrot";
            this.ResumeLayout(false);

        }

        #endregion

        private OpenTK.GLControl _glControl;
    }
}

