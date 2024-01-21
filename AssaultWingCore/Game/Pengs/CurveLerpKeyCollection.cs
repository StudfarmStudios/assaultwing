using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Serialization;

namespace AW2.Game.Pengs
{
    /// <summary>
    /// A collection of <see cref="CurveLerpKey"/>s.
    /// </summary>
    [Obsolete]
    [LimitedSerialization]
    public class CurveLerpKeyCollection
    {
        /// <summary>
        /// The keys, sorted by <see cref="CurveLerpKey.Input"/>
        /// </summary>
        [TypeParameter]
        private List<CurveLerpKey> _keys;

        public int Count { get { return _keys.Count; } }
        public CurveLerpKey this[int index] { get { return _keys[index]; } }

        public CurveLerpKeyCollection()
        {
            _keys = new List<CurveLerpKey>();
        }

        public void Add(CurveLerpKey key)
        {
            // Linear search -- '_keys' are assumed to be a small collection.
            int i = 0;
            while (i < _keys.Count && _keys[i].Input < key.Input) ++i;
            _keys.Insert(i, key);
        }

        public void Clear()
        {
            _keys.Clear();
        }
    }
}
