using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Edu.Psu.Cse.R_Tree_Framework.Framework;

namespace Edu.Psu.Cse.R_Tree_Framework.Indexes
{
    public class R_Tree
    {
        #region Instance Variables

        protected Int32 treeHeight;
        protected Node root;

        #endregion
        #region Properties

        public Node Root
        {
            get { return root; }
            protected set { root = value; }
        }
        public Int32 TreeHeight
        {
            get { return treeHeight; }
            protected set { treeHeight = value; }
        }

        #endregion
        #region Constructors

        public R_Tree()
        {
            Node rootNode = new Leaf(null);
            root = rootNode;
            TreeHeight = 1;
        }

        #endregion
        #region Public Methods

        public virtual void Delete(Record record)
        {
            Leaf leafWithRecord = FindLeaf(record, Root);
            if (leafWithRecord == null)
                return;
            LeafEntry entryToRemove = null;
            foreach (LeafEntry entry in leafWithRecord.NodeEntries)
                if (entry.Child.Equals(record))
                    entryToRemove = entry;
            leafWithRecord.RemoveNodeEntry(entryToRemove);
            CondenseTree(leafWithRecord);
            Node rootNode = Root;
            if (rootNode.NodeEntries.Count == 1 && typeof(Node).IsAssignableFrom(rootNode.ChildType))
            {
                Node newRoot = (Node)rootNode.NodeEntries[0].Child;
                newRoot.Parent = null;
                Root = newRoot;
                TreeHeight--;
            }
        }
        public virtual void Insert(Record record)
        {
            Leaf leafToInsertInto = ChooseLeaf(record);
            Insert(record, leafToInsertInto);
            if (leafToInsertInto.NodeEntries.Count > Constants.MAXIMUM_ENTRIES_PER_NODE)
            {
                List<Node> splitNodes = Split(leafToInsertInto);
                RemoveFromParent(leafToInsertInto);
                AdjustTree(splitNodes[0] as Leaf, splitNodes[1] as Leaf);
            }
            else
                AdjustTree(leafToInsertInto);
        }
        public virtual List<Record> Search(Query query)
        {
            if (query is RegionQuery)
                return Search(query as RegionQuery, Root);
            else if (query is KNearestNeighborQuery)
                return Search(query as KNearestNeighborQuery, Root);
            else
                return null;
        }
        public virtual void Update(Record originalRecord, Record newRecord)
        {
            Delete(originalRecord);
            Insert(newRecord);
        }

        #endregion
        #region Protected Methods

