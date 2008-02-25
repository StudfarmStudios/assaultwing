using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.Sound
{
    /// <summary>
    /// Describes options for sounds.
    /// </summary>
    public class SoundOptions
    {
        /// <summary>
        /// A special effect to apply for a sound.
        /// </summary>
        public enum Effect { 
            /// <summary>
            /// No effect.
            /// </summary>
            None, 

            /// <summary>
            /// Sounds like the listener is underwater.
            /// </summary>
            Underwater,
        }

        /// <summary>
        /// A sound producing action.
        /// </summary>
        public enum Action {
            /// <summary>
            /// Ship vs. ground collision.
            /// </summary>
            Collision, 

            /// <summary>
            /// Ship vs. ship collision.
            /// </summary>
            Shipcollision, 

            /// <summary>
            /// Explosion.
            /// </summary>
            Explosion, 

            /// <summary>
            /// Pistol fire.
            /// </summary>
            Pistol, 

            /// <summary>
            /// Artillery fire.
            /// </summary>
            Artillery, 

            /// <summary>
            /// Missile fire.
            /// </summary>
            Missile, 

            /// <summary>
            /// Cannon fire.
            /// </summary>
            Cannon,
        }
    }
}
