//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
//
//  Email:  gustavo_franco@hotmail.com
//
//  Copyright (C) 2006 Franco, Gustavo 
//
#if DEBUG
#define DEBUGON
#endif

using System;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Collections;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AW2.Helpers.Collections;

namespace AW2.Game.Arenas
{
    /// <remarks>
    /// Code adapted from Gustavo Franco's PathFinderFast class. It is part of his
    /// article on CodeGuru.com on 2006-09-06.
    /// </remarks>
    /// <seealso cref="http://www.codeguru.com/csharp/csharp/cs_misc/designtechniques/article.php/c12527/AStar-A-Implementation-in-C-Path-Finding-PathFinder.htm"/>
    public class PathFinderAStar
    {
        #region Structs
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PathFinderNodeFast
        {
            #region Variables Declaration
            public float F; // f = gone + heuristic
            public float G;
            public ushort PX; // Parent
            public ushort PY;
            public byte Status;
            #endregion
        }

        public struct PathFinderNode
        {
            #region Variables Declaration
            public float F;
            public float G;
            public float H;  // f = gone + heuristic
            public int X;
            public int Y;
            public int PX; // Parent
            public int PY;
            #endregion
        }
        #endregion

        #region Enum
        public enum PathFinderNodeType
        {
            Start = 1,
            End = 2,
            Open = 4,
            Close = 8,
            Current = 16,
            Path = 32
        }

        public enum HeuristicFormula
        {
            Manhattan = 1,
            MaxDXDY = 2,
            DiagonalShortCut = 3,
            Euclidean = 4,
            EuclideanNoSQR = 5,
            Custom1 = 6
        }
        #endregion

        #region Events
        public delegate void PathFinderDebugHandler(int fromX, int fromY, int x, int y, PathFinderNodeType type, float totalCost, float cost);
        public event PathFinderDebugHandler PathFinderDebug;
        #endregion

        private static readonly sbyte[,] g_direction = new sbyte[8, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 }, { 1, -1 }, { 1, 1 }, { -1, 1 }, { -1, -1 } };

        #region Variables Declaration
        // Heap variables are initializated to default, but I like to do it anyway
        private byte[,] mGrid = null;
        private PriorityQueueB<int> mOpen = null;
        private List<PathFinderNode> mClose = new List<PathFinderNode>();
        private bool mStop = false;
        private bool mStopped = true;
        private float mHEstimate = 2;
        private int mSearchLimit = 2000;
        private bool mDebugProgress = false;
        private bool mDebugFoundPath = false;
        private PathFinderNodeFast[] mCalcGrid = null;
        private byte mOpenNodeValue = 1;
        private byte mCloseNodeValue = 2;

        //Promoted local variables to member variables to avoid recreation between calls
        private float mH = 0;
        private int mLocation = 0;
        private int mNewLocation = 0;
        private ushort mLocationX = 0;
        private ushort mLocationY = 0;
        private ushort mNewLocationX = 0;
        private ushort mNewLocationY = 0;
        private int mCloseNodeCounter = 0;
        private ushort mGridX = 0;
        private ushort mGridY = 0;
        private ushort mGridXMinus1 = 0;
        private ushort mGridYLog2 = 0;
        private bool mFound = false;
        private int mEndLocation = 0;
        private float mNewG = 0;
        #endregion

        #region Constructors
        public PathFinderAStar(byte[,] grid)
        {
            if (grid == null)
                throw new Exception("Grid cannot be null");

            mGrid = grid;
            mGridX = (ushort)(mGrid.GetUpperBound(0) + 1);
            mGridY = (ushort)(mGrid.GetUpperBound(1) + 1);
            mGridXMinus1 = (ushort)(mGridX - 1);
            mGridYLog2 = (ushort)Math.Log(mGridY, 2);

            // This should be done at the constructor, for now we leave it here.
            if (Math.Log(mGridX, 2) != (int)Math.Log(mGridX, 2) ||
                Math.Log(mGridY, 2) != (int)Math.Log(mGridY, 2))
                throw new Exception("Invalid Grid, size in X and Y must be power of 2");

            if (mCalcGrid == null || mCalcGrid.Length != (mGridX * mGridY))
                mCalcGrid = new PathFinderNodeFast[mGridX * mGridY];

            mOpen = new PriorityQueueB<int>(new ComparePFNodeMatrix(mCalcGrid));
        }
        #endregion

