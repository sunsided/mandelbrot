using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace widemeadows.Visualization.Mandelbrot
{
    /// <summary>
    /// Class BrotForm.
    /// </summary>
    public partial class BrotForm : Form
    {
        /// <summary>
        /// Determines if the OpenGL control has been loaded
        /// </summary>
        private bool _glLoaded;

        /// <summary>
        /// The back buffer for rendering the Mandelbrot set
        /// </summary>
        [CanBeNull]
        private Bitmap _backBuffer;
        
        /// <summary>
        /// The starting position
        /// </summary>
        private Complex _topLeft = new Complex(-2D, 1D);

        /// <summary>
        /// The end position
        /// </summary>
        private Complex _bottomRight = new Complex(1D, -1D);

        /// <summary>
        /// The delta value on the imaginary axis
        /// </summary>
        private Complex _deltaImaginary;
        
        /// <summary>
        /// The delta value on the real axis
        /// </summary>
        private Complex _deltaReal;

        /// <summary>
        /// The pixel format
        /// </summary>
        const PixelFormat PixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

        /// <summary>
        /// The last window state
        /// </summary>
        private FormWindowState LastWindowState;

        /// <summary>
        /// The OpenGL texture identifier as created in the call to <see cref="SetupTexture"/>
        /// </summary>
        private int _textureId;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrotForm"/> class.
        /// </summary>
        public BrotForm()
        {
            InitializeComponent();

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.Opaque |
                ControlStyles.UserPaint,
                true);

            LastWindowState = WindowState;
        }

        /// <summary>
        /// Handles the <see cref="E:Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PrepareNewBufferAndInvalidate();
        }

        /// <summary>
        /// Handles the <see cref="E:ResizeBegin" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);
            ResetBuffer();
        }

        /// <summary>
        /// Handles the <see cref="E:ResizeEnd" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            PrepareNewBufferAndInvalidate();
        }

        /// <summary>
        /// Handles the <see cref="E:Resize" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // The maximize event is not caught by the OnResize*() methods,
            // so we'll handle it here if the form is being maximized.
            if (LastWindowState == WindowState) return;
            LastWindowState = WindowState;
            if (WindowState == FormWindowState.Maximized || WindowState == FormWindowState.Normal)
            {
                PrepareNewBufferAndInvalidate();
            }
        }

        /// <summary>
        /// Resets the buffer.
        /// </summary>
        private void ResetBuffer()
        {
            _backBuffer = null;
        }

        /// <summary>
        /// Prepares the new buffer and invalidates the display.
        /// </summary>
        private void PrepareNewBufferAndInvalidate()
        {
            var width = ClientSize.Width;
            var height = ClientSize.Height;

            // create a new image buffer
            _backBuffer = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat);

            // determine the range in the complex plane
            _topLeft = new Complex(-2D, 1D);
            _bottomRight = new Complex(1D, -1D);

            // determine the step parameters in the complex plane
            var realDelta = (_bottomRight.Real - _topLeft.Real)/(width - 1D);
            var imagDelta = (_bottomRight.Imaginary - _topLeft.Imaginary)/(height - 1D); // flipped, because of the way the bitmap works

            // calculate the actual positions and steps in the complex plane
            _deltaImaginary = new Complex(0, imagDelta);
            _deltaReal = new Complex(realDelta, 0);

            Invalidate();
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
            if (buffer == null) return;

            /*
            // Bake the bread
            RenderMandelbrotSet(buffer);

            // Render the back buffer onto the form
            var gr = e.Graphics;
            gr.DrawImageUnscaledAndClipped(buffer, ClientRectangle);
            */
        }

        /// <summary>
        /// Renders the mandelbrot set onto the <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        private unsafe void RenderMandelbrotSet([NotNull] Bitmap buffer)
        {
            Debug.Assert(PixelFormat == PixelFormat.Format32bppArgb, "pixelFormat == PixelFormat.Format32bppArgb");
            const int bytesPerPixel = 4;

            // Predetermine width and height "constants"
            var width = buffer.Width;
            var height = buffer.Height;

            // Lock the bitmap for rendering
            var bitmapData = buffer.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat);
            var scan0 = bitmapData.Scan0;

            // fetch some base pointers
            var stride = bitmapData.Stride;
            var basePointer = (byte*) scan0.ToPointer();
            
            // determine the running variables in the complex plane
            var linePosition = _topLeft;
            var lineIncrease = _deltaImaginary;
            var pixelIncrease = _deltaReal;

            // iterate over all lines and columns
            var options = new ParallelOptions
                          {
                              MaxDegreeOfParallelism = Environment.ProcessorCount
                          };

            const int maxIterations = 100;
            const double inverseMaxIterations = 1.0D/maxIterations;
            Parallel.For(0, height, options, y =>
                                    {
                                        var linePointer = basePointer + y*stride;
                                        var pixel = linePointer;
                                        var pixelPosition = linePosition + y*lineIncrease;
                                        for (var x = 0; x < width; ++x)
                                        {
                                            // do the Mandelmagic
                                            Complex finalZ;
                                            var iterations = CalculateFractalIterations(pixelPosition, maxIterations, out finalZ);

                                            // determine a smooth interpolation value
                                            // (https://en.wikibooks.org/wiki/Fractals/Iterations_in_the_complex_plane/Mandelbrot_set#Real_Escape_Time)
                                            var smoothIterations = iterations < maxIterations
                                                    ? iterations - Math.Log(Math.Log(finalZ.Magnitude, 2), 2)
                                                    : 0;

                                            // interpolate the color
                                            var red = 255D*inverseMaxIterations*smoothIterations;
                                            var green = 0;
                                            var blue = 0;

                                            // patch the colors
                                            pixel[0] = (byte)blue;
                                            pixel[1] = (byte)green;
                                            pixel[2] = (byte)red;
                                            pixel[3] = 255; // full alpha

                                            // advance the pixel pointer
                                            pixel += bytesPerPixel;
                                            pixelPosition += pixelIncrease;
                                        }
                                    });
            
            // Release the Kraken
            buffer.UnlockBits(bitmapData);
        }

        /// <summary>
        /// Determines the number of iterations required for the Mandelbrot
        /// fractal <c>Z_{n+1}(c) = z^2_{n}(c) + c</c> to reach the value of <c>2</c>.
        /// </summary>
        /// <param name="c">The location in the complex plane.</param>
        /// <param name="maxIterations">The maximum number of iterations.</param>
        /// <param name="z">The z.</param>
        /// <returns>System.Int32.</returns>
        private static int CalculateFractalIterations(Complex c, int maxIterations, out Complex z)
        {
            var iteration = 0;

            const double maxMagnitude = 2D;
            const double maxMagnitudeSquared = maxMagnitude*maxMagnitude;

            // iterate until the maximum magnitude is reached
            z = Complex.Zero;
            while (++iteration < maxIterations)
            {
                z = z*z + c;

                var squaredNorm = z.Real*z.Real + z.Imaginary*z.Imaginary;
                if (squaredNorm >= maxMagnitudeSquared) break;
            } 

            return iteration;
        }

        /// <summary>
        /// Handles the Load event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void glControl_Load(object sender, EventArgs e)
        {
            _glLoaded = true;

            GL.ClearColor(Color.OrangeRed);
            GL.Enable(EnableCap.Texture2D);

            SetupViewport();
            SetupTexture();
        }

        /// <summary>
        /// Handles the Resize event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void glControl_Resize(object sender, EventArgs e)
        {
            if (!_glLoaded) return;
            SetupViewport();
        }

        /// <summary>
        /// Prepares the rendering texture
        /// </summary>
        private void SetupTexture()
        {
            // Create a texture and bind it for all future texture function calls

            var id = _textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            // We will not upload mipmaps, so disable mipmapping (otherwise the texture will not appear).
            // We can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
            // mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // Render the mandelbrot set onto the buffer
            PrepareNewBufferAndInvalidate();
            RenderMandelbrotSet(_backBuffer);
            
            var data = _backBuffer.LockBits(new Rectangle(0, 0, _backBuffer.Width, _backBuffer.Height), ImageLockMode.ReadOnly, PixelFormat);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _backBuffer.Width, _backBuffer.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            _backBuffer.UnlockBits(data);
        }

        /// <summary>
        /// Prepares the OpenGL viewport.
        /// </summary>
        private void SetupViewport()
        {
            int width = glControl.Width;
            int height = glControl.Height;

            // Prepare the projection matrix
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            // top-left corner is (0,0)
            GL.Ortho(0, 1, 1, 0, -1, 1);

            // use the full painting area
            GL.Viewport(0, 0, width, height); // Use all of the glControl painting area

            // Prepare the modelview matrix
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
        }

        /// <summary>
        /// Handles the Paint event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PaintEventArgs"/> instance containing the event data.</param>
        private void glControl_Paint(object sender, PaintEventArgs e)
        {
            if (!_glLoaded) return;

            GL.Clear(ClearBufferMask.DepthBufferBit);

            GL.LoadIdentity();

            // Texture is already bound
            //GL.BindTexture(TextureTarget.Texture2D, _textureId);

            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0);
            GL.Vertex2(0, 0);

            GL.TexCoord2(1, 0);
            GL.Vertex2(1, 0);

            GL.TexCoord2(1, 1);
            GL.Vertex2(1, 1);
            
            GL.TexCoord2(0, 1);
            GL.Vertex2(0, 1);
            
            GL.End();

            GL.Flush();
            glControl.SwapBuffers();
        }
    }
}
