using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Graphics
{
    /// <summary>
    /// Works with <see cref="Effect"/> instances that have a parameter called
    /// "Texture" that takes the input texture to process.
    /// </summary>
    class TexturePostprocessor : IDisposable
    {
        Effect _basicShaders;
        AutoRenderTarget2D[] _targets;
        int _sourceIndex, _targetIndex;
        GraphicsDevice _gfx;
        VertexPositionTexture[] _vertexData;
        VertexDeclaration _vertexDeclaration;
        DepthStencilBuffer _oldDepthStencilBuffer;
        Viewport _oldViewport;

        public List<Effect> Effects { get; private set; }

        public TexturePostprocessor(GraphicsDevice gfx)
        {
            _basicShaders = AssaultWing.Instance.Content.Load<Effect>("basicshaders");
            _gfx = gfx;
            Func<AutoRenderTarget2D.CreationData> getRenderTargetCreationData = () => new AutoRenderTarget2D.CreationData
            {
                Width = _gfx.Viewport.Width,
                Height = _gfx.Viewport.Height,
                DepthStencilEnable = true
            };
            _targets = new AutoRenderTarget2D[] {
                new AutoRenderTarget2D(gfx, getRenderTargetCreationData),
                new AutoRenderTarget2D(gfx, getRenderTargetCreationData)
            };
            Effects = new List<Effect>();
            _vertexData = new VertexPositionTexture[] {
                new VertexPositionTexture(new Vector3(-1, -1, 0), Vector2.UnitY),
                new VertexPositionTexture(new Vector3(-1, 1, 0), Vector2.Zero),
                new VertexPositionTexture(new Vector3(1, -1, 0), Vector2.One),
                new VertexPositionTexture(new Vector3(1, 1, 0), Vector2.UnitX)
            };
            _vertexDeclaration = new VertexDeclaration(gfx, VertexPositionTexture.VertexElements);
        }

        public void ProcessToScreen(Action render)
        {
            PrepareFirstPass();
            render();
            Process();
            DisplayOnScreen();
        }

        private void DisplayOnScreen()
        {
            PrepareLastPass(_basicShaders);
            _basicShaders.Begin();
            _basicShaders.CurrentTechnique.Passes["PixelAndVertexShaderPass"].Begin();
            _gfx.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertexData, 0, 2);
            _basicShaders.CurrentTechnique.Passes["PixelAndVertexShaderPass"].End();
            _basicShaders.End();
        }

        private void Process()
        {
            _gfx.VertexDeclaration = _vertexDeclaration;
            if (Effects.Count == 0) return;

            _basicShaders.Begin();
            _basicShaders.CurrentTechnique.Passes["VertexShaderPass"].Begin();

            for (int effectIndex = 0; effectIndex < Effects.Count; ++effectIndex)
            {
                var effect = Effects[effectIndex];
                effect.Begin();
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    PrepareNextPass(effect);
                    pass.Begin();
                    _gfx.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertexData, 0, 2);
                    pass.End();
                }
                effect.End();
            }

            _basicShaders.CurrentTechnique.Passes["VertexShaderPass"].End();
            _basicShaders.End();
        }

        private void PrepareFirstPass()
        {
            _sourceIndex = -1;
            _targetIndex = 0;
            _oldDepthStencilBuffer = _gfx.DepthStencilBuffer;
            _oldViewport = _gfx.Viewport;
            _targets[_targetIndex].SetAsRenderTarget(0);
            // Given render method might need depth and stencil buffers.
            _gfx.RenderState.DepthBufferEnable = true;
            _gfx.RenderState.StencilEnable = true;
        }

        /// <summary>
        /// <paramref name="effect"/> must contain the next pass.
        /// </summary>
        private void PrepareNextPass(Effect effect)
        {
            _sourceIndex = _targetIndex;
            _targetIndex = (_targetIndex + 1) % _targets.Length;
            _targets[_targetIndex].SetAsRenderTarget(0);
            _gfx.RenderState.DepthBufferEnable = false;
            _gfx.RenderState.StencilEnable = false;
            SetNextTarget(effect);
        }

        private void PrepareLastPass(Effect effect)
        {
            _sourceIndex = _targetIndex;
            _targetIndex = (_targetIndex + 1) % _targets.Length;
            _gfx.DepthStencilBuffer = _oldDepthStencilBuffer;
            _oldDepthStencilBuffer = null;
            _gfx.SetRenderTarget(0, null);
            _gfx.RenderState.DepthBufferEnable = false;
            _gfx.RenderState.StencilEnable = false;
            _gfx.RenderState.AlphaBlendEnable = false;
            _gfx.RenderState.BlendFunction = BlendFunction.Add;
            _gfx.RenderState.SourceBlend = Blend.SourceAlpha;
            _gfx.RenderState.DestinationBlend = Blend.DestinationAlpha;
            _gfx.Viewport = _oldViewport;
            SetNextTarget(effect);
        }

        private void SetNextTarget(Effect effect)
        {
            var tex = _targets[_sourceIndex].GetTexture();
            effect.Parameters["Texture"].SetValue(tex);
            float halfTexelWidth = 0.5f / tex.Width;
            float halfTexelHeight = 0.5f / tex.Height;
            _vertexData[0].Position = new Vector3(-1 - halfTexelWidth, -1 + halfTexelHeight, 0);
            _vertexData[1].Position = new Vector3(-1 - halfTexelWidth, 1 + halfTexelHeight, 0);
            _vertexData[2].Position = new Vector3(1 - halfTexelWidth, -1 + halfTexelHeight, 0);
            _vertexData[3].Position = new Vector3(1 - halfTexelWidth, 1 + halfTexelHeight, 0);
        }

        #region IDisposable Members

        public void Dispose()
        {
            for (int i = 0; i < _targets.Length; ++i)
                _targets[i].Dispose();
            _vertexDeclaration.Dispose();
            _vertexDeclaration = null;
        }

        #endregion
    }
}
