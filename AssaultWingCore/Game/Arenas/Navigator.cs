using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;

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
            var pathFinderGrid = new byte[(int)(dimensions.X / granularity), (int)(dimensions.Y / granularity)];
            for (var gridY = 0; gridY < pathFinderGrid.GetLength(1); gridY++)
                for (var gridX = 0; gridX < pathFinderGrid.GetLength(0); gridX++)
                    pathFinderGrid[gridX, gridY] = isAccessible(new Vector2(gridX, gridY) * granularity) ? (byte)1 : (byte)0;
            _pathFinder = new PathFinderAStar(pathFinderGrid)
            {
                Diagonals = true,
                HeuristicEstimate = 2,
                HeavyDiagonals = false,
                PunishChangeDirection = false,
                TieBreaker = false,
                Formula = PathFinderAStar.HeuristicFormula.Manhattan,
                SearchLimit = 10000,
            };
        }

        public IEnumerable<Vector2> GetPath(Vector2 from, Vector2 to)
        {
            Func<Vector2, System.Drawing.Point> toPoint = v => new System.Drawing.Point((int)Math.Round(v.X), (int)Math.Round(v.Y));
            var reversePath = _pathFinder.FindPath(toPoint(from), toPoint(to));
            if (reversePath == null) yield break;
            for (int i = reversePath.Count - 1; i >= 0; i--) yield return new Vector2(reversePath[i].X, reversePath[i].Y);
        }
    }
}
