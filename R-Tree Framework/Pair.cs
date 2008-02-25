using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    public class Pair<T1, T2>
    {
        private T1 value1;
        private T2 value2;

        public Pair(T1 value1, T2 value2)
        {
            this.value1 = value1;
            this.value2 = value2;
        }

        public T1 Value1
        {
            get { return value1; }
        }
        public T2 Value2
        {
            get { return value2; }
        }

    }
}
