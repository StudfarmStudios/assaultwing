//-----------------------------------------------------------------------------
// GraphicsDeviceService.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Drawing;
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

        // IGraphicsDeviceService events.
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        public GraphicsDevice GraphicsDevice { get; private set; }

        public GraphicsDeviceService(IntPtr windowHandle)
        {
            SetWindow(windowHandle);
        }

        public void Dispose()
        {
            if (DeviceDisposing != null) DeviceDisposing(this, EventArgs.Empty);
            GraphicsDevice.Dispose();
            GraphicsDevice = null;
        }

        /// <summary>
        /// Resets the graphics device to whichever is bigger out of the specified
        /// resolution or its current size. This behavior means the device will
        /// demand-grow to the largest of all its GraphicsDeviceControl clients.
        /// </summary>
        public void ResetDevice(int width, int height, bool isFullscreen)
        {
            if (DeviceResetting != null) DeviceResetting(this, EventArgs.Empty);
            _parameters.BackBufferWidth = isFullscreen ? width : Math.Max(_parameters.BackBufferWidth, width);
            _parameters.BackBufferHeight = isFullscreen ? height : Math.Max(_parameters.BackBufferHeight, height);
            _parameters.IsFullScreen = isFullscreen;
            GraphicsDevice.Reset(_parameters);
            if (DeviceReset != null) DeviceReset(this, EventArgs.Empty);
        }

        /// <summary>
        /// Attempts to begin drawing the control. Returns an error message string
        /// if this was not possible, which can happen if the graphics device is
        /// lost, or if we are running inside the Form designer.
        /// </summary>
        public string BeginDraw(Size clientSize, bool isFullscreen)
        {
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
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost. Returns an error string if the device
        /// could not be reset.
        /// </summary>
        public string HandleDeviceReset(Size clientSize, bool isFullscreen)
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
                    // If the device state is ok, check whether it is big enough.
                    var pp = GraphicsDevice.PresentationParameters;
                    deviceNeedsReset = clientSize.Width > pp.BackBufferWidth ||
                                       clientSize.Height > pp.BackBufferHeight;
                    break;
            }
            if (deviceNeedsReset)
            {
                try
                {
                    ResetDevice(clientSize.Width, clientSize.Height, isFullscreen);
                }
                catch (Exception e)
                {
                    Log.Write("ERROR: Graphics device reset failed", e);
                    return "Graphics device reset failed\n\n" + e;
                }
            }
            return null;
        }

        /// <summary>
        /// If we do not have a valid graphics device (for instance if the device
        /// is lost, or if we are running inside the Form designer), we must use
        /// regular System.Drawing method to display a status message.
        /// </summary>
        public void PaintUsingSystemDrawing(System.Drawing.Graphics graphics, Font font, RectangleF rectangle, string text)
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

        private void SetWindow(IntPtr windowHandle)
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
            if (GraphicsDevice != null) GraphicsDevice.Dispose();
            GraphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, _parameters);
            if (DeviceCreated != null) DeviceCreated(this, EventArgs.Empty);
        }
    }
}
