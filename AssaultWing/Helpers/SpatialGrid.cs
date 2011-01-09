using System;
using System.Collections.Generic;
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
        private float _cellSize;

        /// <summary>
        /// Minimum coordinates of grid cell [0,0].
        /// </summary>
        private Vector2 _gridMin;

        /// <summary>
        /// Maximum coordinates of the last grid cell.
        /// </summary>
        private Vector2 _gridMax;

        /// <summary>
        /// An upper limit for dimensions of bounding boxes that are stored in the grid.
        /// </summary>
        private Vector2 _boundingBoxMax;

        /// <summary>
        /// The grid cells, indexed like [y,x]. A cell at [y,x] covers the 
        /// axis-aligned rectangular area from
        /// <b>gridMin + (cellSize * x, cellSize * y)</b> to
        /// <b>gridMin + (cellSize * (x+1), cellSize * (y+1))</b>,
        /// exclusive of the edges with maximum X and maximum Y coordinates.
        /// Objects are stored in cells based on their bounding box's minimum coordinates.
        /// </summary>
        private List<SpatialGridElement<T>>[,] _cells;

        /// <summary>
        /// Overflow cell containing objects that are outside all regular cells.
        /// </summary>
        private List<SpatialGridElement<T>> _outerCell;

        /// <summary>
        /// Creates a new spatial grid, spanning over a rectangular area.
        /// </summary>
        /// <param name="cellSize">Width and height of all grid cells.</param>
        /// <param name="gridMin">Minimum coordinates covered by the grid, inclusive.</param>
        /// <param name="gridMax">Maximum coordinates covered by the grid, exclusive.</param>
        public SpatialGrid(float cellSize, Vector2 gridMin, Vector2 gridMax)
        {
            _cellSize = cellSize;
            _gridMin = gridMin;
            _gridMax = gridMax;
            _boundingBoxMax = Vector2.Zero;
            int gridWidth = (int)Math.Ceiling((gridMax.X - gridMin.X) / cellSize);
            int gridHeight = (int)Math.Ceiling((gridMax.Y - gridMin.Y) / cellSize);
            _cells = new List<SpatialGridElement<T>>[gridHeight, gridWidth];
            for (int y = 0; y < _cells.GetLength(0); ++y)
                for (int x = 0; x < _cells.GetLength(1); ++x)
                    _cells[y, x] = new List<SpatialGridElement<T>>();
            _outerCell = new List<SpatialGridElement<T>>();
        }

        /// <summary>
        /// Adds an element to the spatial grid.
        /// </summary>
        /// <param name="obj">The actual object to add.</param>
        /// <param name="boundingBox">A bounding box for the object.</param>
        /// <returns>The stored element in the spatial grid.</returns>
        public SpatialGridElement<T> Add(T obj, Rectangle boundingBox)
        {
            int gridX = (int)((boundingBox.Min.X - _gridMin.X) / _cellSize);
            int gridY = (int)((boundingBox.Min.Y - _gridMin.Y) / _cellSize);
            bool outOfBounds = gridX < 0 || gridY < 0
                || gridX >= _cells.GetLength(1) || gridY >= _cells.GetLength(0);
            if (outOfBounds)
            {
                gridX = -1;
                gridY = -1;
            }
            var element = new SpatialGridElement<T>(obj, boundingBox, this, gridX, gridY);
            if (outOfBounds)
                _outerCell.Add(element);
            else
                _cells[gridY, gridX].Add(element);
            _boundingBoxMax = Vector2.Max(_boundingBoxMax, boundingBox.Dimensions);
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
                _outerCell.RemoveAll(element => element.Value.Equals(obj));
            for (int y = gridMinY; y < gridMaxY; ++y)
                for (int x = gridMinX; x < gridMaxX; ++x)
                    _cells[y, x].RemoveAll(element => element.Value.Equals(obj));

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
                if (!_outerCell.Remove(element))
                    throw new ArgumentException("Given element didn't exist");
            }
            else
                if (!_cells[element.GridY, element.GridX].Remove(element))
                    throw new ArgumentException("Given element didn't exist");

            // We can't update 'boundingBoxMax' effectively to be a tight bound,
            // but it still maintains its invariant.
        }

        /// <summary>
        /// Removes all elements from the spatial grid.
        /// </summary>
        public void Clear()
        {
            _outerCell.Clear();
            foreach (var elementList in _cells)
                elementList.Clear();
            _boundingBoxMax = Vector2.Zero;
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
            
            var matches = new List<T>();
            if (outOfBounds)
                foreach (var element in _outerCell)
                    matches.Add(element.Value);
            for (int y = gridMinY; y < gridMaxY; ++y)
                for (int x = gridMinX; x < gridMaxX; ++x)
                    foreach (var element in _cells[y, x])
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
                for (int i = 0; i < _outerCell.Count; ++i)
                {
                    SpatialGridElement<T> cellElement = _outerCell[i];
                    if (!Geometry.Intersect(cellElement.BoundingBox, area)) continue;
                    if (action(cellElement.Value)) return;
                }
            for (int y = gridMinY; y < gridMaxY; ++y)
                for (int x = gridMinX; x < gridMaxX; ++x)
                {
                    var cell = _cells[y, x];
                    // We come here very often -- avoid foreach created iterator overhead.
                    for (int i = 0; i < cell.Count; ++i)
                    {
                        var cellElement = cell[i];
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
            for (int i = 0; i < _outerCell.Count; ++i)
                if (action(_outerCell[i].Value)) return;
            foreach (List<SpatialGridElement<T>> cell in _cells)
                for (int i = 0; i < cell.Count; ++i)
                    if (action(cell[i].Value)) return;
        }

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
        public void ConvertArea(Rectangle area, out int gridMinX, out int gridMinY,
            out int gridMaxX, out int gridMaxY, out bool outOfBounds)
        {
            outOfBounds = false;

            // Minimum grid indices are inclusive, maximum grid indices are exclusive.
            // Lower limits must compensate for the reference point of an object
            // stored in the grid being at most 'boundingBoxMax' farther than the
            // object's edge. Extreme values need be checked separately due to the 
            // danger of integer overflow.
            int maxCellX = _cells.GetLength(1);
            int maxCellY = _cells.GetLength(0);
            gridMinX = area.Min.X - _boundingBoxMax.X < _gridMin.X ? -1
                : area.Min.X - _boundingBoxMax.X >= _gridMax.X ? maxCellX + 1
                : (int)((area.Min.X - _boundingBoxMax.X - _gridMin.X) / _cellSize);
            gridMinY = area.Min.Y - _boundingBoxMax.Y < _gridMin.Y ? -1
                : area.Min.Y - _boundingBoxMax.Y >= _gridMax.Y ? maxCellY + 1
                : (int)((area.Min.Y - _boundingBoxMax.Y - _gridMin.Y) / _cellSize);
            gridMaxX = area.Max.X < _gridMin.X ? -1
                : area.Max.X >= _gridMax.X ? maxCellX + 1
                : (int)((area.Max.X - _gridMin.X) / _cellSize) + 1;
            gridMaxY = area.Max.Y < _gridMin.Y ? -1
                : area.Max.Y >= _gridMax.Y ? maxCellY + 1
                : (int)((area.Max.Y - _gridMin.Y) / _cellSize) + 1;
            outOfBounds = gridMinX < 0 || gridMinY < 0
                || gridMaxX > _cells.GetLength(1) || gridMaxY > _cells.GetLength(0);
            gridMinX = Math.Min(Math.Max(gridMinX, 0), _cells.GetLength(1));
            gridMinY = Math.Min(Math.Max(gridMinY, 0), _cells.GetLength(0));
            gridMaxX = Math.Min(Math.Max(gridMaxX, 0), _cells.GetLength(1));
            gridMaxY = Math.Min(Math.Max(gridMaxY, 0), _cells.GetLength(0));
        }
    }

    /// <summary>
    /// A wrapper for a value stored in a spatial grid. 
    /// Consists of the actual value, its bounding box and 
    /// a reference to the spatial grid where it is being stored.
    /// </summary>
    public class SpatialGridElement<T>
    {
        /// <summary>
        /// The actual value.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// The value's bounding box.
        /// </summary>
        public Rectangle BoundingBox { get; private set; }

        /// <summary>
        /// The spatial grid where this element is stored.
        /// </summary>
        public SpatialGrid<T> Owner { get; private set; }

        /// <summary>
        /// The X coordinate where this element is stored in the spatial grid.
        /// </summary>
        public int GridX { get; private set; }

        /// <summary>
        /// The Y coordinate where this element is stored in the spatial grid.
        /// </summary>
        public int GridY { get; private set; }

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
            Value = value;
            BoundingBox = boundingBox;
            Owner = owner;
            GridX = gridX;
            GridY = gridY;
        }
    }
}
