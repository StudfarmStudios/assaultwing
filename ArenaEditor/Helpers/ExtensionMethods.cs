using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Graphics;
using Point = System.Drawing.Point;

namespace AW2.Helpers
{
    public static class ExtensionMethods
    {
        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static Vector2 MouseMoveToWorldCoordinates(this AWViewport viewport, Point oldMouseLocation, Point newMouseLocation, float z)
        {
            var oldPos = viewport.ToPos(oldMouseLocation.ToVector2(), z);
            var nowPos = viewport.ToPos(newMouseLocation.ToVector2(), z);
            return nowPos - oldPos;
        }
    }
}
