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
        private AutoRenderTarget2D[] _targets;
        private int _sourceIndex, _targetIndex;
        private AssaultWingCore _game;
        private VertexPositionTexture[] _vertexData;
        private Viewport _oldViewport;
        private List<Effect> _effects;
        private Action _render;
        private Action<ICollection<Effect>> _effectContainerUpdater;

        private GraphicsDevice Gfx { get { return _game.GraphicsDeviceService.GraphicsDevice; } }
        private Effect BasicShaders { get { return _game.GraphicsEngine.GameContent.BasicShaders; } }

        public TexturePostprocessor(AssaultWingCore game, Action render, Action<ICollection<Effect>> effectContainerUpdater)
        {
            _game = game;
            _render = render;
            Func<AutoRenderTarget2D.CreationData> getRenderTargetCreationData = () => new AutoRenderTarget2D.CreationData
            {
                Width = Gfx.Viewport.Width,
                Height = Gfx.Viewport.Height,
                DepthStencilState = DepthStencilState.Default,
            };
            _targets = new[]
            {
                new AutoRenderTarget2D(Gfx, getRenderTargetCreationData),
                new AutoRenderTarget2D(Gfx, getRenderTargetCreationData)
            };
            _effects = new List<Effect>();
            _effectContainerUpdater = effectContainerUpdater;
            _vertexData = new[]
            {
                new VertexPositionTexture(new Vector3(-1, -1, 0), Vector2.UnitY),
                new VertexPositionTexture(new Vector3(-1, 1, 0), Vector2.Zero),
                new VertexPositionTexture(new Vector3(1, -1, 0), Vector2.One),
                new VertexPositionTexture(new Vector3(1, 1, 0), Vector2.UnitX)
            };
        }

        public void PrepareForDisplay()
        {
            _effectContainerUpdater(_effects);
            if (_effects.Count == 0) return;
            PrepareFirstPass();
            _render();
            Process();
        }

        public void DisplayOnScreen()
        {
            if (_effects.Count == 0)
                _render();
            else
            {
                PrepareLastPass(BasicShaders);
                BasicShaders.CurrentTechnique.Passes["PixelAndVertexShaderPass"].Apply();
                Gfx.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertexData, 0, 2);
            }
        }

        private void Process()
        {
            BasicShaders.CurrentTechnique.Passes["VertexShaderPass"].Apply();
            for (int effectIndex = 0; effectIndex < _effects.Count; ++effectIndex)
            {
                var effect = _effects[effectIndex];
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    PrepareNextPass(effect);
                    pass.Apply();
                    Gfx.DrawUserPrimitives(PrimitiveType.TriangleStrip, _vertexData, 0, 2);
                }
            }
            Gfx.SetRenderTarget(_game.DefaultRenderTarget);
        }

        private void PrepareFirstPass()
        {
            _sourceIndex = -1;
            _targetIndex = 0;
            _oldViewport = Gfx.Viewport;
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
            _targetIndex = -1;
            Gfx.SetRenderTarget(_game.DefaultRenderTarget);
            Gfx.DepthStencilState = DepthStencilState.None;
            Gfx.BlendState = BlendState.Opaque;
            Gfx.Viewport = _oldViewport;
            PrepareEffect(effect);
        }

        private void PrepareEffect(Effect effect)
        {
            var tex = _targets[_sourceIndex].GetTexture();
            if (effect.Parameters["T"]!= null) {
                effect.Parameters["T"].SetValue((float)AssaultWingCore.Instance.DataEngine.ArenaTotalTime.TotalSeconds);
            }

            if (effect.Parameters["Texture"] != null) {
                effect.Parameters["Texture"].SetValue(tex);
            }

            if (effect.Parameters["TextureWidth"] != null) {
                effect.Parameters["TextureWidth"].SetValue(tex.Width);
            }

            if (effect.Parameters["TextureHeight"] != null) {
                effect.Parameters["TextureHeight"].SetValue(tex.Height);
            }

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
