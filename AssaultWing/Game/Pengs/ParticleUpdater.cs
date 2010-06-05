using System;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// Updates particles.
    /// </summary>
    /// A particle updater is part of a peng.
    /// <see cref="AW2.Game.Gobs.Peng"/>
    public interface ParticleUpdater
    {
        /// <summary>
        /// Updates the state of a particle and returns if it died or not.
        /// </summary>
        /// The given particle should belong to the peng that owns this particle updater.
        /// <param name="particle">The particle to update.</param>
        /// <returns><c>true</c> if the particle died and should be removed,
        /// <c>false</c> otherwise.</returns>
        bool Update(Particle particle);
    }
}
