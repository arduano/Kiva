using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace Kiva_MIDI
{
    /// <summary>
    /// Interaction logic for OpenTkControlBase.xaml. OpenTkControlBase is a base class for OpenTK WPF controls
    /// </summary>
    public abstract partial class OpenTkControlBase
    {
        /// <summary>
        /// Initialize the OpenTk Toolkit
        /// </summary>
        static OpenTkControlBase()
        {
            Toolkit.Init(new ToolkitOptions
            {
                Backend = PlatformBackend.PreferNative
            });
        }

        private volatile string _openGlVersion = (string)OpenGlVersionProperty.DefaultMetadata.DefaultValue;
        public static readonly DependencyProperty OpenGlVersionProperty = DependencyProperty.Register(
            nameof(OpenGlVersion), typeof(string), typeof(OpenTkControlBase), new PropertyMetadata("3.0"));

        /// <summary>
        /// Specifies the OpenGL Version to use. Should be formatted as X.X
        /// </summary>
        public string OpenGlVersion
        {
            get => (string)GetValue(OpenGlVersionProperty);
            set => SetValue(OpenGlVersionProperty, value);
        }

        private volatile float _frameRateLimit = (float)FrameRateLimitProperty.DefaultMetadata.DefaultValue;
        public static readonly DependencyProperty FrameRateLimitProperty = DependencyProperty.Register(
            nameof(FrameRateLimit), typeof(float), typeof(OpenTkControlBase), new PropertyMetadata(float.PositiveInfinity));

        /// <summary>
        /// The maximum frame rate to render at. Anything over 1000 is treated as unlimited.
        /// </summary>
        public float FrameRateLimit
        {
            get => (float)GetValue(FrameRateLimitProperty);
            set => SetValue(FrameRateLimitProperty, value);
        }

        private volatile float _pixelScale = (float)PixelScaleProperty.DefaultMetadata.DefaultValue;
        public static readonly DependencyProperty PixelScaleProperty = DependencyProperty.Register(
            nameof(PixelScale), typeof(float), typeof(OpenTkControlBase), new PropertyMetadata(1f));

        /// <summary>
        /// Scales the pixel size to change the number of pixels rendered. Mainly useful for improving performance.
        /// A scale greater than 1 means that pixels will be bigger and the resolution will decrease.
        /// </summary>
        public float PixelScale
        {
            get => (float)GetValue(PixelScaleProperty);
            set => SetValue(PixelScaleProperty, value);
        }

        private volatile uint _maxPixels = (uint)MaxPixelsProperty.DefaultMetadata.DefaultValue;
        public static readonly DependencyProperty MaxPixelsProperty = DependencyProperty.Register(
            nameof(MaxPixels), typeof(uint), typeof(OpenTkControlBase), new PropertyMetadata(uint.MaxValue));

        /// <summary>
        /// Sets the maximum number of pixels to draw. If the control size is larger than this, the scale will
        /// be changed as necessary to stay under this limit.
        /// </summary>
        public uint MaxPixels
        {
            get => (uint)GetValue(MaxPixelsProperty);
            set => SetValue(MaxPixelsProperty, value);
        }

        protected volatile bool _continuous = (bool)ContinuousProperty.DefaultMetadata.DefaultValue;
        public static readonly DependencyProperty ContinuousProperty = DependencyProperty.Register(
                nameof(Continuous), typeof(bool), typeof(UiOpenTkControl), new PropertyMetadata(true));

        /// <summary>
        /// Determines whether this control is in continuous mode. If set to false, RequestRepaint must be called
        /// to get the control to render. Otherwise, it will automatically Render as fast as it possible up
        /// to the <see cref="FrameRateLimit"/>
        /// </summary>
        public bool Continuous
        {
            get => (bool)GetValue(ContinuousProperty);
            set => SetValue(ContinuousProperty, value);
        }

        /// <summary>
        /// The event arguments that are sent when a <see cref="GlRender"/> event occurs
        /// </summary>
        public class GlRenderEventArgs : EventArgs
        {
            /// <summary>
            /// True if the width or height has change since the previous render event. Always false for screenshots
            /// </summary>
            public bool Resized { get; }

            /// <summary>
            /// True if this render will be saved to a screenshot
            /// </summary>
            public bool Screenshot { get; }

            /// <summary>
            /// If set, the OpenGL context has been recreated and any existing OpenGL objects will be invalid.
            /// </summary>
            public bool NewContext { get; }

            /// <summary>
            /// The width of the drawing area in pixels
            /// </summary>
            public int Width { get; }

            /// <summary>
            /// The height of the drawing area in pixels
            /// </summary>
            public int Height { get; }

            /// <summary>
            /// Can be set to only redraw a certain part of the canvas. Not used for screenshots
            /// </summary>
            public Int32Rect RepaintRect { get; set; }

            /// <summary>
            /// Creates a <see cref="GlRenderEventArgs"/>
            /// </summary>
            /// <param name="width"><see cref="Width"/></param>
            /// <param name="height"><see cref="Height"/></param>
            /// <param name="resized"><see cref="Resized"/></param>
            /// <param name="screenshot"><see cref="Screenshot"/></param>
            public GlRenderEventArgs(int width, int height, bool resized, bool screenshot, bool newContext)
            {
                Width = width;
                Height = height;
                RepaintRect = new Int32Rect(0, 0, Width, Height);
                Resized = resized;
                Screenshot = screenshot;
                NewContext = newContext;
            }
        }

        /// <summary>
        /// Called whenever another render should occur
        /// </summary>
        public event EventHandler<GlRenderEventArgs> GlRender;

        /// <summary>
        /// Called whenever an exception occurs during initialization, rendering or deinitialization
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// An OpenTK graphics context
        /// </summary>
        private IGraphicsContext _context;

        /// <summary>
        /// The source of the internal Image
        /// </summary>
        private volatile WriteableBitmap _bitmap;

        /// <summary>
        /// The width of <see cref="_bitmap"/> in pixels/>
        /// </summary>
        private int _bitmapWidth;

        /// <summary>
        /// The height of <see cref="_bitmap"/> in pixels/>
        /// </summary>
        private int _bitmapHeight;

        /// <summary>
        /// A pointer to <see cref="_bitmap"/>'s back buffer
        /// </summary>
        private IntPtr _backBuffer = IntPtr.Zero;

        /// <summary>
        /// Information about the current window
        /// </summary>
        private IWindowInfo _windowInfo;

        /// <summary>
        /// A Task that represents updating the screen with the current WriteableBitmap back buffer
        /// </summary>
        private Task _previousUpdateImageTask;

        /// <summary>
        /// Stores any pending screenshots that need to be captured
        /// </summary>
        private readonly ConcurrentQueue<Tuple<TaskCompletionSource<uint[,]>, int, int>> _screenshotQueue =
            new ConcurrentQueue<Tuple<TaskCompletionSource<uint[,]>, int, int>>();

        /// <summary>
        /// True if a new OpenGL context has been created since the last render call
        /// </summary>
        private bool _newContext;

        /// <summary>
        /// Keeps track of any pending repaint requests that need to be notified upon completion
        /// </summary>
        private readonly ConcurrentQueue<TaskCompletionSource<object>> _repaintRequestQueue =
            new ConcurrentQueue<TaskCompletionSource<object>>();

        /// <summary>
        /// Set whenever a repaint is requested
        /// </summary>
        protected readonly ManualResetEvent ManualRepaintEvent = new ManualResetEvent(false);

        /// <summary>
        /// The last time a frame was rendered
        /// </summary>
        private DateTime _lastFrameTime = DateTime.MinValue;

        /// <summary>
        /// The OpenGL framebuffer
        /// </summary>
        private int _frameBuffer;

        /// <summary>
        /// The OpenGL render buffer. It stores data in Rgba8 format with color attachment 0
        /// </summary>
        private int _renderBuffer;

        /// <summary>
        /// The OpenGL depth buffer
        /// </summary>
        private int _depthBuffer;

        /// <summary>
        /// True if OnLoaded has already been called
        /// </summary>
        private bool _alreadyLoaded;

        /// <summary>
        /// Creates the <see cref="OpenTkControlBase"/>/>
        /// </summary>
        protected OpenTkControlBase()
        {
            InitializeComponent();

            // Update all of the volatile copies the variables
            // This is a workaround for the WPF threading restrictions on DependencyProperties
            // that allows other threads to read the values.
            DependencyPropertyDescriptor.FromProperty(OpenGlVersionProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) => _openGlVersion = OpenGlVersion);
            DependencyPropertyDescriptor.FromProperty(FrameRateLimitProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) => _frameRateLimit = FrameRateLimit);
            DependencyPropertyDescriptor.FromProperty(PixelScaleProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) => _pixelScale = PixelScale);
            DependencyPropertyDescriptor.FromProperty(MaxPixelsProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) => _maxPixels = MaxPixels);
            DependencyPropertyDescriptor.FromProperty(ContinuousProperty, typeof(OpenTkControlBase))
                .AddValueChanged(this, (sender, args) =>
                {
                    _continuous = Continuous;
                    // Handle the case where we switched to continuous, but the thread is still waiting for a request
                    if (_continuous)
                        RequestRepaint();
                });

            Loaded += (sender, args) =>
            {
                if (_alreadyLoaded)
                    return;

                _alreadyLoaded = true;
                OnLoaded(sender, args);
            };
            Unloaded += (sender, args) =>
            {
                if (!_alreadyLoaded)
                    return;

                _alreadyLoaded = false;
                OnUnloaded(sender, args);
            };
        }

        /// <summary>
        /// Requests that the next frame be drawn, which is the only way to get the control to render 
        /// when not in <see cref="Continuous"/> mode.
        /// In continuous mode this still returns a task that will complete when a frame has rendered.
        /// </summary>
        /// <returns>A task that will complete when the next render completes</returns>
        public Task RequestRepaint()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            _repaintRequestQueue.Enqueue(tcs);
            ManualRepaintEvent.Set();
            return tcs.Task;
        }

        /// <summary>
        /// Renders a screenshot of the frame with the specified dimensions. It will be in bgra format with
        /// [0,0] at the bottom left corner. Note that this is not meant for taking screenshots of what is
        /// displayed on the screen each frame. To do that, just use GL.ReadPixels.
        /// </summary>
        /// <param name="width">The width of the screenshot in pixels or 0 to use the current width</param>
        /// <param name="height">The height of the screenshot in pixels or 0 to use the current height</param>
        /// <returns>A task that completes when the screenshot is ready</returns>
        public Task<uint[,]> Screenshot(int width = 0, int height = 0)
        {
            TaskCompletionSource<uint[,]> tcs = new TaskCompletionSource<uint[,]>();
            _screenshotQueue.Enqueue(new Tuple<TaskCompletionSource<uint[,]>, int, int>(tcs, width, height));
            return tcs.Task;
        }

        /// <summary>
        /// Executes an action on the UI thread
        /// </summary>
        /// <param name="action">The action to run</param>
        /// <returns>a Task that will complete when the action finishes running or null if already complete</returns>
        public abstract Task RunOnUiThread(Action action);

        /// <summary>
        /// Called when this control is loaded
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">Information about the event</param>
        protected virtual void OnLoaded(object sender, RoutedEventArgs args)
        {
            _windowInfo = Utilities.CreateWindowsWindowInfo(
                new WindowInteropHelper(Window.GetWindow(this)).Handle);
        }

        /// <summary>
        /// Called when this control is unloaded
        /// </summary>
        /// <param name="sender">The object that sent the event</param>
        /// <param name="args">Information about the event</param>
        protected virtual async void OnUnloaded(object sender, RoutedEventArgs args)
        {
            try
            {
                Task previousUpdateImageTask = _previousUpdateImageTask;
                if (previousUpdateImageTask != null)
                {
                    await previousUpdateImageTask;
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception e)
            {
                ExceptionOccurred?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }

            _previousUpdateImageTask = null;

            _windowInfo = null;
            _backBuffer = IntPtr.Zero;

            _bitmap = null;

            _lastFrameTime = DateTime.MinValue;
        }

        /// <summary>
        /// Initializes a variety of OpenGL resources
        /// </summary>
        protected void InitOpenGl()
        {
            try
            {
                Version version = Version.Parse(_openGlVersion);
                GraphicsMode mode = new GraphicsMode(DisplayDevice.Default.BitsPerPixel, 16, 0, 4, 0, 2, false);
                _context = new GraphicsContext(mode, _windowInfo, version.Major, version.Minor, GraphicsContextFlags.Default);
                _newContext = true;
                _context.LoadAll();
                _context.MakeCurrent(_windowInfo);
            }
            catch (Exception e)
            {
                ExceptionOccurred?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }
        }

        /// <summary>
        /// Releases any OpenGL resources in use. Must be called from the Render Thread.
        /// </summary>
        protected void DeInitOpenGl()
        {
            try
            {
                DeInitOpenGlBuffers();

                _context.Dispose();
                _context = null;

                while (_screenshotQueue.TryDequeue(out var tuple))
                {
                    tuple.Item1.SetCanceled();
                }
            }
            catch (Exception e)
            {
                ExceptionOccurred?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }
        }

        /// <summary>
        /// Handles generating screenshots and updating the display image
        /// </summary>
        protected TimeSpan Render()
        {
            try
            {
                RenderScreenshots(out int currentBufferWidth, out int currentBufferHeight);

                CalculateBufferSize(out int width, out int height);

                if ((_continuous && !IsVisible) || width == 0 || height == 0)
                    return TimeSpan.FromMilliseconds(20);


                if (_continuous && _frameRateLimit > 0 && _frameRateLimit < 1000)
                {
                    DateTime now = DateTime.Now;
                    TimeSpan delayTime = TimeSpan.FromSeconds(1 / _frameRateLimit) - (now - _lastFrameTime);
                    if (delayTime.CompareTo(TimeSpan.Zero) > 0)
                        return delayTime;

                    _lastFrameTime = now;
                }
                else
                {
                    _lastFrameTime = DateTime.MinValue;
                }

                if (!ReferenceEquals(GraphicsContext.CurrentContext, _context))
                    _context.MakeCurrent(_windowInfo);

                bool resized = false;
                Task resizeBitmapTask = null;
                //Need Abs(...) > 1 to handle an edge case where the resizing the bitmap causes the height to increase in an infinite loop
                if (_bitmap == null || Math.Abs(_bitmapWidth - width) > 1 || Math.Abs(_bitmapHeight - height) > 1)
                {
                    resized = true;
                    _bitmapWidth = width;
                    _bitmapHeight = height;
                    resizeBitmapTask = RunOnUiThread(() =>
                    {
                        _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
                        _backBuffer = _bitmap.BackBuffer;
                    });
                }

                if (currentBufferWidth != _bitmapWidth || currentBufferHeight != _bitmapHeight)
                {
                    CreateOpenGlBuffers(_bitmapWidth, _bitmapHeight);
                }

                List<TaskCompletionSource<object>> repaintRequests = null;
                while (_repaintRequestQueue.TryDequeue(out var tcs))
                {
                    if (repaintRequests == null)
                    {
                        repaintRequests = new List<TaskCompletionSource<object>>();
                    }
                    repaintRequests.Add(tcs);
                }

                GlRenderEventArgs args = new GlRenderEventArgs(_bitmapWidth, _bitmapHeight, resized, false, CheckNewContext());
                try
                {
                    OnGlRender(args);
                }
                finally
                {
                    if (repaintRequests != null)
                    {
                        foreach (var taskCompletionSource in repaintRequests)
                        {
                            taskCompletionSource.SetResult(null);
                        }
                    }
                }

                Int32Rect dirtyArea = args.RepaintRect;

                if (dirtyArea.Width <= 0 || dirtyArea.Height <= 0)
                    return TimeSpan.Zero;

                try
                {
                    resizeBitmapTask?.Wait();
                    try
                    {
                        _previousUpdateImageTask?.Wait();
                    }
                    finally
                    {
                        _previousUpdateImageTask = null;
                    }
                }
                catch (TaskCanceledException)
                {
                    return TimeSpan.Zero;
                }

                if (_backBuffer != IntPtr.Zero)
                    GL.ReadPixels(0, 0, _bitmapWidth, _bitmapHeight, PixelFormat.Bgra, PixelType.UnsignedByte, _backBuffer);

                _previousUpdateImageTask = RunOnUiThread(() => UpdateImage(dirtyArea));
            }
            catch (Exception e)
            {
                ExceptionOccurred?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
            }

            return TimeSpan.Zero;
        }

        /// <summary>
        /// Renders all of the requested screenshots
        /// </summary>
        /// <param name="currentWidth">The new OpenGl buffer width</param>
        /// <param name="currentHeight">The new OpenGl buffer height</param>
        private void RenderScreenshots(out int currentWidth, out int currentHeight)
        {
            currentWidth = _bitmapWidth;
            currentHeight = _bitmapHeight;
            while (_screenshotQueue.TryDequeue(out var screenshotInfo))
            {
                TaskCompletionSource<uint[,]> tcs = screenshotInfo.Item1;
                int screenshotWidth = screenshotInfo.Item2;
                int screenshotHeight = screenshotInfo.Item3;
                if (screenshotWidth <= 0)
                    screenshotWidth = _bitmapWidth;
                if (screenshotHeight <= 0)
                    screenshotHeight = _bitmapHeight;

                try
                {
                    uint[,] screenshot = new uint[screenshotHeight, screenshotWidth];

                    //Handle the case where the window has 0 width or height
                    if (screenshotHeight == 0 || screenshotWidth == 0)
                    {
                        tcs.SetResult(screenshot);
                        continue;
                    }

                    if (screenshotWidth != currentWidth || screenshotHeight != currentHeight)
                    {
                        currentWidth = screenshotWidth;
                        currentHeight = screenshotHeight;
                        CreateOpenGlBuffers(screenshotWidth, screenshotHeight);
                    }
                    OnGlRender(new GlRenderEventArgs(screenshotWidth, screenshotHeight, false, true, CheckNewContext()));
                    GL.ReadPixels(0, 0, screenshotWidth, screenshotHeight,
                        PixelFormat.Bgra, PixelType.UnsignedByte,
                        screenshot);
                    tcs.SetResult(screenshot);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            }
        }

        /// <summary>
        /// Updates <see cref="_newContext"/>
        /// </summary>
        /// <returns>True if there is a new context</returns>
        private bool CheckNewContext()
        {
            if (_newContext)
            {
                _newContext = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines the current buffer size based on the ActualWidth and ActualHeight of the control
        /// in addition to the <see cref="MaxPixels"/> and <see cref="PixelScale"/> settings
        /// </summary>
        /// <param name="width">The new buffer width</param>
        /// <param name="height">The new buffer height</param>
        private void CalculateBufferSize(out int width, out int height)
        {
            width = (int)(ActualWidth / _pixelScale);
            height = (int)(ActualHeight / _pixelScale);

            if (width <= 0 || height <= 0)
                return;

            if (width * height > _maxPixels)
            {
                float scale = (float)Math.Sqrt((float)_maxPixels / width / height);
                width = (int)(width * scale);
                height = (int)(height * scale);
            }
        }

        /// <summary>
        /// Updates what is currently being drawn on the screen from the back buffer.
        /// Must be called from the UI thread
        /// </summary>
        /// <param name="dirtyArea">The dirty dirtyArea of the screen that should be updated</param>
        private void UpdateImage(Int32Rect dirtyArea)
        {
            WriteableBitmap bitmap = _bitmap;
            if (bitmap == null)
            {
                Image.Source = null;
                return;
            }

            bitmap.Lock();
            bitmap.AddDirtyRect(dirtyArea);
            bitmap.Unlock();

            Image.Source = bitmap;
        }

        /// <summary>
        /// Creates new OpenGl buffers of the specified size, including <see cref="_frameBuffer"/>, <see cref="_depthBuffer"/>,
        /// and <see cref="_renderBuffer" />. This method is virtual so the behavior can be overriden, but the default behavior
        /// should work for most purposes.
        /// </summary>
        /// <param name="width">The width of the new buffers</param>
        /// <param name="height">The height of the new buffers</param>
        protected virtual void CreateOpenGlBuffers(int width, int height)
        {
            DeInitOpenGlBuffers();

            _frameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer);

            _depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, _depthBuffer);

            _renderBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _renderBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                RenderbufferTarget.Renderbuffer, _renderBuffer);

            FramebufferErrorCode error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
            {
                throw new GraphicsErrorException("Error creating frame buffer: " + error);
            }
        }

        /// <summary>
        /// Releases all of the OpenGL buffers currently in use
        /// </summary>
        protected virtual void DeInitOpenGlBuffers()
        {
            if (_frameBuffer != 0)
            {
                GL.DeleteFramebuffer(_frameBuffer);
                _frameBuffer = 0;
            }
            if (_depthBuffer != 0)
            {
                GL.DeleteRenderbuffer(_depthBuffer);
                _depthBuffer = 0;
            }
            if (_renderBuffer != 0)
            {
                GL.DeleteRenderbuffer(_renderBuffer);
                _renderBuffer = 0;
            }
        }

        /// <summary>
        /// A helper to actually invoke <see cref="GlRender"/>
        /// </summary>
        /// <param name="args">The render arguments</param>
        private void OnGlRender(GlRenderEventArgs args)
        {
            GlRender?.Invoke(this, args);

            ErrorCode error = GL.GetError();
            if (error != ErrorCode.NoError)
                throw new GraphicsException(error.ToString());
        }
    }
}
