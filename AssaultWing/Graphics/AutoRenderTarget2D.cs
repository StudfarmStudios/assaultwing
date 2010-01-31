using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Graphics
{
    /// <summary>
    /// A <see cref="RenderTarget2D"/> that automatically reallocates the target
    /// when its parameters change, such as the required width and height.
    /// Doesn't support the use of stencil.
    /// </summary>
    class AutoRenderTarget2D : IDisposable
    {
        public struct CreationData
        {
            public int Width;
            public int Height;
            public bool DepthStencilEnable;
        }

        GraphicsDevice _graphicsDevice;
        RenderTarget2D _target;
        DepthStencilBuffer _depthStencilBuffer;
        Func<CreationData> _getCreationData;
        CreationData _oldCreationData;

        public AutoRenderTarget2D(GraphicsDevice graphicsDevice, Func<CreationData> getCreationData)
        {
            _graphicsDevice = graphicsDevice;
            _getCreationData = getCreationData;
        }

        public Texture2D GetTexture()
        {
            return _target.GetTexture();
        }

        public void SetAsRenderTarget(int renderTargetIndex)
        {
            var data = _getCreationData();
            if (_target == null || !data.Equals(_oldCreationData))
            {
                _oldCreationData = data;
                if (_target != null) _target.Dispose();
                if (_depthStencilBuffer != null) _depthStencilBuffer.Dispose();
                _target = new RenderTarget2D(_graphicsDevice, data.Width, data.Height, 1, SurfaceFormat.Color);
                if (data.DepthStencilEnable) _depthStencilBuffer = new DepthStencilBuffer(_graphicsDevice, data.Width, data.Height, DepthFormat.Depth24);
            }
            _graphicsDevice.SetRenderTarget(renderTargetIndex, _target);
            _graphicsDevice.DepthStencilBuffer = _depthStencilBuffer;
            _graphicsDevice.RenderState.DepthBufferEnable = data.DepthStencilEnable;
            _graphicsDevice.RenderState.StencilEnable = data.DepthStencilEnable;
            var clearOptions = data.DepthStencilEnable
                ? ClearOptions.Target | ClearOptions.DepthBuffer
                : ClearOptions.Target;
            _graphicsDevice.Clear(clearOptions, Color.Black, 1, 0);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_target != null && !_target.IsDisposed)
            {
                _target.Dispose();
                _target = null;
            }
        }

        #endregion
    }
}
