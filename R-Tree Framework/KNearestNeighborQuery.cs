using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    public class KNearestNeighborQuery : Query
    {
        protected Int32 k;
        protected Single x, y;

        public Int32 K
        {
            get { return k; }
            protected set { k = value; }
        }
        public Single X
        {
            get { return x; }
            protected set { x = value; }
        }
        public Single Y
        {
            get { return y; }
            protected set { y = value; }
        }

        public KNearestNeighborQuery(Int32 k, Single x, Single y)
        {
            this.k = k;
            this.x = x;
            this.y = y;
        }
    }
}
