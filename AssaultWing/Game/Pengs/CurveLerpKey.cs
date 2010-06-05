using System;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// A key for a <see cref="CurveLerp"/>.
    /// </summary>
    public class CurveLerpKey : IConsistencyCheckable
    {
        private float _input;
        private Curve _curve;

        /// <summary>
        /// The external peng input value the key corresponds to.
        /// </summary>
        public float Input { get { return _input; } }

        /// <summary>
        /// The curve representing the particle management parameter with the 
        /// associated external peng input value.
        /// </summary>
        public Curve Curve { get { return _curve; } }

        public CurveLerpKey(float input, Curve curve)
        {
            _input = input;
            _curve = curve;
        }

        public void MakeConsistent(Type limitationAttribute)
        {
            // Rewrite our curve by linear interpolation between keys.
            // This is a temporary hack to simplify curve editing in serialised XML.
#warning CurveLerpKey tangents are always linear
            _curve.ComputeTangents(CurveTangent.Linear);
        }
    }
}
