using System;
using System.Collections.Generic;
using System.Text;

namespace Edu.Psu.Cse.R_Tree_Framework.Framework
{
    public class Node : ITreeElement
    {
        protected List<NodeEntry> nodeEntries;
        protected Node parent;
        protected Type childType;

        public Type ChildType
        {
            get { return childType; }
            protected set { childType = value; }
        }

        protected virtual NodeChildType ChildTypeID
        {
            get
            {
                if (ChildType.Equals(typeof(Leaf)))
                    return NodeChildType.Leaf;
                else
                    return NodeChildType.Node;
            }
        }
        protected virtual PageDataType TypeID
        {
            get { return PageDataType.Node; }
        }

        public Node Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public List<NodeEntry> NodeEntries
        {
            get { return nodeEntries; }
            protected set { nodeEntries = value; }
        }

        public Node(Node parent, Type childType)
        {
            Parent = parent;
            ChildType = childType;
            nodeEntries = new List<NodeEntry>(Constants.MAXIMUM_ENTRIES_PER_NODE + 1);
        }

        public MinimumBoundingBox CalculateMinimumBoundingBox()
        {
            Single minX = nodeEntries[0].MinimumBoundingBox.MinX,
                minY = nodeEntries[0].MinimumBoundingBox.MinY,
                maxX = nodeEntries[0].MinimumBoundingBox.MaxX, 
                maxY = nodeEntries[0].MinimumBoundingBox.MaxY;
            foreach (NodeEntry node in nodeEntries)
            {
                if (node.MinimumBoundingBox.MinX < minX)
                    minX = node.MinimumBoundingBox.MinX;
                if (node.MinimumBoundingBox.MinY < minY)
                    minY = node.MinimumBoundingBox.MinY;
                if (node.MinimumBoundingBox.MaxX > maxX)
                    maxX = node.MinimumBoundingBox.MaxX;
                if (node.MinimumBoundingBox.MaxY > maxY)
                    maxY = node.MinimumBoundingBox.MaxY;
            }
            return new MinimumBoundingBox(minX, minY, maxX, maxY);
        }

        public void AddNodeEntry(NodeEntry child)
        {
            nodeEntries.Add(child);
        }
        public void RemoveNodeEntry(NodeEntry child)
        {
            nodeEntries.Remove(child);
        }
    }
}