        #region Properties
        public bool Stopped
        {
            get { return mStopped; }
        }

        public float HeuristicEstimate
        {
            get { return mHEstimate; }
            set { mHEstimate = value; }
        }

        public int SearchLimit
        {
            get { return mSearchLimit; }
            set { mSearchLimit = value; }
        }

        public bool DebugProgress
        {
            get { return mDebugProgress; }
            set { mDebugProgress = value; }
        }

        public bool DebugFoundPath
        {
            get { return mDebugFoundPath; }
            set { mDebugFoundPath = value; }
        }
        #endregion

        #region Methods
        public void FindPathStop()
        {
            mStop = true;
        }

        public List<PathFinderNode> FindPath(Point start, Point end)
        {
            lock (this)
            {
                mFound = false;
                mStop = false;
                mStopped = false;
                mCloseNodeCounter = 0;
                mOpenNodeValue += 2;
                mCloseNodeValue += 2;
                mOpen.Clear();
                mClose.Clear();

#if DEBUGON
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, start.X, start.Y, PathFinderNodeType.Start, -1, -1);
                if (mDebugProgress && PathFinderDebug != null)
                    PathFinderDebug(0, 0, end.X, end.Y, PathFinderNodeType.End, -1, -1);
#endif

                mLocation = (start.Y << mGridYLog2) + start.X;
                mEndLocation = (end.Y << mGridYLog2) + end.X;
                mCalcGrid[mLocation].G = 0;
                mCalcGrid[mLocation].F = mHEstimate;
                mCalcGrid[mLocation].PX = (ushort)start.X;
                mCalcGrid[mLocation].PY = (ushort)start.Y;
                mCalcGrid[mLocation].Status = mOpenNodeValue;

                mOpen.Push(mLocation);
                while (mOpen.Count > 0 && !mStop)
                {
                    mLocation = mOpen.Pop();

                    //Is it in closed list? means this node was already processed
                    if (mCalcGrid[mLocation].Status == mCloseNodeValue)
                        continue;

                    mLocationX = (ushort)(mLocation & mGridXMinus1);
                    mLocationY = (ushort)(mLocation >> mGridYLog2);

#if DEBUGON
                    if (mDebugProgress && PathFinderDebug != null)
                        PathFinderDebug(0, 0, mLocation & mGridXMinus1, mLocation >> mGridYLog2, PathFinderNodeType.Current, -1, -1);
#endif

                    if (mLocation == mEndLocation)
                    {
                        mCalcGrid[mLocation].Status = mCloseNodeValue;
                        mFound = true;
                        break;
                    }

                    if (mCloseNodeCounter > mSearchLimit)
                    {
                        mStopped = true;
                        return null;
                    }

                    //Lets calculate each successors
                    for (int i = 0; i < 8; i++)
                    {
                        mNewLocationX = (ushort)(mLocationX + g_direction[i, 0]);
                        mNewLocationY = (ushort)(mLocationY + g_direction[i, 1]);
                        mNewLocation = (mNewLocationY << mGridYLog2) + mNewLocationX;

                        if (mNewLocationX >= mGridX || mNewLocationY >= mGridY)
                            continue;

                        // Unbreakeable?
                        if (mGrid[mNewLocationX, mNewLocationY] == 0)
                            continue;

                        if (i > 3)
                            mNewG = mCalcGrid[mLocation].G + mGrid[mNewLocationX, mNewLocationY] * 1.4142135623730950488016887242097f;
                        else
                            mNewG = mCalcGrid[mLocation].G + mGrid[mNewLocationX, mNewLocationY];

                        //Is it open or closed?
                        if (mCalcGrid[mNewLocation].Status == mOpenNodeValue || mCalcGrid[mNewLocation].Status == mCloseNodeValue)
                        {
                            // The current node has less code than the previous? then skip this node
                            if (mCalcGrid[mNewLocation].G <= mNewG)
                                continue;
                        }

                        mCalcGrid[mNewLocation].PX = mLocationX;
                        mCalcGrid[mNewLocation].PY = mLocationY;
                        mCalcGrid[mNewLocation].G = mNewG;

                        mH = (float)(mHEstimate * Math.Sqrt(Math.Pow((mNewLocationX - end.X), 2) + Math.Pow((mNewLocationY - end.Y), 2)));
                        mCalcGrid[mNewLocation].F = mNewG + mH;

#if DEBUGON
                        if (mDebugProgress && PathFinderDebug != null)
                            PathFinderDebug(mLocationX, mLocationY, mNewLocationX, mNewLocationY, PathFinderNodeType.Open, mCalcGrid[mNewLocation].F, mCalcGrid[mNewLocation].G);
#endif
                        mOpen.Push(mNewLocation);
                        mCalcGrid[mNewLocation].Status = mOpenNodeValue;
                    }

                    mCloseNodeCounter++;
                    mCalcGrid[mLocation].Status = mCloseNodeValue;

#if DEBUGON
                    if (mDebugProgress && PathFinderDebug != null)
                        PathFinderDebug(0, 0, mLocationX, mLocationY, PathFinderNodeType.Close, mCalcGrid[mLocation].F, mCalcGrid[mLocation].G);
#endif
                }

                if (mFound)
                {
                    mClose.Clear();
                    int posX = end.X;
                    int posY = end.Y;

                    PathFinderNodeFast fNodeTmp = mCalcGrid[(end.Y << mGridYLog2) + end.X];
                    PathFinderNode fNode;
                    fNode.F = fNodeTmp.F;
                    fNode.G = fNodeTmp.G;
                    fNode.H = 0;
                    fNode.PX = fNodeTmp.PX;
                    fNode.PY = fNodeTmp.PY;
                    fNode.X = end.X;
                    fNode.Y = end.Y;

                    while (fNode.X != fNode.PX || fNode.Y != fNode.PY)
                    {
                        mClose.Add(fNode);
#if DEBUGON
                        if (mDebugFoundPath && PathFinderDebug != null)
                            PathFinderDebug(fNode.PX, fNode.PY, fNode.X, fNode.Y, PathFinderNodeType.Path, fNode.F, fNode.G);
#endif
                        posX = fNode.PX;
                        posY = fNode.PY;
                        fNodeTmp = mCalcGrid[(posY << mGridYLog2) + posX];
                        fNode.F = fNodeTmp.F;
                        fNode.G = fNodeTmp.G;
                        fNode.H = 0;
                        fNode.PX = fNodeTmp.PX;
                        fNode.PY = fNodeTmp.PY;
                        fNode.X = posX;
                        fNode.Y = posY;
                    }

                    mClose.Add(fNode);
#if DEBUGON
                    if (mDebugFoundPath && PathFinderDebug != null)
                        PathFinderDebug(fNode.PX, fNode.PY, fNode.X, fNode.Y, PathFinderNodeType.Path, fNode.F, fNode.G);
#endif

                    mStopped = true;
                    return mClose;
                }
                mStopped = true;
                return null;
            }
        }
        #endregion

        #region Inner Classes
        internal class ComparePFNodeMatrix : IComparer<int>
        {
            private PathFinderNodeFast[] mMatrix;

            public ComparePFNodeMatrix(PathFinderNodeFast[] matrix)
            {
                mMatrix = matrix;
            }

            public int Compare(int a, int b)
            {
                return mMatrix[a].F.CompareTo(mMatrix[b].F);
            }
        }
        #endregion
    }
}
