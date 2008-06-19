#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers.Geometric;
using Rectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Helpers
{
    /// <summary>
    /// A grid in 2D space, storing in its cells objects with bounding boxes.
    /// </summary>
    /// <typeparam name="T">The type of stored objects.</typeparam>
    public class SpatialGrid<T>
    {
        /// <summary>
        /// Width and height of all grid cells.
        /// </summary>
        float cellSize;

        /// <summary>
        /// Minimum coordinates of grid cell [0,0].
        /// </summary>
        Vector2 gridMin;

        /// <summary>
        /// Maximum coordinates of the last grid cell.
        /// </summary>
        Vector2 gridMax;

        /// <summary>
        /// An upper limit for dimensions of bounding boxes that are stored in the grid.
        /// </summary>
        Vector2 boundingBoxMax;

        /// <summary>
        /// The grid cells, indexed like [y,x]. A cell at [y,x] covers the 
        /// axis-aligned rectangular area from
        /// <b>gridMin + (cellSize * x, cellSize * y)</b> to
        /// <b>gridMin + (cellSize * (x+1), cellSize * (y+1))</b>,
        /// exclusive of the edges with maximum X and maximum Y coordinates.
        /// Objects are stored in cells based on their bounding box's minimum coordinates.
        /// </summary>
        List<SpatialGridElement<T>>[,] cells;

        /// <summary>
        /// Overflow cell containing objects that are outside all regular cells.
        /// </summary>
        List<SpatialGridElement<T>> outerCell;

        /// <summary>
        /// Creates a new spatial grid, spanning over a rectangular area.
        /// </summary>
        /// <param name="cellSize">Width and height of all grid cells.</param>
        /// <param name="gridMin">Minimum coordinates covered by the grid.</param>
        /// <param name="gridMax">Maximum coordinates covered by the grid.</param>
        public SpatialGrid(float cellSize, Vector2 gridMin, Vector2 gridMax)
        {
            this.cellSize = cellSize;
            this.gridMin = gridMin;
            this.gridMax = gridMax;
            boundingBoxMax = Vector2.Zero;
            int gridWidth = (int)Math.Ceiling((gridMax.X - gridMin.X) / cellSize);
            int gridHeight = (int)Math.Ceiling((gridMax.Y - gridMin.Y) / cellSize);
            cells = new List<SpatialGridElement<T>>[gridHeight, gridWidth];
            for (int y = 0; y < cells.GetLength(0); ++y)
                for (int x = 0; x < cells.GetLength(1); ++x)
                    cells[y, x] = new List<SpatialGridElement<T>>();
            outerCell = new List<SpatialGridElement<T>>();
        }

        #region Public methods

        /// <summary>
        /// Adds an element to the spatial grid.
        /// </summary>
        /// <param name="obj">The actual object to add.</param>
        /// <param name="boundingBox">A bounding box for the object.</param>
        /// <returns>The stored element in the spatial grid.</returns>
        public SpatialGridElement<T> Add(T obj, Rectangle boundingBox)
        {
            int gridX = (int)((boundingBox.Min.X - gridMin.X) / cellSize);
            int gridY = (int)((boundingBox.Min.Y - gridMin.Y) / cellSize);
            bool outOfBounds = gridX < 0 || gridY < 0
                || gridX >= cells.GetLength(1) || gridY >= cells.GetLength(0);
            if (outOfBounds)
            {
                gridX = -1;
                gridY = -1;
            }
            SpatialGridElement<T> element = new SpatialGridElement<T>(obj, boundingBox, this, gridX, gridY);
            if (outOfBounds)
                outerCell.Add(element);
            else
                cells[gridY, gridX].Add(element);
            boundingBoxMax = Vector2.Max(boundingBoxMax, boundingBox.Dimensions);
            return element;
        }

        /// <summary>
        /// Removes elements from the spatial grid.
        /// </summary>
        /// The all found instances of the given value are removed.
        /// The search will cover the given area and perhaps some more.
        /// <param name="obj">The actual object to remove.</param>
        /// <param name="area">The minimal area to search for the instances.</param>
        public void Remove(T obj, Rectangle area)
        {
            int gridMinX, gridMinY, gridMaxX, gridMaxY;
            bool outOfBounds;
            ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);

            if (outOfBounds)
                outerCell.RemoveAll(delegate(SpatialGridElement<T> element) { return element.Value.Equals(obj); });
            for (int y = gridMinY; y < gridMaxY; ++y)
                for (int x = gridMinX; x < gridMaxX; ++x)
                    cells[y, x].RemoveAll(delegate(SpatialGridElement<T> element) { return element.Value.Equals(obj); });

            // We can't update 'boundingBoxMax' effectively to be a tight bound,
            // but it still maintains its invariant.
        }

        /// <summary>
        /// Removes an element from the spatial grid.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        public void Remove(SpatialGridElement<T> element)
        {
            if (element.Owner != this)
                throw new ArgumentException("Cannot remove an element that is not stored in this container");
            if (element.GridX == -1)
            {
                if (!outerCell.Remove(element))
                    throw new ArgumentException("Given element didn't exist");
            }
            else
                if (!cells[element.GridY, element.GridX].Remove(element))
                    throw new ArgumentException("Given element didn't exist");

            // We can't update 'boundingBoxMax' effectively to be a tight bound,
            // but it still maintains its invariant.
        }

        /// <summary>
        /// Removes all elements from the spatial grid.
        /// </summary>
        public void Clear()
        {
            outerCell.Clear();
            foreach (List<SpatialGridElement<T>> elementList in cells)
                elementList.Clear();
            boundingBoxMax = Vector2.Zero;
        }

        /// <summary>
        /// Returns all elements that intersect a rectangular area.
        /// </summary>
        /// <param name="area">The rectangular area.</param>
        public IEnumerable<T> GetElements(Rectangle area)
        {
            int gridMinX, gridMinY, gridMaxX, gridMaxY;
            bool outOfBounds;
            ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
            
            List<T> matches = new List<T>();
            if (outOfBounds)
                foreach (SpatialGridElement<T> element in outerCell)
                    matches.Add(element.Value);
            for (int y = gridMinY; y < gridMaxY; ++y)
                for (int x = gridMinX; x < gridMaxX; ++x)
                    foreach (SpatialGridElement<T> element in cells[y, x])
                        if (Geometry.Intersect(element.BoundingBox, area))
                            matches.Add(element.Value);
            return matches;
        }

        /// <summary>
        /// Performs an action on each element that intersects a rectangular area.
        /// If the action returns <c>true</c> then the iteration will break.
        /// </summary>
        /// <param name="area">The rectangular area.</param>
        /// <param name="action">The action to perform. If it returns <c>true</c>,
        /// the iteration will break.</param>
        public void ForEachElement(Rectangle area, Predicate<T> action)
        {
            int gridMinX, gridMinY, gridMaxX, gridMaxY;
            bool outOfBounds;
            ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);

            if (outOfBounds)
                for (int i = 0; i < outerCell.Count; ++i)
                {
                    SpatialGridElement<T> cellElement = outerCell[i];
                    if (!Geometry.Intersect(cellElement.BoundingBox, area)) continue;
                    if (action(cellElement.Value)) return;
                }
            for (int y = gridMinY; y < gridMaxY; ++y)
                for (int x = gridMinX; x < gridMaxX; ++x)
                {
                    List<SpatialGridElement<T>> cell = cells[y, x];
                    // We come here very often -- avoid foreach created iterator overhead.
                    for (int i = 0; i < cell.Count; ++i)
                    {
                        SpatialGridElement<T> cellElement = cell[i];
                        if (!Geometry.Intersect(cellElement.BoundingBox, area)) continue;
                        if (action(cellElement.Value)) return;
                    }
                }
        }

        /// <summary>
        /// Performs an action on each element in the grid. If the action returns
        /// <c>true</c> then the iteration will break.
        /// </summary>
        /// <param name="action">The action to perform. If it returns <c>true</c>,
        /// the iteration will break.</param>
        public void ForEachElement(Predicate<T> action)
        {
            for (int i = 0; i < outerCell.Count; ++i)
                if (action(outerCell[i].Value)) return;
            foreach (List<SpatialGridElement<T>> cell in cells)
                for (int i = 0; i < cell.Count; ++i)
                    if (action(cell[i].Value)) return;
        }

        #endregion Public methods

        #region Private methods

        /// <summary>
        /// Converts a rectangular area given in Cartesian coordinates into
        /// a rectangular range of grid cells that will contain all the elements
        /// whose bounding boxes the area intersects, possibly including the outer cell.
        /// The resulting range is given in pairs of minimum and maximum indices 
        /// of which minimum indices are inclusive and maximum indices are exclusive.
        /// </summary>
        /// <param name="area">The rectangular area.</param>
        /// <param name="gridMinX">Where to store the minimum grid cell X index, inclusive.</param>
        /// <param name="gridMinY">Where to store the minimum grid cell Y index, inclusive.</param>
        /// <param name="gridMaxX">Where to store the maximum grid cell X index, exclusive.</param>
        /// <param name="gridMaxY">Where to store the maximum grid cell Y index, exclusive.</param>
        /// <param name="outOfBounds">Where to store the fact that the outer cell is included in the range.</param>
        void ConvertArea(Rectangle area, out int gridMinX, out int gridMinY,
            out int gridMaxX, out int gridMaxY, out bool outOfBounds)
        {
            outOfBounds = false;

            // Minimum grid indices are inclusive, maximum grid indices are exclusive.
            // Lower limits must compensate for the reference point of an object
            // stored in the grid being at most 'boundingBoxMax' farther than the
            // object's edge. Extreme values need be checked separately due to the 
            // danger of integer overflow.
            int maxCellX = cells.GetLength(1);
            int maxCellY = cells.GetLength(0);
            gridMinX = area.Min.X - boundingBoxMax.X < gridMin.X ? -1
                : area.Min.X - boundingBoxMax.X >= gridMax.X ? maxCellX + 1
                : (int)((area.Min.X - boundingBoxMax.X - gridMin.X) / cellSize);
            gridMinY = area.Min.Y - boundingBoxMax.Y < gridMin.Y ? -1
                : area.Min.Y - boundingBoxMax.Y >= gridMax.Y ? maxCellY + 1
                : (int)((area.Min.Y - boundingBoxMax.Y - gridMin.Y) / cellSize);
            gridMaxX = area.Max.X < gridMin.X ? -1
                : area.Max.X >= gridMax.X ? maxCellX + 1
                : (int)((area.Max.X - gridMin.X) / cellSize) + 1;
            gridMaxY = area.Max.Y < gridMin.Y ? -1
                : area.Max.Y >= gridMax.Y ? maxCellY + 1
                : (int)((area.Max.Y - gridMin.Y) / cellSize) + 1;
            outOfBounds = gridMinX < 0 || gridMinY < 0
                || gridMaxX > cells.GetLength(1) || gridMaxY > cells.GetLength(0);
            gridMinX = Math.Min(Math.Max(gridMinX, 0), cells.GetLength(1));
            gridMinY = Math.Min(Math.Max(gridMinY, 0), cells.GetLength(0));
            gridMaxX = Math.Min(Math.Max(gridMaxX, 0), cells.GetLength(1));
            gridMaxY = Math.Min(Math.Max(gridMaxY, 0), cells.GetLength(0));
        }

        #endregion Private methods

        #region Unit tests
