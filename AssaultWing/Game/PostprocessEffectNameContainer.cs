using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;

namespace AW2.Game
{
    public class PostprocessEffectNameContainer : IEnumerable<CanonicalString>
    {
        Player _owner;
        List<CanonicalString> _items;

        public int Count { get { return _items.Count; } }

        public PostprocessEffectNameContainer(Player owner)
        {
            if (owner == null) throw new ArgumentNullException("Null owner given");
            _owner = owner;
            _items = new List<CanonicalString>();
        }

        public void Add(CanonicalString name)
        {
            _items.Add(name);
            _owner.MustUpdateToClients = true;
        }

        public void EnsureContains(CanonicalString name)
        {
            if (_items.Contains(name)) return;
            _items.Add(name);
            _owner.MustUpdateToClients = true;
        }

        public void Remove(CanonicalString name)
        {
            int index = _items.IndexOf(name);
            if (index < 0) return;
            _items.RemoveAt(index);
            _owner.MustUpdateToClients = true;
        }

        public void Clear()
        {
            _items.Clear();
            _owner.MustUpdateToClients = true;
        }

        public IEnumerator<CanonicalString> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
