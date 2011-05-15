using System;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Core
{
    public static class Direct3D
    {
        public static unsafe void Reset(GraphicsDevice graphicsDevice, PresentationParameters parameters)
        {
            var fi = typeof(GraphicsDevice).GetField("pComPtr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ptr = fi.GetValue(graphicsDevice);
            var pComPtr = new IntPtr(System.Reflection.Pointer.Unbox(ptr));
            var dev = new Microsoft.DirectX.Direct3D.Device(pComPtr);
            var dxParameters = new Microsoft.DirectX.Direct3D.PresentParameters
            {
                AutoDepthStencilFormat = parameters.DepthStencilFormat.ToD3D(),
                BackBufferCount = 1,
                BackBufferFormat = parameters.BackBufferFormat.ToD3D(),
                BackBufferHeight = parameters.BackBufferHeight,
                BackBufferWidth = parameters.BackBufferWidth,
                DeviceWindow = null,
                DeviceWindowHandle = parameters.DeviceWindowHandle,
                EnableAutoDepthStencil = false, // !!! ???
                ForceNoMultiThreadedFlag = false, // !!! ???
                FullScreenRefreshRateInHz = 0, // !!! should be 0 for windowed mode; in fullscreen mode take value from DisplayModeCollection
                MultiSample = Microsoft.DirectX.Direct3D.MultiSampleType.None,
                MultiSampleQuality = 0,
                PresentationInterval = parameters.PresentationInterval.ToD3D(),
                PresentFlag = Microsoft.DirectX.Direct3D.PresentFlag.None, // !!! ???
                SwapEffect = Microsoft.DirectX.Direct3D.SwapEffect.Flip, // !!! ??? see _parameters.RenderTargetUsage
                Windowed = !parameters.IsFullScreen,
            };
            dev.Reset(new[] { dxParameters });
        }

        public static Microsoft.DirectX.Direct3D.DepthFormat ToD3D(this DepthFormat depthFormat)
        {
            switch (depthFormat)
            {
                case DepthFormat.None: return Microsoft.DirectX.Direct3D.DepthFormat.Unknown;
                case DepthFormat.Depth16: return Microsoft.DirectX.Direct3D.DepthFormat.D16;
                case DepthFormat.Depth24: return Microsoft.DirectX.Direct3D.DepthFormat.D24X8;
                case DepthFormat.Depth24Stencil8: return Microsoft.DirectX.Direct3D.DepthFormat.D24S8;
                default: throw new ArgumentException(depthFormat.ToString(), "depthFormat");
            }
        }

        public static Microsoft.DirectX.Direct3D.Format ToD3D(this SurfaceFormat surfaceFormat)
        {
            switch (surfaceFormat)
            {
                case SurfaceFormat.Color: return Microsoft.DirectX.Direct3D.Format.A8R8G8B8;
                default: throw new ArgumentException(surfaceFormat.ToString(), "surfaceFormat");
            }
        }

        public static Microsoft.DirectX.Direct3D.PresentInterval ToD3D(this PresentInterval presentInterval)
        {
            switch (presentInterval)
            {
                case PresentInterval.Default: return Microsoft.DirectX.Direct3D.PresentInterval.Default;
                case PresentInterval.Immediate: return Microsoft.DirectX.Direct3D.PresentInterval.Immediate;
                case PresentInterval.One: return Microsoft.DirectX.Direct3D.PresentInterval.One;
                case PresentInterval.Two: return Microsoft.DirectX.Direct3D.PresentInterval.Two;
                default: throw new ArgumentException(presentInterval.ToString(), "presentInterval");
            }
        }
    }
}
