//-----------------------------------------------------------------------------
// GraphicsDeviceService.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// The IGraphicsDeviceService interface requires a DeviceCreated event, but we
// always just create the device inside our constructor, so we have no place to
// raise that event. The C# compiler warns us that the event is never used, but
// we don't care so we just disable this warning.
#pragma warning disable 67

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
        public void ResetDevice(int width, int height)
        {
            if (DeviceResetting != null) DeviceResetting(this, EventArgs.Empty);
            _parameters.BackBufferWidth = Math.Max(_parameters.BackBufferWidth, width);
            _parameters.BackBufferHeight = Math.Max(_parameters.BackBufferHeight, height);
            GraphicsDevice.Reset(_parameters);
            if (DeviceReset != null) DeviceReset(this, EventArgs.Empty);
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
        }
    }
}
