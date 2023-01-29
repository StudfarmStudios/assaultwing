using System;
using System.Collections.Generic;

namespace AW2.Core
{
    public class AWGameComponentCollection : IEnumerable<AWGameComponent>
    {
        private List<AWGameComponent> _components = new List<AWGameComponent>();

        public void Add(AWGameComponent component)
        {
            _components.Add(component);
            _components.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
        }

        public void Remove(Predicate<AWGameComponent> match)
        {
            _components.RemoveAll(match);
        }

        public IEnumerator<AWGameComponent> GetEnumerator()
        {
            return _components.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _components.GetEnumerator();
        }
    }
}
