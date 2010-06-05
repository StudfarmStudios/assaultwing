using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AW2.Core;
using AW2.Helpers;

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
        /// <param name="input">External peng input value</param>
        /// <param name="random">Particle random seed</param>
        /// <returns>Parameter's value for the given arguments</returns>
        float GetValue(float age, float input, int random);
    }

    /// <summary>
    /// A particle management parameter that consists of many <c>Curve</c>s, each
    /// associated with a value of external peng input (between 0 and 1). The value
    /// of <c>CurveLerp</c> for arguments <c>(age, input, random)</c> is linearly interpolated 
    /// between the <c>Curve</c>s closest to <c>input</c>.
    /// Particle random acts as a constant shift in the function's value,
    /// and the amplitude of the shift can be determined.
    /// All values are clamped to set minimum and maximum values.
    /// 
    /// It is common that several <c>CurveLerp</c>s are evaluated with the same random
    /// value, and it would be desirable to have the <c>CurveLerp</c>s have different
    /// kinds of random despite the same random number. For example, it is not desirable
    /// that when a particle is randomised to have a large scale, it will also have
    /// a large alpha value. To avoid such a thing, <c>CurveLerp</c> has a 'random mixer'
    /// value which uses the random value as a seed in a random sequence of its own,
    /// and picks the n'th value in this sequence.
    /// </summary>
    public class CurveLerp : PengParameter
    {
        float min, max, randomAmplitude;
        int randomMixer;
        CurveLerpKeyCollection keys;

        /// <summary>
        /// The least possible value of the parameter. 
        /// </summary>
        public float Min { get { return min; } set { min = value; } }

        /// <summary>
        /// The greatest possible value of the parameter. 
        /// </summary>
        public float Max { get { return max; } set { max = value; } }

        /// <summary>
        /// The maximum shift a particle's random seed can cause in the
        /// value of the parameter.
        /// </summary>
        public float RandomAmplitude { get { return randomAmplitude; } set { randomAmplitude = value; } }

        /// <summary>
        /// Mixing value of particle random values.
        /// </summary>
        public int RandomMixer { get { return randomMixer; } set { randomMixer = value; } }

        /// <summary>
        /// The curve lerp's keys.
        /// </summary>
        public CurveLerpKeyCollection Keys { get { return keys; } }

        /// <summary>
        /// Creates an uninitialised curve lerp.
        /// </summary>
        /// This constructor is for serialisation.
        public CurveLerp()
        {
            min = 0;
            max = 1;
            randomAmplitude = 0.3f;
            keys = new CurveLerpKeyCollection();
            Curve curve0 = new Curve();
            curve0.Keys.Add(new CurveKey(0, 0.3f));
            curve0.Keys.Add(new CurveKey(1, 0.6f));
            keys.Add(new CurveLerpKey(0, curve0));
            Curve curve1 = new Curve();
            curve1.Keys.Add(new CurveKey(0, 0.4f));
            curve1.Keys.Add(new CurveKey(1, 0.7f));
            keys.Add(new CurveLerpKey(1, curve1));
        }

        /// <summary>
        /// Returns the particle management parameter's value for the given arguments.
        /// </summary>
        /// <param name="age">Particle age, usually between 0 (newborn) and 1 (dead).</param>
        /// <param name="input">External peng input value, between 0 and 1.</param>
        /// <param name="random">Particle random seed.</param>
        /// <returns>Parameter's value for the given arguments</returns>
        public float GetValue(float age, float input, int random)
        {
            float value = float.NaN;

            // Find key or keys that are next to 'input'.
            if (input <= keys[0].Input)
            {
                // Use the curve of least specified 'input'.
                value = keys[0].Curve.Evaluate(age);
            }
            else if (input >= keys[keys.Count-1].Input)
            {
                // Use the curve of the greatest specified 'input'.
                value = keys[keys.Count-1].Curve.Evaluate(age);
            }
            else 
            {
                // Linear search -- 'keys' are assumed to be a small collection.
                int i = 0;
                while (keys[i + 1].Input < input) ++i;

                // 'input' is between keys at i and i+1.
                // Linearly interpolate between them.
                float weight2 = (input - keys[i].Input) / (keys[i+1].Input - keys[i].Input);
                value = MathHelper.Lerp(keys[i].Curve.Evaluate(age), keys[i + 1].Curve.Evaluate(age), weight2);
            }

            // Add random and clamp to limits.
            int ourRandom = RandomHelper.MixRandomInt(random, randomMixer);
            return MathHelper.Clamp(value + randomAmplitude * (ourRandom / (float)int.MaxValue), min, max);
        }
    }

    /// <summary>
    /// A key for a curve lerp.
    /// </summary>
    public class CurveLerpKey : IConsistencyCheckable
    {
        float input;
        Curve curve;

        /// <summary>
        /// The external peng input value the key corresponds to.
        /// </summary>
        public float Input { get { return input; } }

        /// <summary>
        /// The curve representing the particle management parameter with the 
        /// associated external peng input value.
        /// </summary>
        public Curve Curve { get { return curve; } }

        /// <summary>
        /// Creates a curve lerp key.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="curve"></param>
        public CurveLerpKey(float input, Curve curve)
        {
            this.input = input;
            this.curve = curve;
        }

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public void MakeConsistent(Type limitationAttribute)
        {
            // Rewrite our curve by linear interpolation between keys.
            // This is a temporary hack to simplify curve editing in serialised XML.
#warning CurveLerpKey tangents are always linear
            curve.ComputeTangents(CurveTangent.Linear);
        }

        #endregion
    }

    /// <summary>
    /// A collection of curve lerp keys.
    /// </summary>
    public class CurveLerpKeyCollection
    {
        /// <summary>
        /// The keys, sorted by <c>CurveLerpKey.Input</c>.
        /// </summary>
        List<CurveLerpKey> keys;

        /// <summary>
        /// The number of elements contained in the collection.
        /// </summary>
        public int Count { get { return keys.Count; } }

        /// <summary>
        /// The elements in the collection.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <returns>The element at the index in the collection.</returns>
        public CurveLerpKey this[int index] { get { return keys[index]; } }

        /// <summary>
        /// Creates an empty curve lerp key collection.
        /// </summary>
        public CurveLerpKeyCollection()
        {
            keys = new List<CurveLerpKey>();
        }

        /// <summary>
        /// Adds a curve lerp key to the collection.
        /// </summary>
        /// <param name="key">The key to add.</param>
        public void Add(CurveLerpKey key)
        {
            // Linear search -- 'keys' are assumed to be a small collection.
            int i = 0;
            while (i < keys.Count && keys[i].Input < key.Input) ++i;
            keys.Insert(i, key);
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            keys.Clear();
        }
    }
}
