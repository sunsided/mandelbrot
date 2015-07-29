using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Windows.Forms;

namespace widemeadows.Visualization.Mandelbrot
{
    /// <summary>
    /// Class BrotForm.
    /// </summary>
    public partial class BrotForm : Form
    {
        /// <summary>
        /// The back buffer for rendering the Mandelbrot set
        /// </summary>
        private Bitmap _backBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrotForm"/> class.
        /// </summary>
        public BrotForm()
        {
            InitializeComponent();

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.Opaque |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
        }

        /// <summary>
        /// Handles the <see cref="E:Resize" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _backBuffer = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Handles the <see cref="E:Paint" /> event.
        /// </summary>
        /// <param name="e">The <see cref="PaintEventArgs"/> instance containing the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Fetch the buffer
            var buffer = _backBuffer;
            RenderMandelbrotSet(buffer);

            // Render the back buffer onto the form
            var gr = e.Graphics;
            gr.DrawImageUnscaledAndClipped(buffer, ClientRectangle);
        }

        /// <summary>
        /// Renders the mandelbrot set onto the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        private unsafe void RenderMandelbrotSet(Bitmap buffer)
        {
            const PixelFormat pixelFormat = PixelFormat.Format32bppArgb;
            const int bytesPerPixel = 4;

            // Predetermine width and height "constants"
            var width = buffer.Width;
            var height = buffer.Height;
            var scaledWidth = bytesPerPixel * width;

            // Lock the bitmap for rendering
            var bitmapData = buffer.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, pixelFormat);
            var scan0 = bitmapData.Scan0;

            // fetch some base pointers
            var stride = bitmapData.Stride;
            var basePointer = (byte*) scan0.ToPointer();

            // determine the range in the complex plane
            var realMin = -2D;
            var realMax = 1D;
            var imagMin = -1D;
            var imagMax = 1D;

            // determine the step parameters in the complex plane
            var realDelta = (realMax - realMin) /(width - 1D);
            var imagDelta = (imagMin - imagMax)/(height - 1D); // flipped, because of the way the bitmap works

            // determine the running variables in the complex plane
            var linePosition = new Complex(realMin, imagMax);
            var lineIncrease = new Complex(0, imagDelta);
            var pixelIncrease = new Complex(realDelta, 0);

            // iterate over all lines and columns
            var linePointer = basePointer;
            for (var y = 0; y < height; ++y)
            {
                var pixel = linePointer;
                var pixelPosition = linePosition;
                for (var x = 0; x < scaledWidth; x += bytesPerPixel)
                {
                    // do the Mandelmagic
                    var iterations = CalculateFractalIterations(pixelPosition);

                    // patch the colors
                    pixel[0] = 0; // blue
                    pixel[1] = 0; // green
                    pixel[2] = (byte)iterations; // red
                    pixel[3] = 255; // alpha

                    // advance the pixel pointer
                    pixel += bytesPerPixel;
                    pixelPosition += pixelIncrease;
                }

                // advance the line pointer
                linePointer += stride;
                linePosition += lineIncrease;
            }

            // Release the Kraken
            buffer.UnlockBits(bitmapData);
        }
        
        /// <summary>
        /// Determines the number of iterations required for the Mandelbrot
        /// fractal <c>Z_{n+1}(c) = z^2_{n}(c) + c</c> to reach the value of <c>2</c>.
        /// </summary>
        /// <param name="c">The location in the complex plane.</param>
        /// <returns>System.Int32.</returns>
        private int CalculateFractalIterations(Complex c)
        {
            const int maxIterations = 255;
            var iteration = 0;

            const double maxMagnitude = 2D;
            const double maxMagnitudeSquared = maxMagnitude*maxMagnitude;

            var z = Complex.Zero;
            while (iteration < maxIterations)
            {
                ++iteration;
                z = z*z + c;

                var squaredNorm = z.Real*z.Real + z.Imaginary*z.Imaginary;
                if (squaredNorm >= maxMagnitudeSquared) break;
            } 

            return iteration;
        }
    }
}
