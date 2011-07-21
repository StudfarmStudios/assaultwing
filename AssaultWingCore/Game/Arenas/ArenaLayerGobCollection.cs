using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Collections;
using AW2.Helpers.Serialization;

namespace AW2.Game.Arenas
{
    /// <summary>
    /// A collection of gobs in an arena layer.
    /// </summary>
    [SerializedType(typeof(List<Gob>))]
    public class ArenaLayerGobCollection : IList<Gob>, IObservableCollection<object, Gob>
    {
        /// <summary>
        /// Number of simultaneous iterations over the collection.
        /// </summary>
        private int _isEnumerating;

        /// <summary>
        /// Gobs that were scheduled for removal while enumeration was in progress.
        /// </summary>
        private List<Gob> _removedGobs = new List<Gob>();

        /// <summary>
        /// Gobs that were scheduled for addition while enumeration was in progress.
        /// </summary>
        private List<Gob> _addedGobs = new List<Gob>();

        private IList<Gob> gobs = new List<Gob>();

        /// <summary>
        /// Gobs in the arena layer, sorted in 2D draw order from back to front,
        /// exclusive of gobs that are not drawn in 2D.
        /// </summary>
        /// 2D draw order is alphabetic order primarily by decreasing <c>Gob.LayerDepth2D</c>
        /// and secondarily by natural order of <c>Gob.DrawMode2D</c>.
        private IList<Gob> gobsSort2D = new List<Gob>();

        /// <summary>
        /// Explicit conversion to <c>IList&lt;Gob&gt;</c>.
        /// </summary>
        public static explicit operator List<Gob>(ArenaLayerGobCollection gobs)
        {
            return new List<Gob>(gobs);
        }

        /// <summary>
        /// Explicit conversion from <c>IList&lt;Gob&gt;</c>.
        /// </summary>
        public static explicit operator ArenaLayerGobCollection(List<Gob> gobs)
        {
            var collection = new ArenaLayerGobCollection();
            foreach (var gob in gobs) collection.Add(gob);
            return collection;
        }

        /// <summary>
        /// Performs the specified action on each gob on the arena layer,
        /// enumerating the gobs in their 2D draw order from back to front.
        /// </summary>
        /// <param name="action">The Action delegate to perform on each gob.</param>
        public void ForEachIn2DOrder(Action<Gob> action)
        {
            foreach (Gob gob in gobsSort2D)
                action(gob);
        }

        /// <summary>
        /// Removes items that satisfy a condition.
        /// </summary>
        /// <param name="condition">The condition by which to remove items.</param>
        public void Remove(Predicate<Gob> condition)
        {
            if (_isEnumerating > 0)
                _removedGobs.AddRange(gobs.Where(new Func<Gob, bool>(condition)));
            else
            {
                for (int index = gobs.Count - 1; index >= 0; --index)
                    if (condition(gobs[index]))
                        RemoveAt(index);
            }
        }

        #region IList<Gob> Members

        /// <summary>
        /// Determines the index of a specific item in the list.
        /// </summary>
        public int IndexOf(Gob item)
        {
            return gobs.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item to the list at the specified index.
        /// </summary>
        public void Insert(int index, Gob item)
        {
            gobs.Insert(index, item);
            InsertTo2DOrder(item);
        }

        /// <summary>
        /// Removes the list item at the specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            Gob removed = gobs[index];
            gobs.RemoveAt(index);
            gobsSort2D.Remove(removed);
        }

        /// <summary>
        /// The list item at the specified index.
        /// </summary>
        public Gob this[int index]
        {
            get { return gobs[index]; }
            set
            {
                gobs[index] = value;
                InsertTo2DOrder(value);
            }
        }

        #endregion

        #region ICollection<Gob> Members

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        public void Add(Gob gob)
        {
            if (_isEnumerating > 0)
                _addedGobs.Add(gob);
            else
            {
                gobs.Add(gob);
                InsertTo2DOrder(gob);
                if (Added != null) Added(gob);
            }
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            if (_isEnumerating > 0)
                _removedGobs.AddRange(gobs);
            else
            {
                gobs.Clear();
                gobsSort2D.Clear();
            }
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        public bool Contains(Gob item)
        {
            return gobs.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        public void CopyTo(Gob[] array, int arrayIndex)
        {
            gobs.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// The number of elements contained in the collection.
        /// </summary>
        public int Count { get { return gobs.Count; } }

        /// <summary>
        /// Is the collection read-only.
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        public bool Remove(Gob gob)
        {
            if (_isEnumerating > 0)
            {
                _removedGobs.Add(gob);
                return gobs.Contains(gob);
            }
            else
            {
                bool success = gobs.Remove(gob);
                if (success)
                {
                    gobsSort2D.Remove(gob);
                    if (Removed != null) Removed(gob);
                }
                return success;
            }
        }

        #endregion

        #region IEnumerable<Gob> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<Gob> GetEnumerator()
        {
            try
            {
                ++_isEnumerating;
                foreach (var gob in gobs)
                    yield return gob;
            }
            finally
            {
                --_isEnumerating;
                if (_isEnumerating == 0 && (_addedGobs.Any() || _removedGobs.Any()))
                {
                    var oldAddedGobs = _addedGobs;
                    var oldRemovedGobs = _removedGobs;
                    _addedGobs = new List<Gob>();
                    _removedGobs = new List<Gob>();
                    foreach (var gob in oldAddedGobs) Add(gob);
                    foreach (var gob in oldRemovedGobs) Remove(gob);
                }
            }
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)gobs).GetEnumerator();
        }

        #endregion

        #region IObservableCollection<object, Gob> Members

        public event Action<Gob> Added;
        public event Action<Gob> Removed;
        public event Action<IEnumerable<Gob>> Cleared
        {
            add { throw new NotImplementedException("ArenaLayerGobCollection.Cleared event is not in use"); }
            remove { throw new NotImplementedException("ArenaLayerGobCollection.Cleared event is not in use"); }
        }
        public event Func<object, Gob> NotFound
        {
            add { throw new NotImplementedException("ArenaLayerGobCollection.NotFound event is not in use"); }
            remove { throw new NotImplementedException("ArenaLayerGobCollection.NotFound event is not in use"); }
        }

        #endregion

        private void InsertTo2DOrder(Gob gob)
        {
            if (gob.DrawMode2D.IsDrawn)
            {
                int index = 0;
                while (index < gobsSort2D.Count
                       && (gob.DepthLayer2D < gobsSort2D[index].DepthLayer2D
                           || (gob.DepthLayer2D == gobsSort2D[index].DepthLayer2D &&
                               gob.DrawMode2D.CompareTo(gobsSort2D[index].DrawMode2D) > 0)))
                    ++index;
                gobsSort2D.Insert(index, gob);
            }
        }
    }
}
