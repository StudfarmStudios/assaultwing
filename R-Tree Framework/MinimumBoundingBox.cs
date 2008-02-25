using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    [System.Diagnostics.DebuggerDisplay("min:({minX},{minY}) max:({maxX},{maxY})")]
    public struct MinimumBoundingBox
    {
        private Single maxX, maxY, minX, minY;

        public Single MinY
        {
            get { return minY; }
            set { minY = value; }
        }

        public Single MinX
        {
            get { return minX; }
            set { minX = value; }
        }

        public Single MaxY
        {
            get { return maxY; }
            set { maxY = value; }
        }

        public Single MaxX
        {
            get { return maxX; }
            set { maxX = value; }
        }

        public MinimumBoundingBox(Single minX, Single minY, Single maxX, Single maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }

        public Single GetArea()
        {
            return (MaxX - MinX) * (MaxY - MinY);
        }

        public Single GetPerimeter()
        {
            return 2*((MaxX - MinX) + (MaxY - MinY));
        }
    }
}
