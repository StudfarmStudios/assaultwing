using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Size = System.Drawing.Size;

namespace AW2.Helpers
{
    /// <summary>
    /// Contains miscellaneous extension methods.
    /// </summary>
    public static class MiscHelper
    {

        public static NameValueCollection QueryParams
        {
            get
            {
                return new NameValueCollection();
            }
        }
        public static Version Version
        {
            get
            {
                return new Version();
            }
        }
        public static string DataDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }
        /// <summary>
        /// Returns the string starting with a capital letter.
        /// </summary>
        public static string Capitalize(this string value)
        {
            if (value == "") return "";
            return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1);
        }

        public static string Uncapitalize(this string value)
        {
            if (value == "") return "";
            return value.Substring(0, 1).ToLowerInvariant() + value.Substring(1);
        }

        /// <summary>
        /// Returns the string with each word starting with a capital letter.
        /// </summary>
        public static string CapitalizeWords(this string value)
        {
            if (value == "") return "";
            var result = new StringBuilder(value);
            result[0] = char.ToUpperInvariant(result[0]);
            for (int i = 0; i < result.Length - 1; ++i)
                if (result[i] == ' ') result[i + 1] = char.ToUpperInvariant(result[i + 1]);
            return result.ToString();
        }

        public static string ToOrdinalString(this int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException();
            var unitDigit = value % 10;
            var tensDigit = (value % 100) / 10;
            var suffix =
                tensDigit == 1 ? "th" :
                unitDigit == 1 ? "st" :
                unitDigit == 2 ? "nd" :
                unitDigit == 3 ? "rd" :
                "th";
            return value + suffix;
        }

        /// <summary>
        /// Returns a human-readable, nice string representation of the TimeSpan as a duration.
        /// String parameters specify the names of the corresponding units. A null string means
        /// to completely omit that element from the string. All units with zero values are
        /// omitted from the string.
        /// </summary>
        /// <param name="usePlurals">If true, append 's' to unit names if the correponding value is not 1.</param>
        public static string ToDurationString(this TimeSpan timeSpan, string day, string hour, string minute, string second, bool usePlurals)
        {
            if (timeSpan < TimeSpan.Zero) throw new ArgumentException("Only non-negative TimeSpans are supported", "timeSpan");
            var str = new StringBuilder();
            Func<double, string, int> append = (value, unit) =>
            {
                var intValue = unit == null ? 0 : (int)value;
                if (intValue > 0) str.Append(intValue).Append(' ').Append(unit).Append(usePlurals && intValue > 1 ? "s " : " ");
                return intValue;
            };
            var remainingTimeSpan = timeSpan;
            remainingTimeSpan -= TimeSpan.FromDays(append(remainingTimeSpan.TotalDays, day));
            remainingTimeSpan -= TimeSpan.FromHours(append(remainingTimeSpan.TotalHours, hour));
            remainingTimeSpan -= TimeSpan.FromMinutes(append(remainingTimeSpan.TotalMinutes, minute));
            remainingTimeSpan -= TimeSpan.FromSeconds(append(remainingTimeSpan.TotalSeconds, second));
            if (str.Length > 0) str.Remove(str.Length - 1, 1); // remove trailing space
            return str.ToString();
        }

        /// <summary>
        /// Returns the time span in the form "X ms", where X is a decimal representation of the time span
        /// in milliseconds.
        /// </summary>
        public static string Ms(this TimeSpan timeSpan)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} ms", timeSpan.TotalMilliseconds);
        }

        /// <summary>
        /// Like <see cref="System.Web.HttpUtility.ParseQueryString"/> but works with .NET 4.0 Client Profile
        /// instead of the full .NET 4.0 Framework.
        /// </summary>
        public static NameValueCollection ParseQueryString(string queryString)
        {
            // Adapted on 2012-02-17 from Scott Dorman's code at http://stackoverflow.com/a/68803.
            var queryParameters = new NameValueCollection();
            var querySegments = UrlDecode(queryString).TrimStart('?').Split('&');
            foreach (var segment in querySegments)
            {
                var parts = segment.Split('=');
                if (parts.Length != 2) continue;
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                queryParameters.Add(key, value);
            }
            return queryParameters;
        }

        /// <summary>
        /// Like <see cref="System.Web.HttpUtility.UrlDecode"/> but works with .NET 4.0 Client Profile
        /// instead of the full .NET 4.0 Framework.
        /// </summary>
        public static string UrlDecode(string str)
        {
            return Regex.Replace(str, "(%..)+", match =>
            {
                var matchStr = match.ToString();
                try
                {
                    var bytes = new byte[matchStr.Length / 3];
                    for (int i = 0; i < matchStr.Length; i += 3)
                        bytes[i / 3] = byte.Parse(matchStr.Substring(i + 1, 2), NumberStyles.AllowHexSpecifier);
                    return Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    return matchStr;
                }
            });
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

        public static IEnumerable<T> GetRange<T>(this ReadOnlyCollection<T> items, int start, int count)
        {
            if (items.Count == 0) yield break;
            var safeStart = start.Clamp(0, items.Count - 1);
            var safeEnd = safeStart + count.Clamp(0, items.Count - safeStart);
            for (int i = safeStart; i < safeEnd; i++) yield return items[i];
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
                    throw new ArgumentException("Invalid IP end point '" + text + "'", e);
                else if (e is System.Net.Sockets.SocketException)
                    throw new ArgumentException("Couldn't resolve IP end point '" + text + "'", e);
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

        /// <summary>
        /// Returns the item at the greatest index that is accepted. Assumes that acceptable items
        /// come before rejectable items. If no item is acceptable, returns the first item.
        /// </summary>
        public static T FindMaxAcceptedOrMin<T>(int min, int max, Func<int, T> get, Func<T, bool> accept)
        {
            if (min > max) new ArgumentException("Min cannot be greater than max");
            if (accept(get(max))) return get(max);
            if (!accept(get(min))) return get(min);
            var maxAccepted = min;
            var minRejected = max;
            while (true)
            {
                if (maxAccepted + 1 == minRejected) return get(maxAccepted);
                var test = (maxAccepted + minRejected) / 2;
                if (accept(get(test))) maxAccepted = test;
                else minRejected = test;
            }
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
            if (!v1.IsPublished() || !v2.IsPublished()) return true;
            return v1.Major == v2.Major && v1.Minor == v2.Minor;
        }

        public static bool IsPublished(this Version v)
        {
            return v.Major != 0;
        }

        public static Size GetSize(this Rectangle rectangle)
        {
            return new Size(rectangle.Width, rectangle.Height);
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
