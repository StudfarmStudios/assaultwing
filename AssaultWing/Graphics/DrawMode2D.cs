using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Graphics
{
    /// <summary>
    /// Type of draw mode of 2D graphics.
    /// </summary>
    public enum DrawModeType2D
    {
        /// <summary>
        /// Draw nothing.
        /// </summary>
        None,

        /// <summary>
        /// Blend color with the background. Alpha channel measures
        /// the strength of color, and (1 - Alpha) is the strength
        /// of the background.
        /// </summary>
        Transparent,

        /// <summary>
        /// Add color to the background. Alpha channel measures 
        /// the strength of addition.
        /// </summary>
        Additive,

        /// <summary>
        /// Subtract color from the background. Alpha channel measures
        /// the strength of subtraction.
        /// </summary>
        Subtractive,
    }

    /// <summary>
    /// Draw mode of 2D graphics.
    /// </summary>
    public struct DrawMode2D : IComparable<DrawMode2D>
    {
        private DrawModeType2D _type;

        /// <summary>
        /// Is there any 2D graphics.
        /// </summary>
        public bool IsDrawn { get { return _type != DrawModeType2D.None; } }

        public DrawMode2D(DrawModeType2D type)
        {
            _type = type;
        }

        /// <summary>
        /// Calls <c>Begin</c> on a sprite batch and sets the correct render state.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch whose <c>Begin</c> to call.</param>
        public void BeginDraw(AssaultWingCore game, SpriteBatch spriteBatch)
        {
            var renderState = game.GraphicsDeviceService.GraphicsDevice.RenderState;
            switch (_type)
            {
                case DrawModeType2D.None:
                    // We're not going to draw anything, so we don't need to Begin the sprite batch.
                    break;
                case DrawModeType2D.Transparent:
                    spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);
                    break;
                case DrawModeType2D.Additive:
                    spriteBatch.Begin(SpriteBlendMode.Additive, SpriteSortMode.BackToFront, SaveStateMode.None);
                    break;
                case DrawModeType2D.Subtractive:
                    // Sprite sorting must be disabled in order to get immediate mode
                    // which in turn is needed so that our changes to RenderState have any effect.
                    spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
                    renderState.BlendFunction = BlendFunction.ReverseSubtract;
                    renderState.DestinationBlend = Blend.One;
                    renderState.SourceBlend = Blend.SourceAlpha;
                    break;
                default:
                    throw new Exception("DrawMode2D: Unknown type of draw mode, " + _type);
            }
        }

        /// <summary>
        /// Calls <c>End</c> on a sprite batch that was previously Begun by this 
        /// <c>DrawMode2D</c> instance. Doesn't restore render state.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch whose <c>End</c> to call.</param>
        public void EndDraw(AssaultWingCore game, SpriteBatch spriteBatch)
        {
            switch (_type)
            {
                case DrawModeType2D.None:
                    // We never called SpriteBatch.Begin, so we won't call SpriteBatch.End.
                    break;
                case DrawModeType2D.Transparent:
                case DrawModeType2D.Additive:
                    spriteBatch.End();
                    break;
                case DrawModeType2D.Subtractive:
                    spriteBatch.End();
                    game.GraphicsDeviceService.GraphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
                    break;
                default:
                    throw new Exception("DrawMode2D: Unknown type of draw mode, " + _type);
            }
        }

        #region IComparable<DrawMode2D> Members

        /// <summary>
        /// Compares this object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>Less than zero if this is less than other.
        /// Zero if this is equal to other.
        /// Greater than zero if this is greater than other.</returns>
        public int CompareTo(DrawMode2D other)
        {
            return _type.CompareTo(other._type);
        }

        #endregion
    }
}
