using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace AW2.Menu
{
    [TestFixture]
    public class ScrollableListTest
    {
        [Test]
        public void TestCurrentIndex()
        {
            Action<int, int, bool, bool, ScrollableList> assertList = (expCurr, expTop, expScrlDown, expScrlUp, selList) =>
            {
                Assert.AreEqual(expCurr, selList.CurrentIndex);
                Assert.AreEqual(expTop, selList.TopmostIndex);
                Assert.AreEqual(expScrlDown, selList.IsScrollableDown);
                Assert.AreEqual(expScrlUp, selList.IsScrollableUp);
            };
            var count = 8;
            var list = new ScrollableList(3, () => count);
            assertList(0, 0, true, false, list);
            list.CurrentIndex++;
            assertList(1, 0, true, false, list);
            list.CurrentIndex += 2;
            assertList(3, 1, true, true, list);
            list.CurrentIndex = 99;
            assertList(7, 5, false, true, list);
            count--;
            assertList(6, 4, false, true, list);
            list.CurrentIndex -= 2;
            assertList(4, 4, false, true, list);
            list.CurrentIndex--;
            assertList(3, 3, true, true, list);
            list.CurrentIndex = -99;
            assertList(0, 0, true, false, list);
        }

        [Test]
        public void TestForEachVisible()
        {
            Action<int[], int[], bool[], ScrollableList> assertForEachVisible = (expReals, expVisibles, expSelects, selList) =>
            {
                Assert.IsTrue(expReals.Length == expVisibles.Length && expReals.Length == expSelects.Length);
                List<Tuple<int, int, bool>> results = new List<Tuple<int, int, bool>>();
                selList.ForEachVisible((realIndex, visibleIndex, isSelected) => results.Add(Tuple.Create(realIndex, visibleIndex, isSelected)));
                for (int i = 0; i < expReals.Length; i++)
                {
                    Assert.AreEqual(expReals[i], results[i].Item1);
                    Assert.AreEqual(expVisibles[i], results[i].Item2);
                    Assert.AreEqual(expSelects[i], results[i].Item3);
                }
            };
            var count = 8;
            var list = new ScrollableList(3, () => count);
            assertForEachVisible(new[] { 0, 1, 2 }, new[] { 0, 1, 2 }, new[] { true, false, false }, list);
            list.CurrentIndex++;
            assertForEachVisible(new[] { 0, 1, 2 }, new[] { 0, 1, 2 }, new[] { false, true, false }, list);
            list.CurrentIndex += 2;
            assertForEachVisible(new[] { 1, 2, 3 }, new[] { 0, 1, 2 }, new[] { false, false, true }, list);
            list.CurrentIndex = 99;
            assertForEachVisible(new[] { 5, 6, 7 }, new[] { 0, 1, 2 }, new[] { false, false, true }, list);
            count--;
            assertForEachVisible(new[] { 4, 5, 6 }, new[] { 0, 1, 2 }, new[] { false, false, true }, list);
            list.CurrentIndex -= 2;
            assertForEachVisible(new[] { 4, 5, 6 }, new[] { 0, 1, 2 }, new[] { true, false, false }, list);
            list.CurrentIndex--;
            assertForEachVisible(new[] { 3, 4, 5 }, new[] { 0, 1, 2 }, new[] { true, false, false }, list);
            list.CurrentIndex = -99;
            assertForEachVisible(new[] { 0, 1, 2 }, new[] { 0, 1, 2 }, new[] { true, false, false }, list);
        }
    }
}
