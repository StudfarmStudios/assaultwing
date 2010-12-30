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

        public MainMenuItemCollection(string name)
        {
            if (name == null || name == "") throw new ArgumentNullException("Null or empty menu mode name");
            Name = name;
            _menuItems = new List<MainMenuItem>();
        }

        public void Add(MainMenuItem item)
        {
            item.ItemIndex = _menuItems.Count;
            _menuItems.Add(item);
        }

        public IEnumerator<MainMenuItem> GetEnumerator()
        {
            return _menuItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
