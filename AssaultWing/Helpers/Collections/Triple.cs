using System;

namespace AW2.Helpers
{
    public class Triple<T1, T2, T3>
        where T1 : class
        where T2 : class
        where T3 : class
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }

        public Triple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        public static bool operator ==(Triple<T1, T2, T3> t1, Triple<T1, T2, T3> t2)
        {
            return t1.Item1 == t2.Item1
                && t1.Item2 == t2.Item2
                && t1.Item3 == t2.Item3;
        }

        public static bool operator !=(Triple<T1, T2, T3> t1, Triple<T1, T2, T3> t2)
        {
            return t1.Item1 != t2.Item1
                || t1.Item2 != t2.Item2
                || t1.Item3 != t2.Item3;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Triple<T1, T2, T3>)) return false;
            var t2 = obj as Triple<T1, T2, T3>;
            return Item1 == t2.Item1
                && Item2 == t2.Item2
                && Item3 == t2.Item3;
        }

        public override int GetHashCode()
        {
            int code1 = Item1 == null ? 1195858194 : Item1.GetHashCode();
            int code2 = Item2 == null ? -2144854550 : Item2.GetHashCode();
            int code3 = Item3 == null ? -2098562292 : Item3.GetHashCode();
            return code1 ^ code2 ^ code3;
        }
    }
}
