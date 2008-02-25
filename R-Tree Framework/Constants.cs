using System;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    public static class Constants
    {
        // Note: The maximum number of entries must be at least twice 
        // the minimum number of entries.
        public const Int32
            MAXIMUM_ENTRIES_PER_NODE = 20,
            MINIMUM_ENTRIES_PER_NODE = 2;
    }

    public enum PageDataType : byte { Node, Leaf, Record, IndexUnitSector }
    public enum NodeChildType : byte { Node, Leaf, Record }
    public enum Operation : byte { Insert, Update, Delete };
}