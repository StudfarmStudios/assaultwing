using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    [System.Diagnostics.DebuggerDisplay("ID:{recordID} box:{minimumBoundingBox}")]
    public class Record : ITreeElement
    {
        protected MinimumBoundingBox minimumBoundingBox;
        protected Int32 recordID;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public virtual Int32 RecordID
        {
            get { return recordID; }
            protected set { recordID = value; }
        }
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public virtual MinimumBoundingBox BoundingBox
        {
            get { return minimumBoundingBox; }
            set { minimumBoundingBox = value; }
        }

        public Record(Int32 recordID, MinimumBoundingBox minimumBoundingBox)
        {
            RecordID = recordID;
            BoundingBox = minimumBoundingBox;
        }
    }
}
