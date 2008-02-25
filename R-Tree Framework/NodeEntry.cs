using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    public class NodeEntry : ITreeElement
    {
        protected MinimumBoundingBox minimumBoundingBox;
        protected ITreeElement child;

        public MinimumBoundingBox MinimumBoundingBox
        {
            get { return minimumBoundingBox; }
            set { minimumBoundingBox = value; }
        }
        public ITreeElement Child
        {
            get { return child; }
            protected set { child = value; }
        }

        public NodeEntry(MinimumBoundingBox minimumBoundingBox, ITreeElement child)
        {
            MinimumBoundingBox = minimumBoundingBox;
            Child = child;
        }
    }
}
