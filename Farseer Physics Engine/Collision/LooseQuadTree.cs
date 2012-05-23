using System;
using System.Collections.Generic;
using FarseerPhysics.Collision;
using Microsoft.Xna.Framework;

public class LooseElement<T>
{
    public LooseQuadTree<T> Parent;
    public AABB Span;
    public T Value;

    public LooseElement(T value, AABB span)
    {
        Span = span;
        Value = value;
        Parent = null;
    }
}

public class LooseQuadTree<T>
{
    public int MaxBucket;
    public int MaxDepth;
    public List<LooseElement<T>> Nodes;
    public AABB Span; // fat span
    public LooseQuadTree<T>[] SubTrees;

    public LooseQuadTree(AABB span, int maxbucket, int maxdepth)
    {
        Span = span;
        Nodes = new List<LooseElement<T>>();

        MaxBucket = maxbucket;
        MaxDepth = maxdepth;
    }

    public bool IsPartitioned
    {
        get { return SubTrees != null; }
    }

    /// <summary>
    /// Returns the most suitable quadrant of span that entirely contains test. If none, return 0.
    /// </summary>
    private int Partition(AABB span, AABB test)
    {
        var thinSpan = span.Thinned;
        var bestContainment = float.MinValue;
        var bestIndex = -1;
        float distance = float.MinValue;
        distance = thinSpan.Q1.Fattened.ContainmentDistance(ref test);
        if (distance > bestContainment)
        {
            bestContainment = distance;
            bestIndex = 1;
        }
        distance = thinSpan.Q2.Fattened.ContainmentDistance(ref test);
        if (distance > bestContainment)
        {
            bestContainment = distance;
            bestIndex = 2;
        }
        distance = thinSpan.Q3.Fattened.ContainmentDistance(ref test);
        if (distance > bestContainment)
        {
            bestContainment = distance;
            bestIndex = 3;
        }
        distance = thinSpan.Q4.Fattened.ContainmentDistance(ref test);
        if (distance > bestContainment)
        {
            bestContainment = distance;
            bestIndex = 4;
        }
        return bestContainment >= 0 ? bestIndex : 0;
    }

    public void AddNode(LooseElement<T> node)
    {
        if (!IsPartitioned)
        {
            if (Nodes.Count >= MaxBucket && MaxDepth > 0) //bin is full and can still subdivide
            {
                //
                //partition into quadrants and sort existing nodes amonst quads.
                //
                Nodes.Add(node); //treat new node just like other nodes for partitioning

                SubTrees = new LooseQuadTree<T>[4];
                var thinSpan = Span.Thinned;
                SubTrees[0] = new LooseQuadTree<T>(thinSpan.Q1.Fattened, MaxBucket, MaxDepth - 1);
                SubTrees[1] = new LooseQuadTree<T>(thinSpan.Q2.Fattened, MaxBucket, MaxDepth - 1);
                SubTrees[2] = new LooseQuadTree<T>(thinSpan.Q3.Fattened, MaxBucket, MaxDepth - 1);
                SubTrees[3] = new LooseQuadTree<T>(thinSpan.Q4.Fattened, MaxBucket, MaxDepth - 1);

                List<LooseElement<T>> remNodes = new List<LooseElement<T>>();
                //nodes that are not fully contained by any quadrant

                foreach (LooseElement<T> n in Nodes)
                {
                    switch (Partition(Span, n.Span))
                    {
                        case 1: //quadrant 1
                            SubTrees[0].AddNode(n);
                            break;
                        case 2:
                            SubTrees[1].AddNode(n);
                            break;
                        case 3:
                            SubTrees[2].AddNode(n);
                            break;
                        case 4:
                            SubTrees[3].AddNode(n);
                            break;
                        default:
                            n.Parent = this;
                            remNodes.Add(n);
                            break;
                    }
                }

                Nodes = remNodes;
            }
            else
            {
                node.Parent = this;
                Nodes.Add(node);
                //if bin is not yet full or max depth has been reached, just add the node without subdividing
            }
        }
        else //we already have children nodes
        {
            //
            //add node to specific sub-tree
            //
            switch (Partition(Span, node.Span))
            {
                case 1: //quadrant 1
                    SubTrees[0].AddNode(node);
                    break;
                case 2:
                    SubTrees[1].AddNode(node);
                    break;
                case 3:
                    SubTrees[2].AddNode(node);
                    break;
                case 4:
                    SubTrees[3].AddNode(node);
                    break;
                default:
                    node.Parent = this;
                    Nodes.Add(node);
                    break;
            }
        }
    }

