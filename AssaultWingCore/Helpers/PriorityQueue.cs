using System;
using System.Collections;

/*
 * Contents of this file are snatched from
 * http://www.codeproject.com/KB/recipes/priorityqueue.aspx
 * on 2008-02-28. The interface IPriorityQueue and the class PriorityQueue
 * are coded by the www.codeproject.com member BenDi whose real name is
 * unknown and who didn't specify a licence for his code.
 */

namespace AW2.Helpers
{
    /// <summary>
    /// Priority queue interface.
    /// </summary>
	public interface IPriorityQueue : ICollection, ICloneable, IList
	{
        /// <summary>
        /// Push an object onto the PQ
        /// </summary>
        /// <param name="O">The new object</param>
        /// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
        int Push(object O);

        /// <summary>
        /// Get the smallest object and remove it.
        /// </summary>
        /// <returns>The smallest object</returns>
        object Pop();

        /// <summary>
        /// Get the smallest object without removing it.
        /// </summary>
        /// <returns>The smallest object</returns>
        object Peek();

        /// <summary>
        /// Notify the PQ that the object at position i has changed
        /// and the PQ needs to restore order.
        /// Since you dont have access to any indexes (except by using the
        /// explicit IList.this) you should not call this function without knowing exactly
        /// what you do.
        /// </summary>
        /// <param name="i">The index of the changed object.</param>
        void Update(int i);
	}
    
    /// <summary>
    /// Priority queue.
    /// </summary>
    public class BinaryPriorityQueue : IPriorityQueue, ICollection, ICloneable, IList
	{
        /// <summary>
        /// The inner list.
        /// </summary>
		protected ArrayList InnerList = new ArrayList();

        /// <summary>
        /// Comparer of inner list items.
        /// </summary>
        protected IComparer Comparer;

		#region contructors
        /// <summary>
        /// Creates a priority queue.
        /// </summary>
		public BinaryPriorityQueue() : this(System.Collections.Comparer.Default)
		{}
        /// <summary>
        /// Creates a priority queue with a custom comparer.
        /// </summary>
        public BinaryPriorityQueue(IComparer c)
		{
			Comparer = c;
		}
        /// <summary>
        /// Creates a priority queue with initial capacity.
        /// </summary>
		public BinaryPriorityQueue(int C) : this(System.Collections.Comparer.Default,C)
		{}
        /// <summary>
        /// Creates a priority queue with a custom comparer and initial capacity.
        /// </summary>
        public BinaryPriorityQueue(IComparer c, int Capacity)
		{
			Comparer = c;
			InnerList.Capacity = Capacity;
		}

        /// <summary>
        /// Creates a priority queue around an inner list using a custom comparer,
        /// optionally cloning the inner list.
        /// </summary>
        protected BinaryPriorityQueue(ArrayList Core, IComparer Comp, bool Copy)
		{
			if(Copy)
				InnerList = Core.Clone() as ArrayList;
			else
				InnerList = Core;
			Comparer = Comp;
		}

		#endregion
        /// <summary>
        /// Switches the place of two elements in the inner list.
        /// </summary>
        protected void SwitchElements(int i, int j)
		{
			object h = InnerList[i];
			InnerList[i] = InnerList[j];
			InnerList[j] = h;
		}

        /// <summary>
        /// Compares two elements in the inner list.
        /// </summary>
        protected virtual int OnCompare(int i, int j)
		{
			return Comparer.Compare(InnerList[i],InnerList[j]);
		}

		#region public methods
		/// <summary>
		/// Push an object onto the PQ
		/// </summary>
		/// <param name="O">The new object</param>
		/// <returns>The index in the list where the object is _now_. This will change when objects are taken from or put onto the PQ.</returns>
		public int Push(object O)
		{
			int p = InnerList.Count,p2;
			InnerList.Add(O); // E[p] = O
			do
			{
				if(p==0)
					break;
				p2 = (p-1)/2;
				if(OnCompare(p,p2)<0)
				{
					SwitchElements(p,p2);
					p = p2;
				}
				else
					break;
			}while(true);
			return p;
		}

