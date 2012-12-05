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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AW2.Helpers;
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

        private static readonly sbyte[,] g_direction = new sbyte[8, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 }, { 1, -1 }, { 1, 1 }, { -1, 1 }, { -1, -1 } };

        #region Variables Declaration
        // Heap variables are initializated to default, but I like to do it anyway
        private byte[,] mGrid = null;
        private PriorityQueueB<int> mOpen = null;
        private bool mStop = false;
        private bool mStopped = true;
        private float mHEstimate = 2;
        private int mSearchLimit = 2000;
        private PathFinderNodeFast[] mCalcGrid = null;
        private byte mOpenNodeValue = 0;
        private byte mCloseNodeValue = 1;

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
        private ushort mGridXLog2 = 0;
        private bool mFound = false;
        private int mEndLocation = 0;
        private float mNewG = 0;
        #endregion

        #region Constructors
        public PathFinderAStar(byte[,] grid)
        {
            if (grid == null) throw new ArgumentNullException();
            mGridX = (ushort)grid.GetLength(0);
            mGridY = (ushort)grid.GetLength(1);
            if (mGridX == 0 || mGridY == 0) throw new ArgumentException("Grid must not be zero-sized");
            mGridXLog2 = (ushort)AWMathHelper.LogTwo(mGridX);
            if (1 << mGridXLog2 != mGridX) throw new ArgumentException("Grid width must be a power of two, not " + mGridX);
            mGrid = grid;
            mGridXMinus1 = (ushort)(mGridX - 1);
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
        #endregion

        #region Methods
        public void FindPathStop()
        {
            mStop = true;
        }

        public List<PathFinderNode> FindPath(int startX, int startY, int endX, int endY)
        {
            mFound = false;
            mStop = false;
            mStopped = false;
            mCloseNodeCounter = 0;
            // As long as mOpenNodeValue and mCloseNodeValue are previously unused as values in
            // mCalGrid[].Status, we don't need to clear mCalcGrid.
            mOpenNodeValue += 2;
            mCloseNodeValue += 2;
            if (mOpenNodeValue == 0 || mCloseNodeValue == 0)
            {
                mOpenNodeValue += 2;
                mCloseNodeValue += 2;
                Array.Clear(mCalcGrid, 0, mCalcGrid.Length);
            }
            mOpen.Clear();
            var closedNodes = new List<PathFinderNode>();

            mLocation = (startY << mGridXLog2) + startX;
            mEndLocation = (endY << mGridXLog2) + endX;
            mCalcGrid[mLocation].G = 0;
            mCalcGrid[mLocation].F = mHEstimate;
            mCalcGrid[mLocation].PX = (ushort)startX;
            mCalcGrid[mLocation].PY = (ushort)startY;
            mCalcGrid[mLocation].Status = mOpenNodeValue;

            mOpen.Push(mLocation);
            while (mOpen.Count > 0 && !mStop)
            {
                mLocation = mOpen.Pop();

                //Is it in closed list? means this node was already processed
                if (mCalcGrid[mLocation].Status == mCloseNodeValue)
                    continue;

                mLocationX = (ushort)(mLocation & mGridXMinus1);
                mLocationY = (ushort)(mLocation >> mGridXLog2);

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
                    mNewLocation = (mNewLocationY << mGridXLog2) + mNewLocationX;

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

                    mH = (float)(mHEstimate * Math.Sqrt(Math.Pow((mNewLocationX - endX), 2) + Math.Pow((mNewLocationY - endY), 2)));
                    mCalcGrid[mNewLocation].F = mNewG + mH;

                    mOpen.Push(mNewLocation);
                    mCalcGrid[mNewLocation].Status = mOpenNodeValue;
                }

                mCloseNodeCounter++;
                mCalcGrid[mLocation].Status = mCloseNodeValue;
            }

            if (mFound)
            {
                closedNodes.Clear();
                int posX = endX;
                int posY = endY;

                PathFinderNodeFast fNodeTmp = mCalcGrid[(endY << mGridXLog2) + endX];
                PathFinderNode fNode;
                fNode.F = fNodeTmp.F;
                fNode.G = fNodeTmp.G;
                fNode.H = 0;
                fNode.PX = fNodeTmp.PX;
                fNode.PY = fNodeTmp.PY;
                fNode.X = endX;
                fNode.Y = endY;

                while (fNode.X != fNode.PX || fNode.Y != fNode.PY)
                {
                    closedNodes.Add(fNode);
                    posX = fNode.PX;
                    posY = fNode.PY;
                    fNodeTmp = mCalcGrid[(posY << mGridXLog2) + posX];
                    fNode.F = fNodeTmp.F;
                    fNode.G = fNodeTmp.G;
                    fNode.H = 0;
                    fNode.PX = fNodeTmp.PX;
                    fNode.PY = fNodeTmp.PY;
                    fNode.X = posX;
                    fNode.Y = posY;
                }

                closedNodes.Add(fNode);

                mStopped = true;
                return closedNodes;
            }
            mStopped = true;
            return null;
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
