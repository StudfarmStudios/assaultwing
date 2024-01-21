using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Helpers.Serialization;

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
    [LimitedSerialization]
    public struct DrawMode2D : IComparable<DrawMode2D>
    {
        [TypeParameter]
        public DrawModeType2D Type { get; private set; }

        /// <summary>
        /// Is there any 2D graphics.
        /// </summary>
        public bool IsDrawn { get { return Type != DrawModeType2D.None; } }

        public DrawMode2D(DrawModeType2D type)
            : this()
        {
            Type = type;
        }

        /// <summary>
        /// Calls <c>Begin</c> on a sprite batch and sets the correct render state.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch whose <c>Begin</c> to call.</param>
        public void BeginDraw(AssaultWingCore game, SpriteBatch spriteBatch)
        {
            switch (Type)
            {
                case DrawModeType2D.None:
                    // We're not going to draw anything, so we don't need to Begin the sprite batch.
                    break;
                case DrawModeType2D.Transparent:
                    spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    break;
                case DrawModeType2D.Additive:
                    spriteBatch.Begin(SpriteSortMode.BackToFront, GraphicsEngineImpl.AdditiveBlendPremultipliedAlpha);
                    break;
                case DrawModeType2D.Subtractive:
                    // Sprite sorting must be disabled in order to get immediate mode
                    // which in turn is needed so that our changes to RenderState have any effect.
                    // NOTE: This was the case in XNA 3.1, is it valid anymore in XNA 4.0?
                    spriteBatch.Begin(SpriteSortMode.Immediate, GraphicsEngineImpl.SubtractiveBlend);
                    break;
                default:
                    throw new Exception("DrawMode2D: Unknown type of draw mode, " + Type);
            }
        }

        /// <summary>
        /// Calls <c>End</c> on a sprite batch that was previously Begun by this 
        /// <c>DrawMode2D</c> instance. Doesn't restore render state.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch whose <c>End</c> to call.</param>
        public void EndDraw(AssaultWingCore game, SpriteBatch spriteBatch)
        {
            switch (Type)
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
                    game.GraphicsDeviceService.GraphicsDevice.BlendState = BlendState.AlphaBlend;
                    break;
                default:
                    throw new Exception("DrawMode2D: Unknown type of draw mode, " + Type);
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
            return Type.CompareTo(other.Type);
        }

        #endregion
    }
}
