using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

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
        /// <param name="min">The object's bounding box's minimum coordinates.</param>
        /// <param name="max">The object's bounding box's maximum coordinates.</param>
        /// <returns>The stored element in the spatial grid.</returns>
        public SpatialGridElement<T> Add(T obj, Vector2 min, Vector2 max)
        {
            int gridX = (int)((min.X - gridMin.X) / cellSize);
            int gridY = (int)((min.Y - gridMin.Y) / cellSize);
            bool outOfBounds = gridX < 0 || gridY < 0
                || gridX > cells.GetLength(1) || gridY > cells.GetLength(0);
            if (outOfBounds)
            {
                gridX = -1;
                gridY = -1;
            }
            SpatialGridElement<T> element = new SpatialGridElement<T>(obj, min, max, this, gridX, gridY);
            if (outOfBounds)
                outerCell.Add(element);
            else
                cells[gridY, gridX].Add(element);
            boundingBoxMax = Vector2.Max(boundingBoxMax, max - min);
            return element;
        }

        /// <summary>
        /// Removes from a rectangular area all elements equal to an element.
        /// </summary>
        /// <param name="obj">The actual object to remove.</param>
        /// <param name="min">Minimum coordinates of the rectangular area.</param>
        /// <param name="max">Maximum coordinates of the rectangular area.</param>
        public void Remove(T obj, Vector2 min, Vector2 max)
        {
            int gridMinX, gridMinY, gridMaxX, gridMaxY;
            bool outOfBounds;
            ConvertArea(min, max, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);

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
                throw new ArgumentException("Cannot remove element that is not stored in this container");
            if (element.GridX == -1)
                outerCell.Remove(element);
            else
                cells[element.GridY, element.GridX].Remove(element);

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
        /// Performs an action on each element that intersects a rectangular area.
        /// </summary>
        /// <param name="min">Minimum coordinates of the rectangular area.</param>
        /// <param name="max">Maximum coordinates of the rectangular area.</param>
        /// <param name="action">The action to perform.</param>
        public void ForEachElement(Vector2 min, Vector2 max, Action<T> action)
        {
            int gridMinX, gridMinY, gridMaxX, gridMaxY;
            bool outOfBounds;
            ConvertArea(min, max, out gridMinX, out gridMinY, out gridMaxX, out gridMaxY, out outOfBounds);

            if (outOfBounds)
                foreach (SpatialGridElement<T> element in outerCell)
                    action(element.Value);
            for (int y = gridMinY; y < gridMaxY; ++y)
                for (int x = gridMinX; x < gridMaxX; ++x)
                    foreach (SpatialGridElement<T> element in cells[y, x])
                        action(element.Value);
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
        /// <param name="min">Minimum coordinates of the rectangular area.</param>
        /// <param name="max">Maximum coordinates of the rectangular area.</param>
        /// <param name="gridMinX">Where to store the minimum grid cell X index, inclusive.</param>
        /// <param name="gridMinY">Where to store the minimum grid cell Y index, inclusive.</param>
        /// <param name="gridMaxX">Where to store the maximum grid cell X index, exclusive.</param>
        /// <param name="gridMaxY">Where to store the maximum grid cell Y index, exclusive.</param>
        /// <param name="outOfBounds">Where to store the fact that the outer cell is included in the range.</param>
        void ConvertArea(Vector2 min, Vector2 max, out int gridMinX, out int gridMinY,
            out int gridMaxX, out int gridMaxY, out bool outOfBounds)
        {
            // The empty area contains no elements.
            if (min.X > max.X || min.Y > max.Y)
            {
                gridMinX = gridMinY = gridMaxX = gridMaxY = 0;
                outOfBounds = false;
                return;
            }

            // Minimum grid indices are inclusive, maximum grid indices are exclusive.
            // Lower limits must compensate for the reference point of an object
            // stored in the grid being at most 'boundingBoxMax' farther than the
            // object's edge.
            gridMinX = (int)((min.X - boundingBoxMax.X - gridMin.X) / cellSize);
            gridMinY = (int)((min.Y - boundingBoxMax.Y - gridMin.Y) / cellSize);
            gridMaxX = (int)((max.X - gridMin.X) / cellSize) + 1;
            gridMaxY = (int)((max.Y - gridMin.Y) / cellSize) + 1;
            outOfBounds = gridMinX < 0 || gridMinY < 0
                || gridMaxX > cells.GetLength(1) || gridMaxY > cells.GetLength(0);
            gridMinX = Math.Min(Math.Max(gridMinX, 0), cells.GetLength(1));
            gridMinY = Math.Min(Math.Max(gridMinY, 0), cells.GetLength(0));
            gridMaxX = Math.Min(Math.Max(gridMaxX, 0), cells.GetLength(1));
            gridMaxY = Math.Min(Math.Max(gridMaxY, 0), cells.GetLength(0));
        }

        #endregion Private methods
    }

    /// <summary>
    /// A wrapper for a value stored in a spatial grid. 
    /// Consists of the actual value, its bounding box and 
    /// a reference to the spatial grid where it is being stored.
    /// </summary>
    public class SpatialGridElement<T>
    {
        T value;
        Vector2 min;
        Vector2 max;
        SpatialGrid<T> owner;
        int x, y;

        /// <summary>
        /// The actual value.
        /// </summary>
        public T Value { get { return value; } }

        /// <summary>
        /// The value's bounding box's minimum coordinates.
        /// </summary>
        public Vector2 Min { get { return min; } }

        /// <summary>
        /// The value's bounding box's maximum coordinates.
        /// </summary>
        public Vector2 Max { get { return max; } }

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
        /// <param name="min">The value's bounding box's minimum coordinates.</param>
        /// <param name="max">The value's bounding box's maximum coordinates.</param>
        /// <param name="owner">The spatial grid where this element will be stored.</param>
        /// <param name="gridX">The X coordinate where this element is stored in the spatial grid.</param>
        /// <param name="gridY">The Y coordinate where this element is stored in the spatial grid.</param>
        public SpatialGridElement(T value, Vector2 min, Vector2 max,
            SpatialGrid<T> owner, int gridX, int gridY)
        {
            this.value = value;
            this.min = min;
            this.max = max;
            this.owner = owner;
            this.x = gridX;
            this.y = gridY;
        }
    }
}
