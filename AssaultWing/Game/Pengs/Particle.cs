using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// A particle, managed by a peng.
    /// </summary>
    /// Particles are only visual. They don't interact with gobs
    /// and they don't necessarily behave according to <c>PhysicsEngine</c>.
    /// <see cref="AW2.Game.Gobs.Peng"/>
    public class Particle
    {
        /// <summary>
        /// Time of birth of the particle, in game time.
        /// </summary>
        public TimeSpan birthTime;

        /// <summary>
        /// Custom timeout, in game time.
        /// </summary>
        /// The meaning of this field is decided by the particle updater.
        public TimeSpan timeout;

        /// <summary>
        /// Name of the texture to draw the particle with.
        /// </summary>
        public string textureName;

        /// <summary>
        /// Position of the particle in an unspecified coordinate system.
        /// </summary>
        /// The meaning of this field is determined by the peng that manages this particle.
        /// In other words, this can either be location in game coordinates or
        /// location in the peng's own coordinate system.
        public Vector2 pos;

        /// <summary>
        /// Movement of the particle, in meters per second.
        /// </summary>
        public Vector2 move;

        /// <summary>
        /// Emission direction of the particle.
        /// </summary>
        public float direction;

        /// <summary>
        /// Rotation angle of the particle, in radians.
        /// </summary>
        public float rotation;

        /// <summary>
        /// Size scale of the particle's texture.
        /// </summary>
        public float scale;

        /// <summary>
        /// Alpha value of the particle, between 0 and 1. 0 is transparent, 1 is opaque.
        /// </summary>
        public float alpha;

        /// <summary>
        /// Draw order of the particle relative to other particles of the same peng.
        /// 0 is front, 1 is back.
        /// </summary>
        public float layerDepth;

        /// <summary>
        /// External peng input value at the time of the particle's birth.
        /// </summary>
        public float pengInput;

        /// <summary>
        /// The particle's random seed.
        /// </summary>
        public int random;
    }
}