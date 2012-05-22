using System;
using System.Collections.Generic;
using FarseerPhysics;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

public class LooseQuadTreeBroadPhase : IBroadPhase
{
    private const int TreeUpdateThresh = 10000;
    private int _currID;
    private Dictionary<int, LooseElement<FixtureProxy>> _idRegister;
    private List<LooseElement<FixtureProxy>> _moveBuffer;
    private List<Pair> _pairBuffer;
    private LooseQuadTree<FixtureProxy> _quadTreeNonstatic;
    private LooseQuadTree<FixtureProxy> _quadTreeStatic;
    private int _treeMoveNumNonstatic;
    private int _treeMoveNumStatic;

    /// <summary>
    /// Creates a new quad tree broadphase with the specified span.
    /// </summary>
    /// <param name="span">world size</param>
    public LooseQuadTreeBroadPhase(AABB span)
    {
        var fatSpan = span.Fattened;
        _quadTreeNonstatic = new LooseQuadTree<FixtureProxy>(fatSpan, 5, 10);
        _quadTreeStatic = new LooseQuadTree<FixtureProxy>(fatSpan, 5, 10);
        _idRegister = new Dictionary<int, LooseElement<FixtureProxy>>();
        _moveBuffer = new List<LooseElement<FixtureProxy>>();
        _pairBuffer = new List<Pair>();
    }

    #region IBroadPhase Members

    ///<summary>
    /// The number of proxies
    ///</summary>
    public int ProxyCount
    {
        get { return _idRegister.Count; }
    }

    public void GetFatAABB(int proxyID, out AABB aabb)
    {
        if (_idRegister.ContainsKey(proxyID))
            aabb = _idRegister[proxyID].Span;
        else
            throw new KeyNotFoundException("proxyID not found in register");
    }

    public void UpdatePairs(BroadphaseDelegate callback)
    {
        _pairBuffer.Clear();
        foreach (LooseElement<FixtureProxy> qtnode in _moveBuffer)
        {
            // Query tree, create pairs and add them pair buffer.
            Query(proxyID => PairBufferQueryCallback(proxyID, qtnode.Value.ProxyId), ref qtnode.Span, includeStatic: !qtnode.Value.IsStatic);
        }
        _moveBuffer.Clear();

        // Sort the pair buffer to expose duplicates.
        _pairBuffer.Sort();

        // Send the pairs back to the client.
        int i = 0;
        while (i < _pairBuffer.Count)
        {
            Pair primaryPair = _pairBuffer[i];
            FixtureProxy userDataA = GetProxy(primaryPair.ProxyIdA);
            FixtureProxy userDataB = GetProxy(primaryPair.ProxyIdB);

            callback(ref userDataA, ref userDataB);
            ++i;

            // Skip any duplicate pairs.
            while (i < _pairBuffer.Count && _pairBuffer[i].ProxyIdA == primaryPair.ProxyIdA &&
                   _pairBuffer[i].ProxyIdB == primaryPair.ProxyIdB)
                ++i;
        }
    }

    /// <summary>
    /// Test overlap of fat AABBs.
    /// </summary>
    /// <param name="proxyIdA">The proxy id A.</param>
    /// <param name="proxyIdB">The proxy id B.</param>
    /// <returns></returns>
    public bool TestOverlap(int proxyIdA, int proxyIdB)
    {
        AABB aabb1;
        AABB aabb2;
        GetFatAABB(proxyIdA, out aabb1);
        GetFatAABB(proxyIdB, out aabb2);
        return AABB.TestOverlap(ref aabb1, ref aabb2);
    }

    public int AddProxy(ref FixtureProxy proxy)
    {
        int proxyID = _currID++;
        proxy.ProxyId = proxyID;
        AABB aabb = Fatten(ref proxy.AABB);
        LooseElement<FixtureProxy> qtnode = new LooseElement<FixtureProxy>(proxy, aabb);

        _idRegister.Add(proxyID, qtnode);
        if (proxy.IsStatic)
            _quadTreeStatic.AddNode(qtnode);
        else
            _quadTreeNonstatic.AddNode(qtnode);

        return proxyID;
    }

    public void RemoveProxy(int proxyId)
    {
        if (_idRegister.ContainsKey(proxyId))
        {
            LooseElement<FixtureProxy> qtnode = _idRegister[proxyId];
            UnbufferMove(qtnode);
            _idRegister.Remove(proxyId);
            if (qtnode.Value.IsStatic)
                _quadTreeStatic.RemoveNode(qtnode);
            else
                _quadTreeNonstatic.RemoveNode(qtnode);
        }
        else
            throw new KeyNotFoundException("proxyID not found in register");
    }

