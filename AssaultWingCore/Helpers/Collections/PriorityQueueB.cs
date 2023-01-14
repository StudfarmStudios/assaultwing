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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace AW2.Helpers.Collections
{
    /// <remarks>Part of Gustavo Franco's A* pathfinding tutorial.</remarks>
    /// <seealso cref="http://www.codeguru.com/csharp/csharp/cs_misc/designtechniques/article.php/c12527/AStar-A-Implementation-in-C-Path-Finding-PathFinder.htm"/>
    public class PriorityQueueB<T>
    {
        #region Variables Declaration
        protected List<T> InnerList = new List<T>();
        protected IComparer<T> mComparer;
        #endregion

        #region Contructors
        public PriorityQueueB()
        {
            mComparer = Comparer<T>.Default;
        }

        public PriorityQueueB(IComparer<T> comparer)
        {
            mComparer = comparer;
        }

        public PriorityQueueB(IComparer<T> comparer, int capacity)
        {
            mComparer = comparer;
            InnerList.Capacity = capacity;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Push an object onto the PQ
        /// </summary>
        /// <param name="O">The new object</param>
        /// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
        public int Push(T item)
        {
            int p = InnerList.Count, p2;
            InnerList.Add(item); // E[p] = O
            do
            {
                if (p == 0)
                    break;
                p2 = (p - 1) / 2;
                if (mComparer.Compare(InnerList[p], InnerList[p2]) < 0)
                {
                    var swap = InnerList[p];
                    InnerList[p] = InnerList[p2];
                    InnerList[p2] = swap;
                    p = p2;
                }
                else
                    break;
            } while (true);
            return p;
        }

        /// <summary>
        /// Get the smallest object and remove it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Pop()
        {
            T result = InnerList[0];
            int p = 0, p1, p2, pn;
            var innerListCount = InnerList.Count - 1;
            InnerList[0] = InnerList[innerListCount];
            InnerList.RemoveAt(innerListCount);
            do
            {
                pn = p;
                p1 = 2 * p + 1;
                p2 = 2 * p + 2;
                if (innerListCount > p1 && mComparer.Compare(InnerList[p], InnerList[p1]) > 0) // links kleiner
                    p = p1;
                if (innerListCount > p2 && mComparer.Compare(InnerList[p], InnerList[p2]) > 0) // rechts noch kleiner
                    p = p2;

                if (p == pn)
                    break;
                var swap = InnerList[p];
                InnerList[p] = InnerList[pn];
                InnerList[pn] = swap;
            } while (true);

            return result;
        }

        /// <summary>
        /// Get the smallest object without removing it.
        /// </summary>
        /// <returns>The smallest object</returns>
        public T Peek()
        {
            if (InnerList.Count > 0)
                return InnerList[0];
            return default(T);
        }

        public void Clear()
        {
            InnerList.Clear();
        }

        public int Count
        {
            get { return InnerList.Count; }
        }
        #endregion
    }
}
