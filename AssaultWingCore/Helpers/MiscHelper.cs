using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Helpers
{
    /// <summary>
    /// Contains miscellaneous extension methods.
    /// </summary>
    public static class MiscHelper
    {
        /// <summary>
        /// Returns the string starting with a capital letter.
        /// </summary>
        public static string Capitalize(this string value)
        {
            if (value == "") return "";
            return value.Substring(0, 1).ToUpper() + value.Substring(1);
        }

        /// <summary>
        /// Returns the string with each word starting with a capital letter.
        /// </summary>
        public static string CapitalizeWords(this string value)
        {
            if (value == "") return "";
            var result = new StringBuilder(value);
            result[0] = char.ToUpper(result[0]);
            for (int i = 0; i < result.Length - 1; ++i)
                if (result[i] == ' ') result[i + 1] = char.ToUpper(result[i + 1]);
            return result.ToString();
        }

        /// <summary>
        /// Returns a human-readable, nice string representation of the TimeSpan as a duration.
        /// </summary>
        public static string ToDurationString(this TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero) throw new ArgumentException("Only non-negative TimeSpans are supported", "timeSpan");
            if (timeSpan == TimeSpan.Zero) return "0 seconds";
            var str = new StringBuilder();
            int hours = (int)timeSpan.TotalHours;
            if (hours >= 1) str.Append(hours + (hours > 1 ? " hours " : " hour "));
            if (timeSpan.Minutes > 0) str.Append(timeSpan.Minutes + (timeSpan.Minutes > 1 ? " minutes " : " minute "));
            if (timeSpan.Seconds > 0) str.Append(timeSpan.Seconds + (timeSpan.Seconds > 1 ? " seconds " : " second "));
            str.Remove(str.Length - 1, 1); // remove trailing space
            return str.ToString();
        }

        /// <summary>
        /// Compares two sequences element by element for the first non-equal pair.
        /// </summary>
        /// <param name="a">First sequence</param>
        /// <param name="b">Second sequence</param>
        /// <param name="aDiff">First differing element in <paramref name="a"/>. Is null if either
        /// the sequences were otherwise equal but <paramref name="b"/> had more elements,
        /// or there were no differences in the sequences.</param>
        /// <param name="bDiff">First differing element in <paramref name="b"/>. Is null if either
        /// the sequences were otherwise equal but <paramref name="a"/> had more elements,
        /// or there were no differences in the sequences.</param>
        /// <param name="diffIndex">Index of the differing elements.</param>
        /// <returns>true if there was a difference; false if the sequences were equal</returns>
        public static bool FirstDifference<T>(IEnumerable<T> a, IEnumerable<T> b, out T aDiff, out T bDiff, out int diffIndex) where T : class
        {
            aDiff = null;
            bDiff = null;
            diffIndex = -1;
            using (IEnumerator<T> aEnum = a.GetEnumerator(), bEnum = b.GetEnumerator())
            {
                bool aHas, bHas;
                while (true)
                {
                    aHas = aEnum.MoveNext();
                    bHas = bEnum.MoveNext();
                    diffIndex++;
                    if (!aHas || !bHas) break;
                    var aItem = aEnum.Current;
                    var bItem = bEnum.Current;
                    if (aItem == null && bItem == null) continue;
                    if (aItem != null && bItem != null && aItem.Equals(bItem)) continue;
                    aDiff = aItem;
                    bDiff = bItem;
                    return true;
                }
                if (aHas) aDiff = aEnum.Current;
                if (bHas) bDiff = bEnum.Current;
                if (!aHas && !bHas) diffIndex = -1;
                return aHas || bHas;
            }
        }

        public static Vector2 Dimensions(this Texture2D texture)
        {
            return new Vector2(texture.Width, texture.Height);
        }

        /// <summary>
        /// Parses a string into an IP address and an optional port. The string may contain the IP address
        /// in ASCII or a hostname that will be resolved with DNS. If DNS is used, this method may
        /// take long to finish.
        /// </summary>
        public static IPEndPoint ParseIPEndPoint(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            try
            {
                var parts = text.Split(':');
                IPAddress ipAddress = null;
                if (char.IsDigit(parts[0].Last()))
                    ipAddress = IPAddress.Parse(parts[0]);
                else
                {
                    var hostEntry = Dns.GetHostEntry(parts[0]); // this may take some time
                    ipAddress = hostEntry.AddressList.First();
                }
                int port = parts.Length > 1 ? int.Parse(parts[1]) : 0;
                return new IPEndPoint(ipAddress, port);
            }
            catch (Exception e)
            {
                if (e is FormatException || e is OverflowException || e is ArgumentOutOfRangeException)
                    throw new ArgumentException("Invalid IP end point string: " + text, e);
                else
                    throw;
            }
        }

        public static int ParseIntWithGarbage(string str, int startIndex, out int endIndexExclusive)
        {
            endIndexExclusive = startIndex;
            while (endIndexExclusive < str.Length && char.IsDigit(str, endIndexExclusive)) endIndexExclusive++;
            return int.Parse(str.Substring(startIndex, endIndexExclusive - startIndex));
        }

        public static string BytesToString(ArraySegment<byte> buffer)
        {
            var bytes = buffer.Array
                .Skip(buffer.Offset)
                .Take(buffer.Count)
                .Select(a => a.ToString("X2"))
                .ToArray();
            return string.Join(",", bytes);
        }

        public static bool EqualsDeep(this PresentationParameters a, PresentationParameters b)
        {
            return b != null
                && a.BackBufferFormat == b.BackBufferFormat
                && a.BackBufferHeight == b.BackBufferHeight
                && a.BackBufferWidth == b.BackBufferWidth
                && a.Bounds == b.Bounds
                && a.DepthStencilFormat == b.DepthStencilFormat
                && a.DeviceWindowHandle == b.DeviceWindowHandle
                && a.DisplayOrientation == b.DisplayOrientation
                && a.IsFullScreen == b.IsFullScreen
                && a.MultiSampleCount == b.MultiSampleCount
                && a.PresentationInterval == b.PresentationInterval
                && a.RenderTargetUsage == b.RenderTargetUsage;
        }

        public static bool IsCompatibleWith(this Version v1, Version v2)
        {
            return v1.Equals(v2);
        }

        /// <summary>
        /// First tries to move the viewport to fit the bounds and then crops the viewport
        /// if it still doesn't fit in.
        /// </summary>
        public static Viewport LimitTo(this Viewport viewport, Rectangle clientBounds)
        {
            var x_width = LimitViewportAxis(viewport.X, viewport.Width, clientBounds.X, clientBounds.Right);
            viewport.X = x_width.Item1;
            viewport.Width = x_width.Item2;
            var y_height = LimitViewportAxis(viewport.Y, viewport.Height, clientBounds.Y, clientBounds.Bottom);
            viewport.Y = y_height.Item1;
            viewport.Height = y_height.Item2;
            return viewport;
        }

        private static Tuple<int, int> LimitViewportAxis(int start, int size, int min, int max)
        {
            start = start.Clamp(min, Math.Max(min, max - size));
            size = Math.Min(size, max - start + 1);
            return Tuple.Create(start, size);
        }
    }
}
