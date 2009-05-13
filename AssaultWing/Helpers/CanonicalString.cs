using System.Collections.Generic;
using System;

namespace AW2.Helpers
{
    /// <summary>
    /// A string that has a canonical form as an integer.
    /// </summary>
    /// The canonical form of a string is fixed the first time a
    /// <c>CanonizedString</c> instance is created of the string.
    /// From then on, all <c>CanonizedString</c> instances with
    /// the same string content will have the same canonical form.
    public struct CanonicalString
    {
        static IList<string> canonicalForms = new List<string>();

        /// <summary>
        /// The index of a string in this list is its canonical form.
        /// </summary>
        public static IList<string> CanonicalForms
        {
            get { return canonicalForms; }
            set
            {
                if (value == null) throw new ArgumentNullException("Cannot set null list as canonical forms");
                canonicalForms = new List<string>(value);
            }
        }

        /// <summary>
        /// The string.
        /// </summary>
        public string Value { get; private set; }

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
            Value = value;
            Canonical = CanonicalForms.IndexOf(value);
            if (Canonical < 0)
            {
                Canonical = CanonicalForms.Count;
                CanonicalForms.Add(value);
#if DEBUG
                if (CanonicalForms.Count == 100)
                    Log.Write("WARNING: 100 distinct CanonicalString values and counting... Consider using a Dictionary");
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
            Value = CanonicalForms[canonical];
        }
    }
}