#if DEBUG
        /// <summary>
        /// Testing SpatialGrid.
        /// </summary>
        [TestFixture]
        public class SpatialGridTest
        {
            /// <summary>
            /// Sets up the test.
            /// </summary>
            [SetUp]
            public void SetUp()
            {
            }

            /// <summary>
            /// Tests coordinate translation.
            /// </summary>
            [Test]
            public void TestConvertArea()
            {
                int gridMinX, gridMinY, gridMaxX, gridMaxY;
                bool outOfBounds;
                SpatialGrid<int> grid1 = new SpatialGrid<int>(10, new Vector2(-50), new Vector2(100));
                Rectangle area;

                area = new Rectangle(1, 1, 9, 9);
                grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
                Assert.AreEqual(5, gridMinX);
                Assert.AreEqual(5, gridMinY);
                Assert.AreEqual(6, gridMaxX);
                Assert.AreEqual(6, gridMaxY);
                Assert.AreEqual(false, outOfBounds);

                area = new Rectangle(0, 0, 10, 10);
                grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
                Assert.AreEqual(5, gridMinX);
                Assert.AreEqual(5, gridMinY);
                Assert.AreEqual(7, gridMaxX);
                Assert.AreEqual(7, gridMaxY);
                Assert.AreEqual(false, outOfBounds);

                area = new Rectangle(-50, -50, -50, -50);
                grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
                Assert.AreEqual(0, gridMinX);
                Assert.AreEqual(0, gridMinY);
                Assert.AreEqual(1, gridMaxX);
                Assert.AreEqual(1, gridMaxY);
                Assert.AreEqual(false, outOfBounds);

                area = new Rectangle(99.9999f, 99.9999f, 99.9999f, 99.9999f);
                grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
                Assert.AreEqual(15, gridMinX);
                Assert.AreEqual(15, gridMinY);
                Assert.AreEqual(16, gridMaxX);
                Assert.AreEqual(16, gridMaxY);
                Assert.AreEqual(false, outOfBounds);

                area = new Rectangle(15, -15, float.MaxValue, float.MaxValue);
                grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
                Assert.AreEqual(6, gridMinX);
                Assert.AreEqual(3, gridMinY);
                Assert.AreEqual(16, gridMaxX);
                Assert.AreEqual(16, gridMaxY);
                Assert.AreEqual(true, outOfBounds);

                area = new Rectangle(-float.MaxValue, -float.MaxValue, float.MaxValue, 100.1f);
                grid1.ConvertArea(area, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);
                Assert.AreEqual(0, gridMinX);
                Assert.AreEqual(0, gridMinY);
                Assert.AreEqual(16, gridMaxX);
                Assert.AreEqual(16, gridMaxY);
                Assert.AreEqual(true, outOfBounds);
            }
        }
