using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;

namespace AW2.Game.Arenas
{
    /// <summary>
    /// Provides route planning for an arena.
    /// </summary>
    public class Navigator
    {
        private float _granularity;
        private PathFinderAStar _pathFinder;

        public Navigator(Vector2 dimensions, float granularity, Func<Vector2, bool> isAccessible)
        {
            Debug.Assert(dimensions.X > 0 && dimensions.Y > 0);
            Debug.Assert(granularity > 0);
            _granularity = granularity;
            var pathFinderGrid = new byte[AWMathHelper.CeilingPowerTwo(CoordinateToIndex(dimensions.X)), CoordinateToIndex(dimensions.Y)];
            for (var gridY = 0; gridY < pathFinderGrid.GetLength(1); gridY++)
                for (var gridX = 0; gridX < pathFinderGrid.GetLength(0); gridX++)
                    pathFinderGrid[gridX, gridY] = isAccessible(IndicesToVector2(gridX, gridY)) ? (byte)1 : (byte)0;
            _pathFinder = new PathFinderAStar(pathFinderGrid)
            {
                HeuristicEstimate = 1,
                SearchLimit = 20000,
            };
        }

        public IEnumerable<Vector2> GetPath(Vector2 from, Vector2 to)
        {
            var startX = CoordinateToIndex(from.X);
            var startY = CoordinateToIndex(from.Y);
            var endX = CoordinateToIndex(to.X);
            var endY = CoordinateToIndex(to.Y);
            var reversePath = _pathFinder.FindPath(startX, startY, endX, endY);
            if (reversePath == null) yield break;
            for (int i = reversePath.Count - 1; i >= 0; i--) yield return IndicesToVector2(reversePath[i].X, reversePath[i].Y);
        }

        private int CoordinateToIndex(float coord)
        {
            return (int)(coord / _granularity);
        }

        private Vector2 IndicesToVector2(int x, int y)
        {
            return new Vector2((x + 0.5f) * _granularity, (y + 0.5f) * _granularity);
        }
    }
}
