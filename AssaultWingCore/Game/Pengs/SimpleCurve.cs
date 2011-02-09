using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// A particle management parameter that consists of one piecewise linear curve.
    /// The definition points (keys) of the curve are spread at even intervals.
    /// </summary>
    public class SimpleCurve : PengParameter
    {
        /// <summary>
        /// The maximum shift a particle's random seed can cause in the
        /// value of the parameter.
        /// </summary>
        private float _randomAmplitude;

        /// <summary>
        /// Mixing value of particle random values.
        /// </summary>
        private int _randomMixer;

        private float[] _keys;

        /// <summary>
        /// This constructor is for serialisation only.
        /// </summary>
        public SimpleCurve()
        {
            _randomAmplitude = 0.3f;
            _randomMixer = 0;
            _keys = new[] { 0.5f, 1.2f, 1.3f, 0.7f };
        }

        public float GetValue(float age, int random)
        {
            var floatIndex = (Math.Abs(age) % 1f) * (_keys.Length - 1);
            var index = (int)floatIndex;
            var baseValue = MathHelper.Lerp(_keys[index], _keys[index + 1], floatIndex % 1f);
            var ourRandom = RandomHelper.MixRandomInt(random, _randomMixer);
            return baseValue + _randomAmplitude * (ourRandom / (float)int.MaxValue);
        }
    }
}
