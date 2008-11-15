using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// Creates particles and gobs.
    /// </summary>
    /// A particle emitter is part of a peng.
    /// <see cref="AW2.Game.Gobs.Peng"/>
    public abstract class ParticleEmitter
    {
        #region ParticleEmitter fields

        /// <summary>
        /// Names of textures of particles to emit.
        /// </summary>
        [TypeParameter]
        protected string[] textureNames;

        /// <summary>
        /// Names of types of gobs to emit.
        /// </summary>
        [TypeParameter]
        protected string[] gobTypeNames;

        /// <summary>
        /// The peng where this emitter belongs to.
        /// </summary>
        protected Peng peng;

        #endregion ParticleEmitter fields

        #region ParticleEmitter properties

        /// <summary>
        /// Names of textures of particles to emit.
        /// </summary>
        public string[] TextureNames { get { return textureNames; } }

        /// <summary>
        /// Names of types of gobs to emit.
        /// </summary>
        public string[] GobTypeNames { get { return gobTypeNames; } }

        /// <summary>
        /// The peng where this emitter belongs to.
        /// </summary>
        public Peng Peng { get { return peng; } set { peng = value; } }

        /// <summary>
        /// <c>true</c> if emitting has finished for good
        /// <c>false</c> otherwise.
        /// </summary>
        public abstract bool Finished { get; }

        #endregion ParticleEmitter properties

        /// <summary>
        /// Returns created particles, adds created gobs to <c>DataEngine</c>.
        /// </summary>
        /// <returns>Created particles, or <c>null</c> if no particles were created.</returns>
        public abstract ICollection<Particle> Emit();

        /// <summary>
        /// Creates an uninitialised particle emitter.
        /// </summary>
        /// This constructor is for serialisation.
        public ParticleEmitter()
        {
            textureNames = new string[] { "dummytexture" };
            gobTypeNames = new string[] { "dummygob" };
        }
    }
}
