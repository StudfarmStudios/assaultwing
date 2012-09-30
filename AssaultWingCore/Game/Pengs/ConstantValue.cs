using System;
using AW2.Helpers.Serialization;

namespace AW2.Game.Pengs
{
    [LimitedSerialization]
    public class ConstantValue : PengParameter
    {
        [TypeParameter]
        private float _value;

        /// <summary>
        /// This constructor is for serialization only.
        /// </summary>
        public ConstantValue()
        {
            _value = 0;
        }

        public float GetValue(float age, int random)
        {
            return _value;
        }
    }
}