    public void MoveProxy(int proxyId, ref AABB aabb, Vector2 displacement)
    {
        AABB fatAABB;
        GetFatAABB(proxyId, out fatAABB);

        //exit if movement is within fat aabb
        if (fatAABB.Contains(ref aabb))
            return;

        // Extend AABB.
        AABB b = aabb;
        Vector2 r = new Vector2(Settings.AABBExtension, Settings.AABBExtension);
        b.LowerBound = b.LowerBound - r;
        b.UpperBound = b.UpperBound + r;

        // Predict AABB displacement.
        Vector2 d = Settings.AABBMultiplier * displacement;

        if (d.X < 0.0f)
            b.LowerBound.X += d.X;
        else
            b.UpperBound.X += d.X;

        if (d.Y < 0.0f)
            b.LowerBound.Y += d.Y;
        else
            b.UpperBound.Y += d.Y;


        LooseElement<FixtureProxy> qtnode = _idRegister[proxyId];
        qtnode.Value.AABB = b; //not neccesary for QTree, but might be accessed externally
        qtnode.Span = b;

        ReinsertNode(qtnode);

        BufferMove(qtnode);
    }

    public FixtureProxy GetProxy(int proxyId)
    {
        if (_idRegister.ContainsKey(proxyId))
            return _idRegister[proxyId].Value;
        else
            throw new KeyNotFoundException("proxyID not found in register");
    }

    public void TouchProxy(int proxyId)
    {
        if (_idRegister.ContainsKey(proxyId))
            BufferMove(_idRegister[proxyId]);
        else
            throw new KeyNotFoundException("proxyID not found in register");
    }

    public void Query(Func<int, bool> callback, ref AABB query)
    {
        Query(callback, ref query, includeStatic: true);
    }

    public void RayCast(Func<RayCastInput, int, float> callback, ref RayCastInput input)
    {
        _quadTreeNonstatic.RayCast(TransformRayCallback(callback), ref input);
        _quadTreeStatic.RayCast(TransformRayCallback(callback), ref input);
    }

    public void GetSpans(ref List<Tuple<AABB, int>> spansAndElementCounts)
    {
        _quadTreeNonstatic.GetAllSpansR(ref spansAndElementCounts);
        _quadTreeStatic.GetAllSpansR(ref spansAndElementCounts);
    }

    #endregion

    private void Query(Func<int, bool> callback, ref AABB query, bool includeStatic)
    {
        _quadTreeNonstatic.QueryAABB(TransformPredicate(callback), ref query);
        if (includeStatic) _quadTreeStatic.QueryAABB(TransformPredicate(callback), ref query);
    }

    private AABB Fatten(ref AABB aabb)
    {
        Vector2 r = new Vector2(Settings.AABBExtension, Settings.AABBExtension);
        return new AABB(aabb.LowerBound - r, aabb.UpperBound + r);
    }

    private Func<LooseElement<FixtureProxy>, bool> TransformPredicate(Func<int, bool> idPredicate)
    {
        Func<LooseElement<FixtureProxy>, bool> qtPred = qtnode => idPredicate(qtnode.Value.ProxyId);
        return qtPred;
    }

    private Func<RayCastInput, LooseElement<FixtureProxy>, float> TransformRayCallback(
        Func<RayCastInput, int, float> callback)
    {
        Func<RayCastInput, LooseElement<FixtureProxy>, float> newCallback =
            (input, qtnode) => callback(input, qtnode.Value.ProxyId);
        return newCallback;
    }

    private bool PairBufferQueryCallback(int proxyID, int baseID)
    {
        // A proxy cannot form a pair with itself.
        if (proxyID == baseID)
            return true;

        Pair p = new Pair();
        p.ProxyIdA = Math.Min(proxyID, baseID);
        p.ProxyIdB = Math.Max(proxyID, baseID);
        _pairBuffer.Add(p);

        return true;
    }

    private void ReconstructTree(bool isStatic)
    {
        var quadTree = isStatic ? _quadTreeStatic : _quadTreeNonstatic;
        //this is faster than quadTree.Reconstruct(), since the quadtree method runs a recusive query to find all nodes.
        quadTree.Clear();
        foreach (LooseElement<FixtureProxy> elem in _idRegister.Values)
            if (elem.Value.IsStatic == isStatic)
                quadTree.AddNode(elem);
    }

    private void ReinsertNode(LooseElement<FixtureProxy> qtnode)
    {
        if (qtnode.Value.IsStatic)
            ReinsertNodeImpl(qtnode, true, ref _treeMoveNumStatic);
        else
            ReinsertNodeImpl(qtnode, false, ref _treeMoveNumNonstatic);
    }

    private void ReinsertNodeImpl(LooseElement<FixtureProxy> qtnode, bool isStatic, ref int treeMoveNum)
    {
        var quadTree = isStatic ? _quadTreeStatic : _quadTreeNonstatic;
        quadTree.RemoveNode(qtnode);
        quadTree.AddNode(qtnode);

        if (++treeMoveNum > TreeUpdateThresh)
        {
            ReconstructTree(isStatic);
            treeMoveNum = 0;
        }
    }

    private void BufferMove(LooseElement<FixtureProxy> proxy)
    {
        _moveBuffer.Add(proxy);
    }

    private void UnbufferMove(LooseElement<FixtureProxy> proxy)
    {
        _moveBuffer.Remove(proxy);
    }
}