		/// <summary>
		/// Get the smallest object and remove it.
		/// </summary>
		/// <returns>The smallest object</returns>
		public object Pop()
		{
			object result = InnerList[0];
			int p = 0,p1,p2,pn;
			InnerList[0] = InnerList[InnerList.Count-1];
			InnerList.RemoveAt(InnerList.Count-1);
			do
			{
				pn = p;
				p1 = 2*p+1;
				p2 = 2*p+2;
				if(InnerList.Count>p1 && OnCompare(p,p1)>0) // links kleiner
					p = p1;
				if(InnerList.Count>p2 && OnCompare(p,p2)>0) // rechts noch kleiner
					p = p2;
				
				if(p==pn)
					break;
				SwitchElements(p,pn);
			}while(true);
			return result;
		}

		/// <summary>
		/// Notify the PQ that the object at position i has changed
		/// and the PQ needs to restore order.
		/// Since you dont have access to any indexes (except by using the
		/// explicit IList.this) you should not call this function without knowing exactly
		/// what you do.
		/// </summary>
		/// <param name="i">The index of the changed object.</param>
		public void Update(int i)
		{
			int p = i,pn;
			int p1,p2;
			do	// aufsteigen
			{
				if(p==0)
					break;
				p2 = (p-1)/2;
				if(OnCompare(p,p2)<0)
				{
					SwitchElements(p,p2);
					p = p2;
				}
				else
					break;
			}while(true);
			if(p<i)
				return;
			do	   // absteigen
			{
				pn = p;
				p1 = 2*p+1;
				p2 = 2*p+2;
				if(InnerList.Count>p1 && OnCompare(p,p1)>0) // links kleiner
					p = p1;
				if(InnerList.Count>p2 && OnCompare(p,p2)>0) // rechts noch kleiner
					p = p2;
				
				if(p==pn)
					break;
				SwitchElements(p,pn);
			}while(true);
		}

		/// <summary>
		/// Get the smallest object without removing it.
		/// </summary>
		/// <returns>The smallest object</returns>
		public object Peek()
		{
			if(InnerList.Count>0)
				return InnerList[0];
			return null;
		}

        /// <summary>
        /// Returns true iff the given value is contained in the priority queue.
        /// </summary>
        public bool Contains(object value)
		{
			return InnerList.Contains(value);
		}

        /// <summary>
        /// Removes all elements from the priority queue.
        /// </summary>
        public void Clear()
		{
			InnerList.Clear();
		}

        /// <summary>
        /// The number of elements in the priority queue.
        /// </summary>
		public int Count
		{
			get
			{
				return InnerList.Count;
			}
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return InnerList.GetEnumerator();
		}

        /// <summary>
        /// Copies the elements in the priority queue to the given array,
        /// starting from an index.
        /// </summary>
		public void CopyTo(Array array, int index)
		{
			InnerList.CopyTo(array,index);
		}

        /// <summary>
        /// Returns a clone of the priority queue.
        /// </summary>
		public object Clone()
		{
			return new BinaryPriorityQueue(InnerList,Comparer,true);	
		}

        /// <summary>
        /// Is access to the priority queue synchronized (thread safe).
        /// </summary>
		public bool IsSynchronized
		{
			get
			{
				return InnerList.IsSynchronized;
			}
		}

        /// <summary>
        /// Synchronization root of the priority queue.
        /// </summary>
		public object SyncRoot
		{
			get
			{
				return this;
			}
		}
		#endregion
		#region explicit implementation
		bool IList.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		object IList.this[int index]
		{
			get
			{
				return InnerList[index];
			}
			set
			{
				InnerList[index] = value;
				Update(index);
			}
		}

		int IList.Add(object o)
		{
			return Push(o);
		}

		void IList.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		void IList.Insert(int index, object value)
		{
			throw new NotSupportedException();
		}

		void IList.Remove(object value)
		{
			throw new NotSupportedException();
		}

		int IList.IndexOf(object value)
		{
			throw new NotSupportedException();
		}

		bool IList.IsFixedSize
		{
			get
			{
				return false;
			}
		}

        /// <summary>
        /// Returns a priority queue wrapper that is synchronized.
        /// </summary>
		public static BinaryPriorityQueue Synchronized(BinaryPriorityQueue P)
		{
			return new BinaryPriorityQueue(ArrayList.Synchronized(P.InnerList),P.Comparer,false);
		}
        /// <summary>
        /// Returns a read-only priority queue wrapper.
        /// </summary>
		public static BinaryPriorityQueue ReadOnly(BinaryPriorityQueue P)
		{
			return new BinaryPriorityQueue(ArrayList.ReadOnly(P.InnerList),P.Comparer,false);
		}
		#endregion
	}
}
