using System;
using System.Collections.Generic;
using System.Linq;
using AW2.Helpers.Serialization;

namespace AW2.Helpers
{
    /// <summary>
    /// A string that has a canonical form as a positive integer.
    /// Zero is the canonical value of an uninitialised string.
    /// </summary>
    /// The canonical form of a string is fixed the first time a
    /// <see cref="CanonicalString"/> instance is created of the string.
    /// From then on, all <see cref="CanonicalString"/> instances with
    /// the same string content will have the same canonical form.
    /// This enables one to use <c>IList&lt;T&gt;</c> in place of
    /// <c>IDictionary&lt;string, T&gt;</c> and to send only the canonical
    /// integer value over a bandwidth-limited stream instead of an 
    /// arbitrarily long string.
    [SerializedType(typeof(string))]
    public struct CanonicalString
    {
        private static List<string> g_canonicalForms;
        private static Dictionary<string, int> g_canonicalFormsIndices;
        private static bool g_canRegister;

        /// <summary>
        /// The <see cref="CanonicalString"/> instance corresponding to the null <see cref="string"/>.
        /// </summary>
        public static readonly CanonicalString Null;

        /// <summary>
        /// The index of a string in this list is its canonical form.
        /// Zero index is reserved for the uninitialised string.
        /// </summary>
        public static IList<string> CanonicalForms
        {
            get { return g_canonicalForms; }
            set
            {
                if (value == null) throw new ArgumentNullException("Cannot set null list as canonical forms");
                if (value.Count == 0 || value[0] != null) throw new ArgumentException("First canonical form must be null");
                g_canonicalForms = value.ToList();
                g_canonicalFormsIndices.Clear();
                for (int i = 1; i < g_canonicalForms.Count; ++i)
                    g_canonicalFormsIndices.Add(g_canonicalForms[i], i);
            }
        }

        /// <summary>
        /// The string.
        /// </summary>
        public string Value { get { return CanonicalForms[Canonical]; } }

        /// <summary>
        /// The canonical integer form of the string.
        /// </summary>
        public int Canonical { get; private set; }

        public bool IsNull { get { return Canonical == 0; } }

        static CanonicalString()
        {
            g_canonicalForms = new List<string> { null };
            g_canonicalFormsIndices = new Dictionary<string, int>(); // 'null' not added here but handled as a special case
            g_canRegister = true;
            Null = new CanonicalString(null);
        }

        /// <summary>
        /// Implicit conversion to <see cref="string"/>.
        /// </summary>
        public static implicit operator string(CanonicalString a) { return a.Value; }

        /// <summary>
        /// Explicit conversion from <see cref="string"/>.
        /// </summary>
        public static explicit operator CanonicalString(string a) { return new CanonicalString(a); }

        /// <summary>
        /// Explicit conversion from <see cref="int"/>.
        /// </summary>
        public static explicit operator CanonicalString(int a) { return new CanonicalString(a); }

        public static bool operator ==(CanonicalString a, CanonicalString b)
        {
            return a.Canonical == b.Canonical;
        }

        public static bool operator !=(CanonicalString a, CanonicalString b)
        {
            return a.Canonical != b.Canonical;
        }

        /// <summary>
        /// Chooses a canonical form for a string.
        /// </summary>
        public static void Register(string value)
        {
            new CanonicalString(value);
        }

        /// <summary>
        /// Ensures that each <see cref="CanonicalString"/> instance that is created
        /// after this method returns will be equal to one of the previously registered
        /// instances as listed in <see cref="CanonicalForms"/>.
        /// </summary>
        public static void DisableRegistering()
        {
            g_canRegister = false;
        }

        /// <summary>
        /// Creates a canonical string from a string value.
        /// </summary>
        public CanonicalString(string value)
            : this()
        {
            Canonical = GetCanonicalValue(value);
            if (Canonical < 0)
            {
                if (!g_canRegister) throw new InvalidOperationException("Registering previously unseen CanonicalString instances has been disabled");
                Canonical = CanonicalForms.Count;
                CanonicalForms.Add(value);
                g_canonicalFormsIndices.Add(value, Canonical);
            }
        }

        /// <summary>
        /// Creates a canonical string from its canonical form.
        /// </summary>
        public CanonicalString(int canonical)
            : this()
        {
            if (canonical < 0 || canonical >= CanonicalForms.Count)
                throw new InvalidOperationException("No canonical string has canonical form " + canonical);
            Canonical = canonical;
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CanonicalString)) return false;
            return this == (CanonicalString)obj;
        }

        public override int GetHashCode()
        {
            return Canonical;
        }

        private static int GetCanonicalValue(string value)
        {
            if (value == null) return 0; // 'null' handled separately because Dictionary cannot store it
            int canonical;
            if (g_canonicalFormsIndices.TryGetValue(value, out canonical))
                return canonical;
            return -1;
        }
    }
}
