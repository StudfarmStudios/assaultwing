using System;
using Microsoft.Xna.Framework;
using AW2.Helpers.Serialization;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// A key for a <see cref="CurveLerp"/>.
    /// </summary>
    [Obsolete]
    public class CurveLerpKey : IConsistencyCheckable
    {
        /// <summary>
        /// The external peng input value the key corresponds to.
        /// </summary>
        public float Input { get; private set; }

        /// <summary>
        /// The curve representing the particle management parameter with the 
        /// associated external peng input value.
        /// </summary>
        public Curve Curve { get; private set; }

        public CurveLerpKey(float input, Curve curve)
        {
            Input = input;
            Curve = curve;
        }

        public void MakeConsistent(Type limitationAttribute)
        {
            // Rewrite our curve by linear interpolation between keys.
            // This is a temporary hack to simplify curve editing in serialised XML.
            // TODO !!! Replace CurveLerp by SimpleCurve
            Curve.ComputeTangents(CurveTangent.Linear);
        }
    }
}