        protected virtual void AdjustTree(Node node)
        {
            AdjustTree(node, null);
        }
        protected virtual void AdjustTree(Node node1, Node node2)
        {
            if (node1.Equals(Root))
            {
                return;
            }
            if (Root == null)
            {
                Type childType = node1 is Leaf ? typeof(Leaf) : typeof(Node);
                Node rootNode = new Node(null, childType);
                Root = rootNode;
                node1.Parent = Root;
                node2.Parent = Root;
                TreeHeight++;
            }
            Node parent = node1.Parent;
            NodeEntry entryToUpdate = null;
            foreach (NodeEntry entry in parent.NodeEntries)
                if (entry.Child.Equals(node1))
                    entryToUpdate = entry;
            if (entryToUpdate == null)
                parent.AddNodeEntry(new NodeEntry(node1.CalculateMinimumBoundingBox(), node1));
            else
                entryToUpdate.MinimumBoundingBox = node1.CalculateMinimumBoundingBox();
            if (node2 != null)
            {
                parent.AddNodeEntry(new NodeEntry(node2.CalculateMinimumBoundingBox(), node2));
                if (parent.NodeEntries.Count > Constants.MAXIMUM_ENTRIES_PER_NODE)
                {
                    List<Node> splitNodes = Split(parent);
                    if (parent.Equals(Root))
                        Root = null;
                    RemoveFromParent(parent);
                    AdjustTree(splitNodes[0], splitNodes[1]);
                    return;
                }
            }
            AdjustTree(parent, null);
        }
        protected virtual Int32 CalculateHeight(Node node)
        {
            if (node is Leaf)
                return 1;
            Int32 height = 2;
            while (!node.ChildType.Equals(typeof(Leaf)))
            {
                node = (Node)node.NodeEntries[0].Child;
                height++;
            }
            return height;
        }
        protected virtual Leaf ChooseLeaf(Record record)
        {
            Node node = Root;
            while (!(node is Leaf))
            {
                NodeEntry minEnlargment = node.NodeEntries[0];
                Single minEnlargedArea = GetFutureSize(record, minEnlargment.MinimumBoundingBox) - minEnlargment.MinimumBoundingBox.GetArea();
                foreach (NodeEntry nodeEntry in node.NodeEntries)
                {
                    Single enlargment = GetFutureSize(record, nodeEntry.MinimumBoundingBox) - nodeEntry.MinimumBoundingBox.GetArea();
                    if ((enlargment == minEnlargedArea && nodeEntry.MinimumBoundingBox.GetArea() < minEnlargment.MinimumBoundingBox.GetArea()) ||
                        enlargment < minEnlargedArea)
                    {
                        minEnlargedArea = enlargment;
                        minEnlargment = nodeEntry;
                    }
                }
                node = (Node)minEnlargment.Child;
            }
            return node as Leaf;
        }
        protected virtual Node ChooseNode(Node node)
        {
            Node insertionNode = Root;
            MinimumBoundingBox nodeBoundingBox = node.CalculateMinimumBoundingBox();
            Int32 nodeHeight = CalculateHeight(node), currentDepth = 1;
            while (currentDepth + nodeHeight < TreeHeight)
            {
                NodeEntry minEnlargment = insertionNode.NodeEntries[0];
                Single minEnlargedArea = GetFutureSize(nodeBoundingBox, minEnlargment.MinimumBoundingBox) - minEnlargment.MinimumBoundingBox.GetArea();
                foreach (NodeEntry nodeEntry in insertionNode.NodeEntries)
                {
                    Single enlargment = GetFutureSize(nodeBoundingBox, nodeEntry.MinimumBoundingBox) - nodeEntry.MinimumBoundingBox.GetArea();
                    if ((enlargment == minEnlargedArea && nodeEntry.MinimumBoundingBox.GetArea() < minEnlargment.MinimumBoundingBox.GetArea()) ||
                        enlargment < minEnlargedArea)
                    {
                        minEnlargedArea = enlargment;
                        minEnlargment = nodeEntry;
                    }
                }
                insertionNode = (Node)minEnlargment.Child;
                currentDepth++;
            }
            return insertionNode;
        }
        protected virtual MinimumBoundingBox CombineMinimumBoundingBoxes(MinimumBoundingBox area1, MinimumBoundingBox area2)
        {
            Single newMinX, newMaxX, newMinY, newMaxY;
            if (area1.MinX < area2.MinX)
                newMinX = area1.MinX;
            else
                newMinX = area2.MinX;
            if (area1.MaxX > area2.MaxX)
                newMaxX = area1.MaxX;
            else
                newMaxX = area2.MaxX;
            if (area1.MinY < area2.MinY)
                newMinY = area1.MinY;
            else
                newMinY = area2.MinY;
            if (area1.MaxY > area2.MaxY)
                newMaxY = area1.MaxY;
            else
                newMaxY = area2.MaxY;
            return new MinimumBoundingBox(newMinX, newMinY, newMaxX, newMaxY);
        }
        protected virtual void CondenseTree(Node node)
        {
            List<Node> eliminatedNodes = new List<Node>();
            while (!node.Equals(Root))
            {
                Node parent = node.Parent;
                NodeEntry nodeEntry = null;
                foreach (NodeEntry entry in parent.NodeEntries)
                    if (entry.Child.Equals(node))
                        nodeEntry = entry;
                if (node.NodeEntries.Count < Constants.MINIMUM_ENTRIES_PER_NODE)
                {
                    parent.RemoveNodeEntry(nodeEntry);
                    eliminatedNodes.Add(node);
                }
                else
                    nodeEntry.MinimumBoundingBox = node.CalculateMinimumBoundingBox();
                node = parent;
            }
            for (int i = 0; i < eliminatedNodes.Count; i++)
            {
                Node eliminatedNode = eliminatedNodes[i];
                if (eliminatedNode is Leaf)
                    foreach (LeafEntry leafEntry in eliminatedNode.NodeEntries)
                        Insert((Record)leafEntry.Child);
                else
                    foreach (NodeEntry entry in eliminatedNode.NodeEntries)
                        Insert((Node)entry.Child);
            }
        }
        protected virtual void EnqueNodeEntries(KNearestNeighborQuery kNN, Node node, PriorityQueue<NodeEntry, Single> proximityQueue)
        {
            foreach (NodeEntry entry in node.NodeEntries)
                proximityQueue.Enqueue(entry, GetDistance(kNN.X, kNN.Y, entry.MinimumBoundingBox) * -1);
        }
        protected virtual Leaf FindLeaf(Record record, Node node)
        {
            if (node is Leaf)
            {
                foreach (LeafEntry entry in node.NodeEntries)
                    if (entry.Child.Equals(record))
                        return node as Leaf;
            }
            else
                foreach (NodeEntry entry in node.NodeEntries)
                    if (Overlaps(entry.MinimumBoundingBox, record.BoundingBox))
                    {
                        Leaf leaf = FindLeaf(record, (Node)entry.Child);
                        if (leaf != null)
                            return leaf;
                    }
            return null;
        }
        protected virtual Single GetDistance(Single x, Single y, MinimumBoundingBox area)
        {
            if (area.MaxX < x && area.MinY > y)
                return GetDistance(x, y, area.MaxX, area.MinY);
            else if (area.MinX > x && area.MinY > y)
                return GetDistance(x, y, area.MinX, area.MinY);
            else if (area.MaxX < x && area.MaxY < y)
                return GetDistance(x, y, area.MaxX, area.MaxY);
            else if (area.MinX > x && area.MaxY < y)
                return GetDistance(x, y, area.MinX, area.MaxY);
            else if (area.MaxX < x)
                return GetDistance(x, y, area.MaxX, y);
            else if (area.MinX > x)
                return GetDistance(x, y, area.MinX, y);
            else if (area.MaxY < y)
                return GetDistance(x, y, x, area.MaxY);
            else if (area.MinY > y)
                return GetDistance(x, y, x, area.MinY);
            else
                return 0;
        }
        protected virtual Single GetDistance(Single x1, Single y1, Single x2, Single y2)
        {
            return (((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
        }
        protected virtual Single GetFutureSize(Record record, MinimumBoundingBox area)
        {
            return GetFutureSize(record.BoundingBox, area);
        }
        protected virtual Single GetFutureSize(MinimumBoundingBox area1, MinimumBoundingBox area2)
        {
            MinimumBoundingBox futureMinimumBoundingBox = CombineMinimumBoundingBoxes(area1, area2);
            return (futureMinimumBoundingBox.MaxX - futureMinimumBoundingBox.MinX) *
                (futureMinimumBoundingBox.MaxX - futureMinimumBoundingBox.MinX) +
                (futureMinimumBoundingBox.MaxY - futureMinimumBoundingBox.MinY) *
                (futureMinimumBoundingBox.MaxY - futureMinimumBoundingBox.MinY);
        }
        protected virtual void Insert(Node node)
        {
            Node nodeToInsertInto = ChooseNode(node);
            Insert(node, nodeToInsertInto);
            if (nodeToInsertInto.NodeEntries.Count > Constants.MAXIMUM_ENTRIES_PER_NODE)
            {
                List<Node> splitNodes = Split(nodeToInsertInto);
                RemoveFromParent(nodeToInsertInto);
                AdjustTree(splitNodes[0], splitNodes[1]);
            }
            else
                AdjustTree(nodeToInsertInto);

        }
        protected virtual void Insert(Record record, Leaf leaf)
        {
            leaf.AddNodeEntry(new LeafEntry(record.BoundingBox, record));
        }
        protected virtual void Insert(Node newNode, Node node)
        {
            node.AddNodeEntry(new NodeEntry(newNode.CalculateMinimumBoundingBox(), newNode));
            newNode.Parent = node;
        }
        protected virtual Boolean Overlaps(RangeQuery range, MinimumBoundingBox area)
        {
            Single distance = 0;
            if (range.CenterX < area.MinX)
                distance = (range.CenterX - area.MinX) * (range.CenterX - area.MinX);
            else
                if (range.CenterX > area.MaxX)
                    distance = (range.CenterX - area.MaxX) * (range.CenterX - area.MaxX);
            if (range.CenterY < area.MinY)
                distance += (range.CenterY - area.MinY) * (range.CenterY - area.MinY);
            else
                if (range.CenterY > area.MaxY)
                    distance += (range.CenterY - area.MaxY) * (range.CenterY - area.MaxY);
            return (distance < range.Radius * range.Radius); 



            /*
            return
                //does not overlap the center point
                !(
                    range.CenterX < area.MinX ||
                    range.CenterX > area.MaxX ||
                    range.CenterY < area.MinY ||
                    range.CenterY > area.MaxY
                )
                ||
                //distance from center to any corner is less than a radius
                (
                    (range.CenterX - area.MinX) * (range.CenterX - area.MinX) + (range.CenterY - area.MinY) * (range.CenterY - area.MinY) < range.Radius * range.Radius ||
                    (range.CenterX - area.MaxX) * (range.CenterX - area.MaxX) + (range.CenterY - area.MinY) * (range.CenterY - area.MinY) < range.Radius * range.Radius ||
                    (range.CenterX - area.MinX) * (range.CenterX - area.MinX) + (range.CenterY - area.MaxY) * (range.CenterY - area.MaxY) < range.Radius * range.Radius ||
                    (range.CenterX - area.MaxX) * (range.CenterX - area.MaxX) + (range.CenterY - area.MaxY) * (range.CenterY - area.MaxY) < range.Radius * range.Radius
                )
                ||
                //the box intersects the circle but no corner lies within the circle
                (
                    (range.CenterX > area.MinX && range.CenterX < area.MaxX && range.CenterY < area.MinY && area.MinY - range.CenterY < range.Radius) ||
                    (range.CenterX > area.MinX && range.CenterX < area.MaxX && range.CenterY > area.MaxY && range.CenterY - area.MaxY < range.Radius) ||
                    (range.CenterY > area.MinY && range.CenterY < area.MaxY && range.CenterX < area.MinX && area.MinX - range.CenterX < range.Radius) ||
                    (range.CenterY > area.MinY && range.CenterY < area.MaxY && range.CenterX > area.MaxX && range.CenterX - area.MaxX < range.Radius)
                );*/
        }
        protected virtual Boolean Overlaps(WindowQuery window, MinimumBoundingBox area)
        {
            //checks the only conditions in which they don't overlap
            return !(
                window.MinX > area.MaxX ||   // left > right
                window.MaxX < area.MinX ||   // right < left
                window.MinY > area.MaxY ||   // bottom > top
                window.MaxY < area.MinY);    // top < bottom
        }
        protected virtual Boolean Overlaps(MinimumBoundingBox area1, MinimumBoundingBox area2)
        {
            //checks the only conditions in which they don't overlap
            return !(
                area1.MinX > area2.MaxX ||
                area1.MaxX < area2.MinX ||
                area1.MinY > area2.MaxY ||
                area1.MaxY < area2.MinY);
        }
        protected virtual NodeEntry PickNext(List<NodeEntry> entryPool, MinimumBoundingBox minimumBoundingBox1, MinimumBoundingBox minimumBoundingBox2)
        {
            NodeEntry nextEntry = entryPool[0];

            Single maxEnlargementDifference = Math.Abs(
                GetFutureSize(nextEntry.MinimumBoundingBox, minimumBoundingBox1) -
                GetFutureSize(nextEntry.MinimumBoundingBox, minimumBoundingBox2));
            foreach (NodeEntry entry in entryPool)
            {
                Single enlargmentDifference = Math.Abs(
                GetFutureSize(entry.MinimumBoundingBox, minimumBoundingBox1) -
                GetFutureSize(entry.MinimumBoundingBox, minimumBoundingBox2));
                if (enlargmentDifference > maxEnlargementDifference)
                {
                    maxEnlargementDifference = enlargmentDifference;
                    nextEntry = entry;
                }
            }
            return nextEntry;
        }
        protected virtual List<NodeEntry> LinearPickSeeds(List<NodeEntry> seedPool)
        {
            NodeEntry lowestMaxX = null;
            NodeEntry lowestMaxY = null;
            NodeEntry highestMinX = null;
            NodeEntry highestMinY = null;
            MinimumBoundingBox poolBox = seedPool[0].MinimumBoundingBox;
            foreach (NodeEntry entry in seedPool)
            {
                poolBox = CombineMinimumBoundingBoxes(poolBox, entry.MinimumBoundingBox);
                if (lowestMaxX == null || entry.MinimumBoundingBox.MaxX < lowestMaxX.MinimumBoundingBox.MaxX)
                    lowestMaxX = entry;
                if (lowestMaxY == null || entry.MinimumBoundingBox.MaxY < lowestMaxY.MinimumBoundingBox.MaxY)
                    lowestMaxY = entry;
                if (highestMinX == null || entry.MinimumBoundingBox.MinX > highestMinX.MinimumBoundingBox.MinX)
                    highestMinX = entry;
                if (highestMinY == null || entry.MinimumBoundingBox.MinY > highestMinY.MinimumBoundingBox.MinY)
                    highestMinY = entry;
            }
            float normalizedSeparationX =
                (highestMinX.MinimumBoundingBox.MinX - lowestMaxX.MinimumBoundingBox.MaxX)
                / (poolBox.MaxX - poolBox.MinX);
            float normalizedSeparationY =
                (highestMinY.MinimumBoundingBox.MinY - lowestMaxY.MinimumBoundingBox.MaxY)
                / (poolBox.MaxY - poolBox.MinY);
            List<NodeEntry> seeds = new List<NodeEntry>(2);
            if (normalizedSeparationX > normalizedSeparationY)
            {
                seeds.Add(lowestMaxX);
                seeds.Add(highestMinX);
            }
            else
            {
                seeds.Add(lowestMaxY);
                seeds.Add(highestMinY);
            }
            return seeds;
        }
        protected virtual List<NodeEntry> QuadraticPickSeeds(List<NodeEntry> seedPool)
        {
            NodeEntry worstPairEntry1, worstPairEntry2;
            worstPairEntry1 = seedPool[0];
            worstPairEntry2 = seedPool[1];
            Single worstEnlargement = GetFutureSize(worstPairEntry1.MinimumBoundingBox, worstPairEntry2.MinimumBoundingBox) -
                        worstPairEntry1.MinimumBoundingBox.GetArea() - worstPairEntry2.MinimumBoundingBox.GetArea();
            for (int i = 0; i < seedPool.Count; i++)
                for (int j = i + 1; j < seedPool.Count; j++)
                {
                    Single d = GetFutureSize(seedPool[i].MinimumBoundingBox, seedPool[j].MinimumBoundingBox) -
                        seedPool[i].MinimumBoundingBox.GetArea() - seedPool[j].MinimumBoundingBox.GetArea();
                    if (d > worstEnlargement)
                    {
                        worstPairEntry1 = seedPool[i];
                        worstPairEntry2 = seedPool[j];
                        worstEnlargement = d;
                    }
                }
            List<NodeEntry> worstPair = new List<NodeEntry>();
            worstPair.Add(worstPairEntry1);
            worstPair.Add(worstPairEntry2);
            return worstPair;
        }
        protected virtual void RemoveFromParent(Node node)
        {
            if (node.Parent != null)
            {
                Node parent = node.Parent;
                NodeEntry entryToRemove = null;
                foreach (NodeEntry entry in parent.NodeEntries)
                    if (entry.Child.Equals(node))
                        entryToRemove = entry;
                parent.RemoveNodeEntry(entryToRemove);
            }
            else
                Root = null;
        }
        protected virtual List<Record> Search(RegionQuery window, Node node)
        {
            List<Record> records = new List<Record>();
            foreach (NodeEntry nodeEntry in node.NodeEntries)
            {
                if (window is RangeQuery && Overlaps((RangeQuery)window, nodeEntry.MinimumBoundingBox) ||
                    window is WindowQuery && Overlaps((WindowQuery)window, nodeEntry.MinimumBoundingBox))
                    if (nodeEntry is LeafEntry)
                        records.Add((Record)nodeEntry.Child);
                    else
                        records.AddRange(Search(window, (Node)nodeEntry.Child));
            }
            return records;
        }
        protected virtual List<Record> Search(KNearestNeighborQuery kNN, Node node)
        {
            PriorityQueue<NodeEntry, Single> proximityQueue = new PriorityQueue<NodeEntry, Single>();
            List<Record> results = new List<Record>(kNN.K);
            EnqueNodeEntries(kNN, node, proximityQueue);
            while (results.Count < kNN.K && proximityQueue.Count > 0)
            {
                NodeEntry closestEntry = proximityQueue.Dequeue().Value;
                if (closestEntry is LeafEntry)
                {
                    Record closestRecord = (Record)closestEntry.Child;
                    results.Add(closestRecord);
                }
                else
                {
                    Node closestNode = (Node)closestEntry.Child;
                    EnqueNodeEntries(kNN, closestNode, proximityQueue);
                }
            }
            for(int i = 0; i < results.Count; i++)
                for(int j = i; j < results.Count; j++)
            {
                if (results[i].BoundingBox.MinX == results[j].BoundingBox.MinX &&
                    results[i].BoundingBox.MinY == results[j].BoundingBox.MinY &&
                    results[i].BoundingBox.MaxX == results[j].BoundingBox.MaxX &&
                    results[i].BoundingBox.MaxY == results[j].BoundingBox.MaxY)
                {
                    if (results[i].RecordID.CompareTo(results[j].RecordID) > 0)
                    {
                        Record temp = results[i];
                        results[i] = results[j];
                        results[j] = temp;
                    }
                }
                else
                    break;
            }
            return results;
        }
        protected virtual List<Node> Split(Node nodeToBeSplit)
        {
            List<NodeEntry> entries = new List<NodeEntry>(nodeToBeSplit.NodeEntries);
            List<NodeEntry> seeds = QuadraticPickSeeds(entries);
            //List<NodeEntry> seeds = LinearPickSeeds(entries);
            entries.Remove(seeds[0]);
            entries.Remove(seeds[1]);
            Node node1, node2;
            if (nodeToBeSplit is Leaf)
            {
                node1 = new Leaf(nodeToBeSplit.Parent);
                node2 = new Leaf(nodeToBeSplit.Parent);
            }
            else
            {
                node1 = new Node(nodeToBeSplit.Parent, nodeToBeSplit.ChildType);
                node2 = new Node(nodeToBeSplit.Parent, nodeToBeSplit.ChildType);
            }
            node1.AddNodeEntry(seeds[0]);
            node2.AddNodeEntry(seeds[1]);
            if (!(seeds[0] is LeafEntry))
            {
                Node child = (Node)seeds[0].Child;
                child.Parent = node1;
            }
            if (!(seeds[1] is LeafEntry))
            {
                Node child = (Node)seeds[1].Child;
                child.Parent = node2;
            }
            while (entries.Count > 0)
            {
                if (node1.NodeEntries.Count + entries.Count == Constants.MINIMUM_ENTRIES_PER_NODE)
                {
                    foreach (NodeEntry entry in entries)
                    {
                        node1.AddNodeEntry(entry);
                        if(!(entry is LeafEntry))
                        {
                            Node child = (Node)entry.Child;
                            child.Parent = node1;
                        }
                    }
                    break;
                }
                else if (node2.NodeEntries.Count + entries.Count == Constants.MINIMUM_ENTRIES_PER_NODE)
                {
                    foreach (NodeEntry entry in entries)
                    {
                        node2.AddNodeEntry(entry);
                        if (!(entry is LeafEntry))
                        {
                            Node child = (Node)entry.Child;
                            child.Parent = node2;
                        }
                    }
                    break;
                }
                MinimumBoundingBox minimumBoundingBox1 = node1.CalculateMinimumBoundingBox(),
                minimumBoundingBox2 = node2.CalculateMinimumBoundingBox();
                NodeEntry nextEntry = PickNext(entries, minimumBoundingBox1, minimumBoundingBox2);
                entries.Remove(nextEntry);
                Node nodeToEnter;
                if (GetFutureSize(nextEntry.MinimumBoundingBox, minimumBoundingBox1) ==
                    GetFutureSize(nextEntry.MinimumBoundingBox, minimumBoundingBox2))
                {
                    if (minimumBoundingBox1.GetArea() == minimumBoundingBox2.GetArea())
                        if (node1.NodeEntries.Count <= node2.NodeEntries.Count)
                            nodeToEnter = node1;
                        else
                            nodeToEnter = node2;
                    else if (minimumBoundingBox1.GetArea() < minimumBoundingBox2.GetArea())
                        nodeToEnter = node1;
                    else
                        nodeToEnter = node2;
                }
                else if (GetFutureSize(nextEntry.MinimumBoundingBox, minimumBoundingBox1) <
                    GetFutureSize(nextEntry.MinimumBoundingBox, minimumBoundingBox2))
                    nodeToEnter = node1;
                else
                    nodeToEnter = node2;
                nodeToEnter.AddNodeEntry(nextEntry);

                if (!(nextEntry is LeafEntry))
                {
                    Node child = (Node)nextEntry.Child;
                    child.Parent = nodeToEnter;
                }
            }
            List<Node> newNodes = new List<Node>();
            newNodes.Add(node1);
            newNodes.Add(node2);
            return newNodes;
        }

        #endregion
    }
}
