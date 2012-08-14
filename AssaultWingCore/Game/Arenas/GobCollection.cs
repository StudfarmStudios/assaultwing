using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Collections;
using AW2.Helpers.Serialization;

namespace AW2.Game.Arenas
{
    /// <summary>
    /// A collection of gobs in an arena. The collection reflects a list of arena layers.
    /// If the arena layer contents change, the <see cref="GobCollection"/> changes
    /// and vice versa, operations on the <see cref="GobCollection"/> are forwarded
    /// to the underlying arena layers.
    /// </summary>
    [LimitedSerialization]
    public class GobCollection : IEnumerable<Gob>, IObservableCollection<object, Gob>
    {
        /// <summary>
        /// The arena layers that contain the gobs.
        /// </summary>
        [RuntimeState]
        private IList<ArenaLayer> _arenaLayers;

        /// <summary>
        /// All gobs in all layers by gob IDs.
        /// </summary>
        private Dictionary<int, Gob> _gobDictionary;

        /// <summary>
        /// The arena layer where the gameplay takes place.
        /// </summary>
        public ArenaLayer GameplayLayer { get; set; }

        /// <summary>
        /// The arena layer right behind the gameplay layer.
        /// </summary>
        public ArenaLayer GameplayBackLayer { get; set; }

        /// <summary>
        /// The arena layer right in front of the gameplay layer.
        /// </summary>
        public ArenaLayer GameplayOverlayLayer { get; set; }

        /// <summary>
        /// Returns the gob with <paramref name="gobID"/> or null if no such gob exists.
        /// </summary>
        public Gob this[int gobID]
        {
            get
            {
                Gob gob;
                _gobDictionary.TryGetValue(gobID, out gob);
                return gob;
            }
        }

        /// <summary>
        /// Creates a new gob collection that reflects the gobs on some arena layers.
        /// </summary>
        public GobCollection(IList<ArenaLayer> arenaLayers)
        {
            if (arenaLayers == null) throw new ArgumentNullException();
            _arenaLayers = arenaLayers;
            _gobDictionary = new Dictionary<int, Gob>();
            // Note: There may be gobs in _arenaLayers but _gobDictionary holds no references to them.
            // This happens only when an arena template is loaded from XML. When an arena is started
            // for playing, all the gobs are added by calling Add() whence _gobDictionary is filled.
        }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        /// <param name="force">Remove the element regardless of how <see cref="Removing"/>
        /// evaluates on the element.</param>
        public void Remove(Gob gob, bool force)
        {
            if (gob.Layer == null) return;
            if (Removing != null && !Removing(gob) && !force) return;
            _gobDictionary.Remove(gob.ID);
            gob.Layer.Gobs.Remove(gob);
        }

        /// <summary>
        /// Called before a single item is removed from the collection.
        /// If the event returns <c>false</c> the removal will not proceed
        /// and the item will stay in the collection.
        /// </summary>
        public event Predicate<Gob> Removing;

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

        public void Add(Gob gob)
        {
            if (gob.Layer == null)
                switch (gob.LayerPreference)
                {
                    case Gob.LayerPreferenceType.Front: gob.Layer = GameplayLayer; break;
                    case Gob.LayerPreferenceType.Back: gob.Layer = GameplayBackLayer; break;
                    case Gob.LayerPreferenceType.Overlay: gob.Layer = GameplayOverlayLayer; break;
                    default: throw new ApplicationException("Unexpected layer preference " + gob.LayerPreference);
                }
            gob.Layer.Gobs.Add(gob);
            _gobDictionary.Add(gob.ID, gob);
        }

        public void Clear()
        {
            _gobDictionary.Clear();
            foreach (var layer in _arenaLayers) layer.Gobs.Clear();
        }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        public void Remove(Gob gob)
        {
            Remove(gob, false);
        }

        /// <summary>
        /// To be called every frame at a time when no-one is operating on the collection.
        /// </summary>
        public void FinishAddsAndRemoves()
        {
            foreach (var layer in _arenaLayers) layer.Gobs.FinishAddsAndRemoves();
        }

        public IEnumerator<Gob> GetEnumerator()
        {
            foreach (var layer in _arenaLayers)
                foreach (var gob in layer.Gobs)
                    yield return gob;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
