using System.Collections.Generic;
using System;

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
        static IList<string> canonicalForms = new List<string> { null };

        /// <summary>
        /// The <see cref="CanonicalString"/> instance corresponding to the null <see cref="string"/>.
        /// </summary>
        public static readonly CanonicalString Null = new CanonicalString(0);

        /// <summary>
        /// The index of a string in this list is its canonical form.
        /// Zero index is reserved for the uninitialised string.
        /// </summary>
        public static IList<string> CanonicalForms
        {
            get { return canonicalForms; }
            set
            {
                if (value == null) throw new ArgumentNullException("Cannot set null list as canonical forms");
                canonicalForms = new List<string>(value);
                if (canonicalForms.Count == 0 || canonicalForms[0] != null)
                    throw new ArgumentException("First canonical form must be null");
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

        /// <summary>
        /// Chooses a canonical form for a string.
        /// </summary>
        public static void Register(string value)
        {
            new CanonicalString(value);
        }

        /// <summary>
        /// Creates a canonical string from a string value.
        /// </summary>
        public CanonicalString(string value)
            : this()
        {
            Canonical = CanonicalForms.IndexOf(value);
            if (Canonical < 0)
            {
                Canonical = CanonicalForms.Count;
                CanonicalForms.Add(value);
#if DEBUG
                if ((CanonicalForms.Count % 100) == 0)
                    Log.Write("WARNING: " + CanonicalForms.Count + " distinct CanonicalString values and counting... Consider using a Dictionary");
#endif
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

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        public override string ToString()
        {
            return Value;
        }
    }
}
