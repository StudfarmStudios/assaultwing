using System;
using System.Collections.Generic;

namespace AW2.Menu.Main
{
    /// <summary>
    /// A list of menu items, pluggable into <see cref="MainMenuComponent"/>.
    /// </summary>
    public class MainMenuItemCollection : IEnumerable<MainMenuItem>
    {
        private List<MainMenuItem> _menuItems;

        public string Name { get; private set; }
        public int Count { get { return _menuItems.Count; } }
        public MainMenuItem this[int i] { get { return _menuItems[i]; } }
        public Action Update { get; set; }

        public MainMenuItemCollection(string name)
        {
            if (name == null || name == "") throw new ArgumentNullException("Null or empty menu mode name");
            Name = name;
            Update = () => { };
            _menuItems = new List<MainMenuItem>();
        }

        public void Add(MainMenuItem item)
        {
            _menuItems.Add(item);
            UpdateItemIndices();
        }

        public void Insert(int index, MainMenuItem item)
        {
            _menuItems.Insert(index, item);
            UpdateItemIndices();
        }

        public void RemoveAt(int index)
        {
            _menuItems.RemoveAt(index);
            UpdateItemIndices();
        }

        public void Clear()
        {
            _menuItems.Clear();
        }

        public IEnumerator<MainMenuItem> GetEnumerator()
        {
            return _menuItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void UpdateItemIndices()
        {
            for (int i = 0; i < _menuItems.Count; i++)
                _menuItems[i].ItemIndex = i;
        }
    }
}
