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
        private static readonly IEnumerable<Gob> g_emptyGobArray = new Gob[0];

        /// <summary>
        /// The arena layers that contain the gobs.
        /// </summary>
        [RuntimeState]
        private IList<ArenaLayer> _arenaLayers;

        private Dictionary<int, Gob> _gobsByID;
        private Dictionary<Type, HashSet<Gob>> _gobsByClass;

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

        public int Count { get { return _gobsByID.Count > 0 ? _gobsByID.Count : _arenaLayers.Sum(l => l.Gobs.Count); } }

        /// <summary>
        /// Returns the gob with <paramref name="gobID"/> or null if no such gob exists.
        /// </summary>
        public Gob this[int gobID]
        {
            get
            {
                Gob gob;
                _gobsByID.TryGetValue(gobID, out gob);
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
            _gobsByClass = new Dictionary<Type, HashSet<Gob>>();
            _gobsByID = new Dictionary<int, Gob>();
            // Note: There may be gobs in _arenaLayers but _gobsByID is uninitialized.
            // This happens only when an arena template is loaded from XML, in which case the gobs
            // don't even have proper IDs. When an arena is started for playing, all the gobs are added
            // by calling Add() whence _gobsByID is filled.
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

        public IEnumerable<T> All<T>() where T : Gob
        {
            return All(typeof(T)).Cast<T>();
        }

        public IEnumerable<Gob> All(Type gobClass)
        {
            HashSet<Gob> kindred;
            if (!_gobsByClass.TryGetValue(gobClass, out kindred)) return g_emptyGobArray;
            return kindred;
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
            if (_gobsByID.ContainsKey(gob.ID)) // This bug crashes game servers occasionally (2012-09-16).
                throw new ApplicationException(string.Format("Cannot add {0} (at {1}) because {2} (at {3}) exists",
                    gob, gob.BirthTime, _gobsByID[gob.ID], _gobsByID[gob.ID].BirthTime));
            _gobsByID.Add(gob.ID, gob);
            var gobClass = gob.GetType();
            HashSet<Gob> kindred;
            if (!_gobsByClass.TryGetValue(gobClass, out kindred))
            {
                kindred = new HashSet<Gob>();
                _gobsByClass.Add(gobClass, kindred);
            }
            kindred.Add(gob);
        }

        public void Clear()
        {
            _gobsByID.Clear();
            _gobsByClass.Clear();
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
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        /// <param name="force">Remove the element regardless of how <see cref="Removing"/>
        /// evaluates on the element.</param>
        public void Remove(Gob gob, bool force)
        {
            if (gob.Layer == null) return;
            if (Removing != null && !Removing(gob) && !force) return;
            _gobsByID.Remove(gob.ID);
            var gobClass = gob.GetType();
            HashSet<Gob> kindred;
            if (_gobsByClass.TryGetValue(gobClass, out kindred)) kindred.Remove(gob);
            gob.Layer.Gobs.Remove(gob);
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
