using System;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// A float-valued parameter for particle management objects of a peng.
    /// </summary>
    /// Particle management parameters are functions of three arguments,
    /// particle age, external peng input and particle random seed.
    public interface PengParameter
    {
        /// <summary>
        /// Returns the particle management parameter's value for the given arguments.
        /// </summary>
        /// <param name="age">Particle age</param>
        /// <param name="random">Particle random seed</param>
        /// <returns>Parameter's value for the given arguments</returns>
        float GetValue(float age, int random);
    }
}
