using System;
using System.Dynamic;
using System.Reflection;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;

namespace AW2.Core
{
    public static class Direct3D
    {
        private static Assembly g_mdxAssembly;

        private class MDXPresentParameters : DynamicObject
        {
            public object WrappedValue { get; private set; }

            public MDXPresentParameters(object wrappedPresentParameters)
            {
                WrappedValue = wrappedPresentParameters;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                var memberType = GetMDXType("PresentParameters").GetProperty(binder.Name);
                memberType.SetValue(WrappedValue, value, null);
                return true;
            }
        }

        static Direct3D()
        {
            try
            {
                g_mdxAssembly = Assembly.Load("Microsoft.DirectX.Direct3D, Version=1.0.2902.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            }
            catch (FileNotFoundException)
            {
                // Managed DirectX not found. Situation is okay so far. We will throw an exception
                // if an actual attempt is made to use Managed DirectX.
            }
        }

        public static unsafe void Reset(GraphicsDevice graphicsDevice, PresentationParameters parameters)
        {
            var fi = typeof(GraphicsDevice).GetField("pComPtr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ptr = fi.GetValue(graphicsDevice);
            var pComPtr = new IntPtr(System.Reflection.Pointer.Unbox(ptr));
            if (g_mdxAssembly == null) throw new ApplicationException("GraphicsDevice.Reset failed. Please install Managed DirectX from the Assault Wing web site.");
            var mdxDeviceType = g_mdxAssembly.GetType("Microsoft.DirectX.Direct3D.Device");
            var mdxPresentParametersType = g_mdxAssembly.GetType("Microsoft.DirectX.Direct3D.PresentParameters");
            var dev = Activator.CreateInstance(mdxDeviceType, pComPtr);
            dynamic dxParameters = new MDXPresentParameters(Activator.CreateInstance(mdxPresentParametersType));
            dxParameters.AutoDepthStencilFormat = parameters.DepthStencilFormat.ToD3D();
            dxParameters.BackBufferCount = 1;
            dxParameters.BackBufferFormat = parameters.BackBufferFormat.ToD3D();
            dxParameters.BackBufferHeight = parameters.BackBufferHeight;
            dxParameters.BackBufferWidth = parameters.BackBufferWidth;
            dxParameters.DeviceWindow = null;
            dxParameters.DeviceWindowHandle = parameters.DeviceWindowHandle;
            dxParameters.EnableAutoDepthStencil = false; // ???
            dxParameters.ForceNoMultiThreadedFlag = false; // ???
            dxParameters.FullScreenRefreshRateInHz = 0; // ??? should be 0 for windowed mode; in fullscreen mode take value from DisplayModeCollection
            dxParameters.MultiSample = GetMDXEnumValue("MultiSampleType", "None");
            dxParameters.MultiSampleQuality = 0;
            dxParameters.PresentationInterval = parameters.PresentationInterval.ToD3D();
            dxParameters.PresentFlag = GetMDXEnumValue("PresentFlag", "None"); // ???
            dxParameters.SwapEffect = GetMDXEnumValue("SwapEffect", "Flip"); // ??? see _parameters.RenderTargetUsage
            dxParameters.Windowed = !parameters.IsFullScreen;
            var resetMethod = mdxDeviceType.GetMethod("Reset");
            var mdxPresentParametersArray = Array.CreateInstance(mdxPresentParametersType, 1);
            mdxPresentParametersArray.SetValue(((MDXPresentParameters)dxParameters).WrappedValue, 0);
            resetMethod.Invoke(dev, new[] { mdxPresentParametersArray });
        }

        private static Type GetMDXType(string typeName)
        {
            return g_mdxAssembly.GetType("Microsoft.DirectX.Direct3D." + typeName);
        }

        private static object GetMDXEnumValue(string enumTypeName, string valueName)
        {
            return GetMDXType(enumTypeName).GetField(valueName).GetValue(null);
        }

        private static dynamic ToD3D(this DepthFormat depthFormat)
        {
            switch (depthFormat)
            {
                case DepthFormat.None: return GetMDXEnumValue("DepthFormat", "Unknown");
                case DepthFormat.Depth16: return GetMDXEnumValue("DepthFormat", "D16");
                case DepthFormat.Depth24: return GetMDXEnumValue("DepthFormat", "D24X8");
                case DepthFormat.Depth24Stencil8: return GetMDXEnumValue("DepthFormat", "D24S8");
                default: throw new ArgumentException(depthFormat.ToString(), "depthFormat");
            }
        }

        private static dynamic ToD3D(this SurfaceFormat surfaceFormat)
        {
            switch (surfaceFormat)
            {
                case SurfaceFormat.Color: return GetMDXEnumValue("Format", "A8R8G8B8");
                default: throw new ArgumentException(surfaceFormat.ToString(), "surfaceFormat");
            }
        }

        private static dynamic ToD3D(this PresentInterval presentInterval)
        {
            switch (presentInterval)
            {
                case PresentInterval.Default: return GetMDXEnumValue("PresentInterval", "Default");
                case PresentInterval.Immediate: return GetMDXEnumValue("PresentInterval", "Immediate");
                case PresentInterval.One: return GetMDXEnumValue("PresentInterval", "One");
                case PresentInterval.Two: return GetMDXEnumValue("PresentInterval", "Two");
                default: throw new ArgumentException(presentInterval.ToString(), "presentInterval");
            }
        }
    }
}
