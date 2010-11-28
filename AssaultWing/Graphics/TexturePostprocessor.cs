using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;

namespace AW2.Graphics
{
    /// <summary>
    /// Works with <see cref="Effect"/> instances that have the following parameters:
    /// "Texture" takes the input texture to process,
    /// "TextureWidth" takes the input texture width in texels,
    /// "TextureHeight" takes the input texture height in texels.
    /// </summary>
    public class TexturePostprocessor : IDisposable
    {
        private Effect _basicShaders;
        private AutoRenderTarget2D[] _targets;
        private int _sourceIndex, _targetIndex;
        private GraphicsDevice _gfx;
        private VertexPositionTexture[] _vertexData;
        private Viewport _oldViewport;
        private List<Effect> _effects;
        private Action<ICollection<Effect>> _effectContainerUpdater;

        public TexturePostprocessor(GraphicsDevice gfx, Action<ICollection<Effect>> effectContainerUpdater)
        {
            _basicShaders = AssaultWingCore.Instance.Content.Load<Effect>("basicshaders");
            _gfx = gfx;
            Func<AutoRenderTarget2D.CreationData> getRenderTargetCreationData = () => new AutoRenderTarget2D.CreationData
            {
                Width = _gfx.Viewport.Width,
                Height = _gfx.Viewport.Height,
                DepthStencilState = DepthStencilState.Default,
            };
            _targets = new AutoRenderTarget2D[] {
                new AutoRenderTarget2D(gfx, getRenderTargetCreationData),
                new AutoRenderTarget2D(gfx, getRenderTargetCreationData)
            };
            _effects = new List<Effect>();
            _effectContainerUpdater = effectContainerUpdater;
            _vertexData = new VertexPositionTexture[] {
                new VertexPositionTexture(new Vector3(-1, -1, 0), Vector2.UnitY),
                new VertexPositionTexture(new Vector3(-1, 1, 0), Vector2.Zero),
                new VertexPositionTexture(new Vector3(1, -1, 0), Vector2.One),
                new VertexPositionTexture(new Vector3(1, 1, 0), Vector2.UnitX)
            };
        }

        public void ProcessToScreen(Action render)
        {
            _effectContainerUpdater(_effects);
            if (_effects.Count == 0)
            {
                render();
                return;
            }
            PrepareFirstPass();
            render();
            Process();
            DisplayOnScreen();
        }

        private void DisplayOnScreen()
        {
            PrepareLastPass(_basicShaders);
            _basicShaders.CurrentTechnique.Passes["PixelAndVertexShaderPass"].Apply();
            _gfx.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertexData, 0, 2);
        }

        private void Process()
        {
            _basicShaders.CurrentTechnique.Passes["VertexShaderPass"].Apply();
            for (int effectIndex = 0; effectIndex < _effects.Count; ++effectIndex)
            {
                var effect = _effects[effectIndex];
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    PrepareNextPass(effect);
                    pass.Apply();
                    _gfx.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertexData, 0, 2);
                }
            }
        }

        private void PrepareFirstPass()
        {
            _sourceIndex = -1;
            _targetIndex = 0;
            _oldViewport = _gfx.Viewport;
            _targets[_targetIndex].SetAsRenderTarget();
        }

        /// <summary>
        /// <paramref name="effect"/> must contain the next pass.
        /// </summary>
        private void PrepareNextPass(Effect effect)
        {
            _sourceIndex = _targetIndex;
            _targetIndex = (_targetIndex + 1) % _targets.Length;
            _targets[_targetIndex].SetAsRenderTarget();
            PrepareEffect(effect);
        }

        private void PrepareLastPass(Effect effect)
        {
            _sourceIndex = _targetIndex;
            _targetIndex = (_targetIndex + 1) % _targets.Length;
            _gfx.SetRenderTarget(null);
            _gfx.DepthStencilState = DepthStencilState.None;
            _gfx.BlendState = BlendState.Opaque;
            _gfx.Viewport = _oldViewport;
            _gfx.Clear(Color.Black);
            PrepareEffect(effect);
        }

        private void PrepareEffect(Effect effect)
        {
            var tex = _targets[_sourceIndex].GetTexture();
            effect.Parameters["T"].SetValue((float)AssaultWingCore.Instance.DataEngine.ArenaTotalTime.TotalSeconds);
            effect.Parameters["Texture"].SetValue(tex);
            effect.Parameters["TextureWidth"].SetValue(tex.Width);
            effect.Parameters["TextureHeight"].SetValue(tex.Height);
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
        }

        #endregion
    }
}
