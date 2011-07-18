using System.Collections.Generic;

namespace AW2.Core
{
    public class AWGameComponentCollection : IEnumerable<AWGameComponentCollection.Item>
    {
        public class Item
        {
            public AWGameComponent Component;
            public bool LoadContentCalled;
        }

        private List<Item> _components = new List<Item>();

        public void Add(AWGameComponent component)
        {
            _components.Add(new Item { Component = component });
            _components.Sort((a, b) => a.Component.UpdateOrder.CompareTo(b.Component.UpdateOrder));
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return _components.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _components.GetEnumerator();
        }
    }
}
