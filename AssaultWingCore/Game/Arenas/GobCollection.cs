using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Collections;

namespace AW2.Game.Arenas
{
    /// <summary>
    /// A collection of gobs in an arena. The collection reflects a list of arena layers.
    /// If the arena layer contents change, the <see cref="GobCollection"/> changes
    /// and vice versa, operations on the <see cref="GobCollection"/> are forwarded
    /// to the underlying arena layers.
    /// </summary>
    public class GobCollection : IEnumerable<Gob>, IObservableCollection<object, Gob>
    {
        /// <summary>
        /// The arena layers that contain the gobs.
        /// </summary>
        private IList<ArenaLayer> _arenaLayers;

        /// <summary>
        /// The arena layer where the gameplay takes place.
        /// </summary>
        /// <seealso cref="_arenaLayers"/>
        public ArenaLayer GameplayLayer { get; set; }

        /// <summary>
        /// The arena layer right behind the gameplay layer.
        /// </summary>
        /// <seealso cref="GameplayLayer"/>
        public ArenaLayer GameplayBackLayer { get; set; }

        /// <summary>
        /// Creates a new gob collection that reflects the gobs on some arena layers.
        /// </summary>
        public GobCollection(IList<ArenaLayer> arenaLayers)
        {
            if (arenaLayers == null) throw new ArgumentNullException();
            _arenaLayers = arenaLayers;
        }

        /// <summary>
        /// Removes items that satisfy a condition.
        /// </summary>
        /// <param name="condition">The condition by which to remove items.</param>
        public void Remove(Predicate<Gob> condition)
        {
            foreach (var layer in _arenaLayers) layer.Gobs.Remove(condition);
        }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        /// <param name="gob">The element to remove.</param>
        /// <param name="force">Remove the element regardless of how 
        /// <see cref="Removing"/> evaluates on the element.</param>
        public bool Remove(Gob gob, bool force)
        {
            if (gob.Layer == null) return false;
            if (Removing != null && !Removing(gob) && !force) return false;
            return gob.Layer.Gobs.Remove(gob);
        }

        /// <summary>
        /// Called before a single item is removed from the collection.
        /// If the event returns <c>false</c> the removal will not proceed
        /// and the item will stay in the collection.
        /// </summary>
        public event Predicate<Gob> Removing;

        #region IObservableCollection<object, Gob> Members

        public event Action<Gob> Added
        {
            add { foreach (var layer in _arenaLayers) layer.Gobs.Added += value; }
            remove { foreach (var layer in _arenaLayers) layer.Gobs.Added -= value; }
        }
        public event Action<Gob> Removed
        {
            add { foreach (var layer in _arenaLayers) layer.Gobs.Removed += value; }
            remove { foreach (var layer in _arenaLayers) layer.Gobs.Removed -= value; }
        }
        public event Action<IEnumerable<Gob>> Cleared
        {
            add { foreach (var layer in _arenaLayers) layer.Gobs.Cleared += value; }
            remove { foreach (var layer in _arenaLayers) layer.Gobs.Cleared -= value; }
        }
        public event Func<object, Gob> NotFound
        {
            add { foreach (var layer in _arenaLayers) layer.Gobs.NotFound += value; }
            remove { foreach (var layer in _arenaLayers) layer.Gobs.NotFound -= value; }
        }

        #endregion

        #region ICollection<Gob> Members

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        public void Add(Gob gob)
        {
            if (gob.Layer == null)
                gob.Layer = gob.LayerPreference == Gob.LayerPreferenceType.Front
                    ? GameplayLayer
                    : GameplayBackLayer;
            gob.Layer.Gobs.Add(gob);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            foreach (var layer in _arenaLayers) layer.Gobs.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        public bool Contains(Gob gob)
        {
            return gob.Layer.Gobs.Contains(gob);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        public void CopyTo(Gob[] array, int arrayIndex)
        {
            foreach (var layer in _arenaLayers)
            {
                layer.Gobs.CopyTo(array, arrayIndex);
                arrayIndex += layer.Gobs.Count;
            }
        }

        /// <summary>
        /// The number of elements contained in the collection.
        /// </summary>
        public int Count { get { return _arenaLayers.Sum(layer => layer.Gobs.Count); } }

        /// <summary>
        /// Is the collection read-only.
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        public bool Remove(Gob gob)
        {
            return Remove(gob, false);
        }

        #endregion

        #region IEnumerable<Gob> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public IEnumerator<Gob> GetEnumerator()
        {
            foreach (var layer in _arenaLayers)
                foreach (var gob in layer.Gobs)
                    yield return gob;
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
