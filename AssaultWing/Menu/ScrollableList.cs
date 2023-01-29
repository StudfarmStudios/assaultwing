using System;
using AW2.Helpers;

namespace AW2.Menu
{
    public class ScrollableList
    {
        public delegate void ListItemAction(int realIndex, int visibleIndex, bool isSelected);

        private int _index;
        private int _topmostIndex;
        private int _visibleCount;
        private Func<int> _getTotalCount;

        public int CurrentIndex { get { Update(); return _index; } set { _index = value; } }
        public int TopmostIndex { get { Update(); return _topmostIndex; } set { _topmostIndex = value; } }
        public bool IsScrollableUp { get { Update(); return _topmostIndex > 0; } }
        public bool IsScrollableDown { get { Update(); return _topmostIndex + _visibleCount < _getTotalCount(); } }
        public bool IsCurrentValidIndex { get { Update(); return _index >= 0 && _index < _getTotalCount(); } }

        public ScrollableList(int visibleCount, Func<int> getTotalCount)
        {
            if (visibleCount <= 0) throw new ArgumentOutOfRangeException();
            _visibleCount = visibleCount;
            _getTotalCount = getTotalCount;
        }

        public void ForEachVisible(ListItemAction action)
        {
            Update();
            for (int visibleIndex = 0; visibleIndex < _visibleCount && _topmostIndex + visibleIndex < _getTotalCount(); visibleIndex++)
            {
                int realIndex = _topmostIndex + visibleIndex;
                action(realIndex, visibleIndex, realIndex == _index);
            }
        }

        private void Update()
        {
            _index = _getTotalCount() == 0 ? 0 : _index.Clamp(0, _getTotalCount() - 1);
            _topmostIndex = _topmostIndex.Clamp(_index - _visibleCount + 1, _index);
            _topmostIndex = Math.Min(_topmostIndex, Math.Max(0, _getTotalCount() - _visibleCount));
        }
    }
}
