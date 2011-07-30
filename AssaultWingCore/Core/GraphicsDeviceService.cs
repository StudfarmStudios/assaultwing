//-----------------------------------------------------------------------------
// GraphicsDeviceService.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using Color = System.Drawing.Color;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace AW2.Core
{
    /// <summary>
    /// Helper class responsible for creating and managing the GraphicsDevice.
    /// All GraphicsDeviceControl instances share the same GraphicsDeviceService,
    /// so even though there can be many controls, there will only ever be a single
    /// underlying GraphicsDevice. This implements the standard IGraphicsDeviceService
    /// interface, which provides notification events for when the device is reset
    /// or disposed.
    /// </summary>
    public class GraphicsDeviceService : IGraphicsDeviceService, IDisposable
    {
        private PresentationParameters _parameters;
        private int _graphicsThreadID;
        private int _graphicsCodeBlocks;
        private int _graphicsCodeBlocksThreadID;

        // IGraphicsDeviceService events.
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        public GraphicsDevice GraphicsDevice { get; private set; }
        public bool IsVerticalSynced { get { return _parameters.PresentationInterval != PresentInterval.Immediate; } }

        /// <summary>
        /// If we do not have a valid graphics device (for instance if the device
        /// is lost, or if we are running inside the Form designer), we must use
        /// regular System.Drawing method to display a status message.
        /// </summary>
        public static void PaintUsingSystemDrawing(System.Drawing.Graphics graphics, Font font, RectangleF rectangle, string text)
        {
            graphics.Clear(Color.CornflowerBlue);
            using (var brush = new SolidBrush(Color.Black))
            {
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    graphics.DrawString(text, font, brush, rectangle, format);
                }
            }
        }

        public GraphicsDeviceService(IntPtr windowHandle)
        {
            var screenBounds = System.Windows.Forms.Screen.FromHandle(windowHandle).Bounds;
            _parameters = new PresentationParameters
            {
                BackBufferWidth = screenBounds.Width,
                BackBufferHeight = screenBounds.Height,
                BackBufferFormat = SurfaceFormat.Color,
                DepthStencilFormat = DepthFormat.Depth24,
                DeviceWindowHandle = windowHandle,
                IsFullScreen = false,
                PresentationInterval = PresentInterval.Immediate,
            };

            var profilingAdapter = GraphicsAdapter.Adapters.FirstOrDefault(a => a.Description.Contains("PerfHUD"));
            var useAdapter = profilingAdapter ?? GraphicsAdapter.DefaultAdapter;
            if (profilingAdapter != null) GraphicsAdapter.UseReferenceDevice = true;

            _graphicsThreadID = Thread.CurrentThread.ManagedThreadId;
            if (!useAdapter.IsProfileSupported(GraphicsProfile.Reach))
                GraphicsAdapter.UseReferenceDevice = !GraphicsAdapter.UseReferenceDevice;
            if (!useAdapter.IsProfileSupported(GraphicsProfile.Reach))
                throw new NotSupportedException("No suitable graphics adapter found");
            try
            {
                GraphicsDevice = new GraphicsDevice(useAdapter, GraphicsProfile.Reach, _parameters);
            }
            catch (InvalidOperationException)
            {
                // With VMware, GraphicsDevice.ctor may throw InvalidOperationException when using reference device.
                GraphicsAdapter.UseReferenceDevice = false;
                GraphicsDevice = new GraphicsDevice(useAdapter, GraphicsProfile.Reach, _parameters);
            }
            if (DeviceCreated != null) DeviceCreated(this, EventArgs.Empty);
        }

        public void CheckThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _graphicsThreadID)
                throw new ApplicationException("Wrong thread for graphics");
        }

        public void CheckReentrancyBegin()
        {
            if (_graphicsCodeBlocks > 0 && Thread.CurrentThread.ManagedThreadId != _graphicsCodeBlocksThreadID)
                throw new ApplicationException("Two threads try to run graphics code");
            _graphicsCodeBlocks++;
            _graphicsCodeBlocksThreadID = Thread.CurrentThread.ManagedThreadId;
        }

        public void CheckReentrancyEnd()
        {
            if (_graphicsCodeBlocks == 0) throw new InvalidOperationException("Reentrancy check end without begin");
            _graphicsCodeBlocks--;
            if (_graphicsCodeBlocks == 0) _graphicsCodeBlocksThreadID = 0;
        }

        public void Dispose()
        {
            CheckReentrancyBegin();
            CheckThread();
            if (DeviceDisposing != null) DeviceDisposing(this, EventArgs.Empty);
            GraphicsDevice.Dispose();
            GraphicsDevice = null;
            CheckReentrancyEnd();
        }

        public void SetFullScreen(int width, int height)
        {
            CheckReentrancyBegin();
            _parameters.BackBufferWidth = width;
            _parameters.BackBufferHeight = height;
            _parameters.IsFullScreen = true;
            ResetDevice();
            CheckReentrancyEnd();
        }

        public void SetWindowed()
        {
            CheckReentrancyBegin();
            _parameters.IsFullScreen = false;
            ResetDevice();
            CheckReentrancyEnd();
        }

        public void EnableVerticalSync()
        {
            CheckReentrancyBegin();
            _parameters.PresentationInterval = PresentInterval.One;
            ResetDevice();
            CheckReentrancyEnd();
        }

        public void DisableVerticalSync()
        {
            CheckReentrancyBegin();
            _parameters.PresentationInterval = PresentInterval.Immediate;
            ResetDevice();
            CheckReentrancyEnd();
        }

        /// <summary>
        /// Attempts to begin drawing the control. Returns an error message string
        /// if this was not possible, which can happen if the graphics device is
        /// lost, or if we are running inside the Form designer.
        /// </summary>
        public string BeginDraw(Size clientSize, bool isFullscreen)
        {
            CheckThread();
            var deviceResetError = HandleDeviceReset(clientSize, isFullscreen);
            if (deviceResetError == null)
                GraphicsDevice.Viewport = new Viewport
                {
                    X = 0,
                    Y = 0,
                    Width = clientSize.Width,
                    Height = clientSize.Height,
                    MinDepth = 0,
                    MaxDepth = 1,
                };
            return deviceResetError;
        }

        /// <summary>
        /// Ends drawing the control. This is called after derived classes
        /// have finished their Draw method, and is responsible for presenting
        /// the finished image onto the screen, using the appropriate WinForms
        /// control handle to make sure it shows up in the right place.
        /// </summary>
        public void EndDraw(Size clientSize, IntPtr handle)
        {
            try
            {
                var sourceRectangle = _parameters.IsFullScreen
                    ? (XnaRectangle?)null
                    : new XnaRectangle(0, 0, clientSize.Width, clientSize.Height);
                GraphicsDevice.Present(sourceRectangle, null, handle);
            }
            catch (DeviceLostException)
            {
                // Present() might throw if the device became lost while we were
                // drawing. The lost device will be handled by the next BeginDraw,
                // so we just swallow the exception.
            }
        }

        /// <summary>
        /// May throw <see cref="Microsoft.Xna.Framework.Graphics.DeviceLostException"/> in which case
        /// this reset failed but a future reset may succeed. Other exceptions are not recoverable,
        /// such as <see cref="System.InvalidOperationException"/>.
        /// Returns null on success, or a message on a recoverable error.
        /// </summary>
        private string ResetDevice()
        {
            try
            {
                CheckThread();
                if (DeviceResetting != null) DeviceResetting(this, EventArgs.Empty);
                try
                {
                    GraphicsDevice.Reset(_parameters);
                }
                catch (InvalidOperationException)
                {
                    // GraphicsDevice.Reset() seems to only call GraphicsDeviceResetEx().
                    // This sometimes results in InvalidOperationException. DirectX debug runtime
                    // says that only GraphicsDeviceReset() can be called. We call it using
                    // Managed DirectX because XNA doesn't offer such a possibility.
                    Direct3D.Reset(GraphicsDevice, _parameters);
                    GraphicsDevice.Reset(_parameters);
                }
                if (DeviceReset != null) DeviceReset(this, EventArgs.Empty);
                return null;
            }
            catch (DeviceLostException)
            {
                Log.Write("Note: Graphics device lost during reset");
                return "Graphics device lost during reset";
            }
        }

        /// <summary>
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost. Returns an error string if the device
        /// could not be reset.
        /// </summary>
        private string HandleDeviceReset(Size clientSize, bool isFullscreen)
        {
            bool deviceNeedsReset = false;
            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // If the graphics device is lost, we cannot use it at all.
                    return "Graphics device lost";
                case GraphicsDeviceStatus.NotReset:
                    // If device is in the not-reset state, we should try to reset it.
                    deviceNeedsReset = true;
                    break;
                default:
                    var pp = GraphicsDevice.PresentationParameters;
                    deviceNeedsReset = pp.IsFullScreen
                        ? clientSize.Width != pp.BackBufferWidth || clientSize.Height != pp.BackBufferHeight
                        : clientSize.Width > pp.BackBufferWidth || clientSize.Height > pp.BackBufferHeight;
                    break;
            }
            if (deviceNeedsReset) return EnsureBackBufferSize(clientSize.Width, clientSize.Height);
            return null;
        }

        /// <summary>
        /// Returns null on success, or a message on a recoverable error.
        /// </summary>
        private string EnsureBackBufferSize(int width, int height)
        {
            _parameters.BackBufferWidth = Math.Max(_parameters.BackBufferWidth, width);
            _parameters.BackBufferHeight = Math.Max(_parameters.BackBufferHeight, height);
            return ResetDevice();
        }
    }
}
