using System;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace Widemeadows.Visualization.Mandelbrot
{
    /// <summary>
    /// Class BrotForm.
    /// </summary>
    internal sealed partial class BrotForm : Form
    {
        /// <summary>
        /// Determines if the OpenGL control has been loaded
        /// </summary>
        private bool _glLoaded;

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
        /// The OpenGL texture identifier as created in the call to <see cref="SetupTexture"/>
        /// </summary>
        private int _textureId;

        /// <summary>
        /// The buffer for rendering the Mandelbrot set
        /// </summary>
        private byte[] _backBuffer;

        /// <summary>
        /// The texture width
        /// </summary>
        private int _textureWidth;

        /// <summary>
        /// The texture height
        /// </summary>
        private int _textureHeight;

        /// <summary>
        /// The texture stride
        /// </summary>
        private int _textureStride;

        /// <summary>
        /// The mouse down location
        /// </summary>
        private Point _mouseDownLocation;

        /// <summary>
        /// The left mouse button is pressed down
        /// </summary>
        private bool _leftMouseDown;

        /// <summary>
        /// The translation in complex space
        /// </summary>
        private Complex _translate = Complex.Zero;

        /// <summary>
        /// The number of bytes per pixel
        /// </summary>
        const int BytesPerPixel = 4;

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
        }

        /// <summary>
        /// Prepares the new buffer.
        /// </summary>
        private void PrepareNewBuffer()
        {
            var width = ClientSize.Width;
            var height = ClientSize.Height;

            // we don't do that
            if (width == 0 || height == 0) return;

            // create a new image buffer
            _textureWidth = width;
            _textureHeight = height;
            _textureStride = width*BytesPerPixel;
            _backBuffer = new byte[(width * BytesPerPixel) * height];

            UpdateSteps();
        }

        /// <summary>
        /// Updates the steps.
        /// </summary>
        private void UpdateSteps()
        {
            // determine the step parameters in the complex plane
            var realDelta = (_bottomRight.Real - _topLeft.Real)/(_textureWidth - 1D);
            var imagDelta = (_bottomRight.Imaginary - _topLeft.Imaginary)/(_textureHeight- 1D); // flipped, because of the way the bitmap works

            // calculate the actual positions and steps in the complex plane
            _deltaImaginary = new Complex(0, imagDelta);
            _deltaReal = new Complex(realDelta, 0);
        }

        /// <summary>
        /// Helper structure to bundle per-line rendering parameters
        /// </summary>
        private unsafe struct RenderInfo
        {
            /// <summary>
            /// The image width
            /// </summary>
            internal readonly int Width;

            /// <summary>
            /// The line pointer
            /// </summary>
            [NotNull]
            internal readonly byte* LinePointer;

            /// <summary>
            /// The position in the complex plane
            /// </summary>
            internal readonly Complex PixelPosition;

            /// <summary>
            /// The positional increment in x direction
            /// </summary>
            internal readonly Complex PixelIncrease;

            /// <summary>
            /// Initializes a new instance of the <see cref="RenderInfo"/> struct.
            /// </summary>
            /// <param name="linePointer">The line pointer.</param>
            /// <param name="pixelPosition">The pixel position.</param>
            /// <param name="pixelIncrease">The pixel increase.</param>
            /// <param name="width">The width.</param>
            internal RenderInfo([NotNull] byte* linePointer, Complex pixelPosition, Complex pixelIncrease, int width)
            {
                LinePointer = linePointer;
                PixelPosition = pixelPosition;
                PixelIncrease = pixelIncrease;
                Width = width;
            }
        }

        /// <summary>
        /// Renders the mandelbrot set onto the <paramref name="buffer" />.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="height">The height.</param>
        /// <param name="width">The width.</param>
        /// <param name="stride">The stride.</param>
        private unsafe void RenderMandelbrotSet([NotNull] byte[] buffer, int height, int width, int stride)
        {
            // Lock the bitmap for rendering
            fixed (byte* scan0 = buffer)
            {
                // fetch some base pointers
                var basePointer = scan0;

                // determine the running variables in the complex plane
                var linePosition = _topLeft + _translate;
                var lineIncrease = _deltaImaginary;
                var pixelIncrease = _deltaReal;

                // iterate over all lines and columns
                var options = new ParallelOptions
                              {
                                  MaxDegreeOfParallelism = Environment.ProcessorCount
                              };

                const int maxIterations = 100;
                Parallel.For(0, height, options, y =>
                                                 {
                                                     var linePointer = basePointer + y * stride;
                                                     var pixelPosition = linePosition + y * lineIncrease;
                                                     var ii = new RenderInfo(linePointer, pixelPosition, pixelIncrease, width);

                                                     RenderSingleLine(ref ii, maxIterations);
                                                 });

            } // Release the Kraken
        }

        /// <summary>
        /// Renders a single line of the mandelbrot image.
        /// </summary>
        /// <param name="renderInfo">The image information.</param>
        /// <param name="maxIterations">The maximum iterations.</param>
        private static unsafe void RenderSingleLine(ref RenderInfo renderInfo, int maxIterations)
        {
            var colorScale = 255D / maxIterations;

            var width = renderInfo.Width;
            var pixel = renderInfo.LinePointer;
            var pixelPosition = renderInfo.PixelPosition;
            var pixelIncrease = renderInfo.PixelIncrease;

            for (var x = 0; x < width; ++x)
            {
                SetColorAtLocation(pixel, ref pixelPosition, colorScale, maxIterations);

                // advance the pixel pointer
                pixelPosition += pixelIncrease;
                pixel += BytesPerPixel;
            }
        }

        /// <summary>
        /// Sets the color of the <paramref name="pixel"/> at the given <paramref name="location"/>
        /// </summary>
        /// <param name="pixel">The pixel.</param>
        /// <param name="location">The pixel position.</param>
        /// <param name="colorScale">The color scale.</param>
        /// <param name="maxIterations">The maximum iterations.</param>
        private static unsafe void SetColorAtLocation([NotNull] byte* pixel, ref Complex location, double colorScale, int maxIterations)
        {
            const int blue = 0, green = 1, red = 2, alpha = 3;

            // do the Mandelmagic
            var iterations = DetermineNumberOfIterationsRequired(ref location, maxIterations);

            // patch the colors
            pixel[blue] = (byte) 0;
            pixel[green] = (byte) 0;
            pixel[red] = (byte) (iterations*colorScale);
            pixel[alpha] = 255; // full alpha
        }

        /// <summary>
        /// Determines how many iterations are required to determine unboundedness.
        /// </summary>
        /// <param name="location">The pixel position.</param>
        /// <param name="maxIterations">The maximum iterations.</param>
        /// <returns>System.Double.</returns>
        private static double DetermineNumberOfIterationsRequired(ref Complex location, int maxIterations)
        {
            Complex finalZ;
            var iterations = CalculateFractalIterations(location, maxIterations, out finalZ);
            var smoothIterations = InterpolateIterations(maxIterations, iterations, ref finalZ);
            return smoothIterations;
        }

        /// <summary>
        /// Interpolates the iterations so that it becomes smoother.
        /// </summary>
        /// <param name="maxIterations">The maximum iterations.</param>
        /// <param name="iterations">The iterations.</param>
        /// <param name="finalZ">The final z.</param>
        /// <returns>System.Double.</returns>
        private static double InterpolateIterations(int maxIterations, int iterations, ref Complex finalZ)
        {
            // (https://en.wikibooks.org/wiki/Fractals/Iterations_in_the_complex_plane/Mandelbrot_set#Real_Escape_Time)
            return iterations < maxIterations
                ? iterations - Math.Log(Math.Log(finalZ.Magnitude, 2D), 2D)
                : 0D;
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
        private void GlControlLoad(object sender, EventArgs e)
        {
            _glLoaded = true;

            GL.Enable(EnableCap.Texture2D);
            SetupViewport();

            PrepareNewBuffer();
            SetupTexture();
        }

        /// <summary>
        /// Handles the Resize event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GlControlResize(object sender, EventArgs e)
        {
            if (!_glLoaded) return;
            SetupViewport();

            PrepareNewBuffer();
            SetupTexture();
        }

        /// <summary>
        /// Prepares the OpenGL viewport.
        /// </summary>
        private void SetupViewport()
        {
            int width = _glControl.Width;
            int height = _glControl.Height;

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
        /// Prepares the rendering texture
        /// </summary>
        private void SetupTexture()
        {
            var buffer = _backBuffer;
            var width = _textureWidth;
            var height = _textureHeight;
            var stride = _textureStride;
            if (width <= 0 || height <= 0) return;

            RenderMandelbrotSet(buffer, height, width, stride);

            // Create a texture and bind it for all future texture function calls
            if (_textureId > 0) GL.DeleteTexture(_textureId);
            var id = _textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            // Disable mipmapping; set linear filtering.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, buffer);
        }


        /// <summary>
        /// Handles the Paint event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PaintEventArgs"/> instance containing the event data.</param>
        private void GlControlPaint(object sender, PaintEventArgs e)
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
            _glControl.SwapBuffers();
        }

        /// <summary>
        /// Handles the MouseDown event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void GlControlMouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) != MouseButtons.Left) return;
            _leftMouseDown = true;
            _mouseDownLocation = e.Location;
        }

        /// <summary>
        /// Handles the MouseUp event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void GlControlMouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) != MouseButtons.Left) return;
            _leftMouseDown = false;
        }

        /// <summary>
        /// Handles the MouseMove event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        private void GlControlMouseMove(object sender, MouseEventArgs e)
        {
            var translateX = _mouseDownLocation.X - e.Location.X;
            var translateY = _mouseDownLocation.Y - e.Location.Y;

            // calculate the translation and store the current mouse position
            _translate = translateX * _deltaReal + translateY * _deltaImaginary;
            _mouseDownLocation = e.Location;

            // if there is nothing to do, bye
            if (!_leftMouseDown || translateX == 0 && translateY == 0) return;

            // update the edges and re-render
            _topLeft += _translate;
            _bottomRight += _translate;

            SetupTexture();
            _glControl.Refresh();
        }

        /// <summary>
        /// Handles the MouseWheel event of the glControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GlControlMouseWheel(object sender, MouseEventArgs e)
        {
            var zoomFactor = 0.5F;
            if (e.Delta > 0)
            {
                zoomFactor = 2F;
            }

            var topLeft = new Vector2((float)_topLeft.Real, (float)_topLeft.Imaginary);
            var bottomRight = new Vector2((float)_bottomRight.Real, (float)_bottomRight.Imaginary);

            // normalize the current mouse location to the range (0,0)-(1,1)
            var formTopLeft = Vector2.Zero;
            var formBottomRight = new Vector2(_textureWidth, _textureHeight);
            var current = (new Vector2(_mouseDownLocation.X, _mouseDownLocation.Y) - formTopLeft) / (formBottomRight - formTopLeft);

            // interpolate the normalized location to the original bounds
            current = (bottomRight - topLeft)*current + topLeft;

            // calculate the new position
            topLeft = (topLeft - current)*zoomFactor + current;
            bottomRight = (bottomRight - current) * zoomFactor + current;

            // update the edges and re-render
            _topLeft = new Complex(topLeft.X, topLeft.Y);
            _bottomRight = new Complex(bottomRight.X, bottomRight.Y);

            UpdateSteps();
            SetupTexture();
            _glControl.Refresh();
        }
    }
}
