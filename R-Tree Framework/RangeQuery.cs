using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    [System.Diagnostics.DebuggerDisplay("center:({centerX},{centerY}) radius:{radius}")]
    public class RangeQuery : RegionQuery
    {
        protected Single centerX, centerY, radius;

        public Single CenterX
        {
            get { return centerX; }
            protected set { centerX = value; }
        }

        public Single CenterY
        {
            get { return centerY; }
            protected set { centerY = value; }
        }

        public Single Radius
        {
            get { return radius; }
            protected set { radius = value; }
        }

        public RangeQuery(Single centerX, Single centerY, Single radius)
        {
            this.centerX = centerX;
            this.centerY = centerY;
            this.radius = radius;
        }
    }
}
