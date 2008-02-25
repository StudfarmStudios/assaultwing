using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    [System.Diagnostics.DebuggerDisplay("min:({minX},{minY}) max:({maxX},{maxY})")]
    public class WindowQuery : RegionQuery
    {
        protected Single minX, minY, maxX, maxY;

        public Single MinX
        {
            get { return minX; }
            protected set { minX = value; }
        }

        public Single MinY
        {
            get { return minY; }
            protected set { minY = value; }
        }

        public Single MaxX
        {
            get { return maxX; }
            protected set { maxX = value; }
        }

        public Single MaxY
        {
            get { return maxY; }
            protected set { maxY = value; }
        }

        public WindowQuery(Single minX, Single minY, Single maxX, Single maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }
    }
}
