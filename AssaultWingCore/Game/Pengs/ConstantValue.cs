using System;

namespace AW2.Game.Pengs
{
    public class ConstantValue : PengParameter
    {
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
