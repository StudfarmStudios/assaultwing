using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Game.Gobs;
using AW2.Helpers;

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
        public TimeSpan BirthTime;

        /// <summary>
        /// Custom timeout, in game time.
        /// </summary>
        /// The meaning of this field is decided by the particle updater.
        public TimeSpan Timeout;

        /// <summary>
        /// Index of the texture to draw the particle with, as listed in
        /// <see cref="SprayEmitter.Textures"/>.
        /// </summary>
        public int TextureIndex;

        /// <summary>
        /// Position of the particle in an unspecified coordinate system.
        /// </summary>
        /// The meaning of this field is determined by the peng that manages this particle.
        /// In other words, this can either be location in game coordinates or
        /// location in the peng's own coordinate system.
        public Vector2 Pos;

        /// <summary>
        /// Movement of the particle, in meters per second.
        /// </summary>
        public Vector2 Move;

        /// <summary>
        /// Emission direction of the particle.
        /// </summary>
        public float Direction;
        public Vector2 DirectionVector;

        /// <summary>
        /// Rotation angle of the particle, in radians.
        /// </summary>
        public float Rotation;

        /// <summary>
        /// Size scale of the particle's texture.
        /// </summary>
        public float Scale;

        /// <summary>
        /// Alpha value of the particle, between 0 and 1. 0 is transparent, 1 is opaque.
        /// </summary>
        public float Alpha;

        /// <summary>
        /// Draw order of the particle relative to other particles of the same peng.
        /// 0 is front, 1 is back.
        /// </summary>
        public float LayerDepth;

        /// <summary>
        /// External peng input value at the time of the particle's birth.
        /// </summary>
        public float PengInput;

        /// <summary>
        /// The particle's random seed.
        /// </summary>
        public int Random;
    }
}