#endif
            #endregion Unit tests
    }

    /// <summary>
    /// A wrapper for a value stored in a spatial grid. 
    /// Consists of the actual value, its bounding box and 
    /// a reference to the spatial grid where it is being stored.
    /// </summary>
    public class SpatialGridElement<T>
    {
        T value;
        Rectangle boundingBox;
        SpatialGrid<T> owner;
        int x, y;

        /// <summary>
        /// The actual value.
        /// </summary>
        public T Value { get { return value; } }

        /// <summary>
        /// The value's bounding box.
        /// </summary>
        public Rectangle BoundingBox { get { return boundingBox; } }

        /// <summary>
        /// The spatial grid where this element is stored.
        /// </summary>
        public SpatialGrid<T> Owner { get { return owner; } }

        /// <summary>
        /// The X coordinate where this element is stored in the spatial grid.
        /// </summary>
        public int GridX { get { return x; } }

        /// <summary>
        /// The Y coordinate where this element is stored in the spatial grid.
        /// </summary>
        public int GridY { get { return y; } }

        /// <summary>
        /// Creates a wrapper for a value to be stored to a spatial grid.
        /// </summary>
        /// <param name="value">The actual value.</param>
        /// <param name="boundingBox">The value's bounding box.</param>
        /// <param name="owner">The spatial grid where this element will be stored.</param>
        /// <param name="gridX">The X coordinate where this element is stored in the spatial grid.</param>
        /// <param name="gridY">The Y coordinate where this element is stored in the spatial grid.</param>
        public SpatialGridElement(T value, Rectangle boundingBox,
            SpatialGrid<T> owner, int gridX, int gridY)
        {
            this.value = value;
            this.boundingBox = boundingBox;
            this.owner = owner;
            this.x = gridX;
            this.y = gridY;
        }
    }
}
