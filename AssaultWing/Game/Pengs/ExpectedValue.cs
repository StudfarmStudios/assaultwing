namespace AW2.Game.Pengs
{
    /// <summary>
    /// Represents a random variable with a given expected value and variance.
    /// </summary>
    public class ExpectedValue : PengParameter
    {
        private float _expected;
        private float _variance;

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public ExpectedValue()
        {
            _expected = 5;
            _variance = 2;
        }

        public float GetValue(float age, float input, int random)
        {
            return _expected + _variance * (random / (float)int.MaxValue);
        }
    }
}
