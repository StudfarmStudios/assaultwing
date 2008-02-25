using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    public class LeafEntry : NodeEntry
    {
        public LeafEntry(MinimumBoundingBox minimumBoundingBox, ITreeElement recordIdentifier)
            : base(minimumBoundingBox, recordIdentifier)
        {
        }
    }
}