    /// <summary>
    /// tests if ray intersects AABB
    /// </summary>
    /// <param name="aabb"></param>
    /// <returns></returns>
    public static bool RayCastAABB(AABB aabb, Vector2 p1, Vector2 p2)
    {
        AABB segmentAABB = new AABB();
        {
            Vector2.Min(ref p1, ref p2, out segmentAABB.LowerBound);
            Vector2.Max(ref p1, ref p2, out segmentAABB.UpperBound);
        }
        if (!AABB.TestOverlap(aabb, segmentAABB)) return false;

        Vector2 rayDir = p2 - p1;
        Vector2 rayPos = p1;

        Vector2 norm = new Vector2(-rayDir.Y, rayDir.X); //normal to ray
        if (norm.Length() == 0.0)
            return true; //if ray is just a point, return true (iff point is within aabb, as tested earlier)
        norm.Normalize();

        float dPos = Vector2.Dot(rayPos, norm);

        Vector2[] verts = aabb.GetVertices();
        float d0 = Vector2.Dot(verts[0], norm) - dPos;
        for (int i = 1; i < 4; i++)
        {
            float d = Vector2.Dot(verts[i], norm) - dPos;
            if (Math.Sign(d) != Math.Sign(d0))
                //return true if the ray splits the vertices (ie: sign of dot products with normal are not all same)
                return true;
        }

        return false;
    }

    public void QueryAABB(Func<LooseElement<T>, bool> callback, ref AABB searchR)
    {
        Stack<LooseQuadTree<T>> stack = new Stack<LooseQuadTree<T>>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            LooseQuadTree<T> qt = stack.Pop();
            if (!AABB.TestOverlap(ref searchR, ref qt.Span))
                continue;

            foreach (LooseElement<T> n in qt.Nodes)
                if (AABB.TestOverlap(ref searchR, ref n.Span))
                {
                    if (!callback(n)) return;
                }

            if (qt.IsPartitioned)
                foreach (LooseQuadTree<T> st in qt.SubTrees)
                    stack.Push(st);
        }
    }

    public void RayCast(Func<RayCastInput, LooseElement<T>, float> callback, ref RayCastInput input)
    {
        Stack<LooseQuadTree<T>> stack = new Stack<LooseQuadTree<T>>();
        stack.Push(this);

        float maxFraction = input.MaxFraction;
        Vector2 p1 = input.Point1;
        Vector2 p2 = p1 + (input.Point2 - input.Point1) * maxFraction;

        while (stack.Count > 0)
        {
            LooseQuadTree<T> qt = stack.Pop();

            if (!RayCastAABB(qt.Span, p1, p2))
                continue;

            foreach (LooseElement<T> n in qt.Nodes)
            {
                if (!RayCastAABB(n.Span, p1, p2))
                    continue;

                RayCastInput subInput;
                subInput.Point1 = input.Point1;
                subInput.Point2 = input.Point2;
                subInput.MaxFraction = maxFraction;

                float value = callback(subInput, n);
                if (value == 0.0f)
                    return; // the client has terminated the raycast.

                if (value <= 0.0f)
                    continue;

                maxFraction = value;
                p2 = p1 + (input.Point2 - input.Point1) * maxFraction; //update segment endpoint
            }
            if (qt.IsPartitioned)
                foreach (LooseQuadTree<T> st in qt.SubTrees)
                    stack.Push(st);
        }
    }

    public void GetAllNodesR(ref List<LooseElement<T>> nodes)
    {
        nodes.AddRange(Nodes);

        if (IsPartitioned)
            foreach (LooseQuadTree<T> st in SubTrees) st.GetAllNodesR(ref nodes);
    }

    public void GetAllSpansR(ref List<Tuple<AABB, int>> spansAndElementCounts)
    {
        spansAndElementCounts.Add(Tuple.Create(Span.Thinned, Nodes.Count));
        if (IsPartitioned)
            foreach (var st in SubTrees) st.GetAllSpansR(ref spansAndElementCounts);
    }

    public void RemoveNode(LooseElement<T> node)
    {
        node.Parent.Nodes.Remove(node);
    }

    public void Reconstruct()
    {
        List<LooseElement<T>> allNodes = new List<LooseElement<T>>();
        GetAllNodesR(ref allNodes);

        Clear();

        allNodes.ForEach(AddNode);
    }

    public void Clear()
    {
        Nodes.Clear();
        SubTrees = null;
    }
}