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
    public class ArenaLayerGobCollection : IEnumerable<Gob>, IObservableCollection<object, Gob>
    {
        /// <summary>
        /// Gobs that were scheduled for removal while enumeration was in progress.
        /// </summary>
        private List<Gob> _removedGobs = new List<Gob>();

        /// <summary>
        /// Gobs that were scheduled for addition while enumeration was in progress.
        /// </summary>
        private List<Gob> _addedGobs = new List<Gob>();

        private List<Gob> _gobs = new List<Gob>();

        /// <summary>
        /// Gobs in the arena layer, sorted in 2D draw order from back to front,
        /// exclusive of gobs that are not drawn in 2D.
        /// </summary>
        /// 2D draw order is alphabetic order primarily by decreasing <c>Gob.LayerDepth2D</c>
        /// and secondarily by natural order of <c>Gob.DrawMode2D</c>.
        private List<Gob> gobsSort2D = new List<Gob>();

        public int Count { get { return _gobs.Count; } }

        public static explicit operator List<Gob>(ArenaLayerGobCollection gobs)
        {
            return new List<Gob>(gobs._gobs);
        }

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
        public void ForEachIn2DOrder(Action<Gob> action)
        {
            foreach (var gob in gobsSort2D) action(gob);
        }

        public void Add(Gob gob)
        {
            _addedGobs.Add(gob);
            if (Added != null) Added(gob);
        }

        public void Clear()
        {
            _removedGobs.AddRange(_gobs);
        }

        public void Remove(Gob gob)
        {
            _removedGobs.Add(gob);
        }

        /// <summary>
        /// To be called every frame at a time when no-one is operating on the collection.
        /// </summary>
        public void FinishAddsAndRemoves()
        {
            foreach (var gob in _addedGobs) FinishAdd(gob);
            _addedGobs.Clear();
            // Note: Removing a gob may trigger removal of other gobs. Therefore iterate over a copy of _removedGobs.
            var oldRemovedGobs = _removedGobs;
            _removedGobs = new List<Gob>();
            foreach (var gob in oldRemovedGobs) FinishRemove(gob);
        }

        public IEnumerator<Gob> GetEnumerator()
        {
            return _gobs.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)_gobs).GetEnumerator();
        }

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

        private void FinishAdd(Gob gob)
        {
            _gobs.Add(gob);
            InsertTo2DOrder(gob);
        }

        private void FinishRemove(Gob gob)
        {
            if (!_gobs.Remove(gob)) return;
            gobsSort2D.Remove(gob);
            if (Removed != null) Removed(gob);
        }

        private void InsertTo2DOrder(Gob gob)
        {
            if (!gob.DrawMode2D.IsDrawn) return;
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
