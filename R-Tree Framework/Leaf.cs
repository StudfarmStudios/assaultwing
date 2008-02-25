using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    public class Leaf : Node
    {
        public Leaf(Node parent)
            : base(parent, typeof(Record))
        {
        }
        protected override NodeChildType ChildTypeID
        {
            get { return NodeChildType.Record; }
        }
        protected override PageDataType TypeID
        {
            get { return PageDataType.Leaf; }
        }
    }
}
