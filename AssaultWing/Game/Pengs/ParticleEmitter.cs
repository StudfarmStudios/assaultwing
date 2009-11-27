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
        protected CanonicalString[] textureNames;

        /// <summary>
        /// Names of types of gobs to emit.
        /// </summary>
        [TypeParameter]
        protected CanonicalString[] gobTypeNames;

        /// <summary>
        /// If <c>true</c>, the peng won't emit new particles.
        /// </summary>
        [RuntimeState]
        protected bool paused;

        /// <summary>
        /// The peng where this emitter belongs to.
        /// </summary>
        protected Peng peng;

        #endregion ParticleEmitter fields

        #region ParticleEmitter properties

        /// <summary>
        /// Names of textures of particles to emit.
        /// </summary>
        public CanonicalString[] TextureNames { get { return textureNames; } }

        /// <summary>
        /// Textures in the same order as in <see cref="textureNames"/>.
        /// </summary>
        public Texture2D[] Textures { get; private set; }

        /// <summary>
        /// Names of types of gobs to emit.
        /// </summary>
        public CanonicalString[] GobTypeNames { get { return gobTypeNames; } }

        /// <summary>
        /// If <c>true</c>, no particles will be emitted.
        /// </summary>
        public virtual bool Paused { get { return paused; } set { paused = value; } }

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
        /// Base classes should remember to not create anything if
        /// <c>paused == true</c>.
        public abstract ICollection<Particle> Emit();

        /// <summary>
        /// Creates an uninitialised particle emitter.
        /// </summary>
        /// This constructor is for serialisation.
        public ParticleEmitter()
        {
            textureNames = new CanonicalString[] { (CanonicalString)"dummytexture" };
            gobTypeNames = new CanonicalString[] { (CanonicalString)"dummygob" };
            paused = false;
        }

        /// <summary>
        /// Called when graphics resources need to be loaded.
        /// </summary>
        public void LoadContent()
        {
            Textures = new Texture2D[TextureNames.Length];
            for (int i = 0; i < TextureNames.Length; ++i)
                Textures[i] = AssaultWing.Instance.Content.Load<Texture2D>(TextureNames[i]);
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded.
        /// </summary>
        public void UnloadContent()
        {
        }
    }
}
