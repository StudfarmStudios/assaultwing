using System;
using System.Collections.Generic;
using System.Text;

namespace AW2.Helpers
{
    /// <summary>
    /// Provides helper methods for simple statistical analysis.
    /// </summary>
    /// Code borrowed from http://www.thescripts.com/forum/thread254401.html
    /// (c) Kyle Kaitan 2005
    /// Modified, though.
    class Statistics
    {
        /// <summary>
        /// Computes the median x~ of a set of values X. O(1) implementation.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float Median(float[] values)
        {
            int size = values.Length - 1;
            return ((values[size / 2] + values[size / 2 + 1]) / 2);
        }

        /// <summary>
        /// Computes the mode set x{} of a set of values X. O(n) implementation.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float[] Mode(float[] values)
        {
            Dictionary<float, int> h = new Dictionary<float,int>();
            int count = 0;
            foreach (float value in values)
            {
                h[value]++;
                if (h[value] > count)
                    count = h[value];
            }

            List<float> modeValues = new List<float>();
            foreach (KeyValuePair<float,int> e in h)
            {
                if (e.Value >= count) modeValues.Add(e.Key);
            }

            return modeValues.ToArray();
        }

        /// <summary>
        /// Computes the arithmetic mean x-bar of a set of values X. O(n) implementation.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float Mean(float[] values)
        {
            float sum = 0;
            foreach (float value in values)
            {
                sum += value;
            }
            return sum / (values.Length);
        }
    }
}
