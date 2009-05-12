using System.Collections.Generic;

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
        /// <summary>
        /// The index of a string in this list is its canonical form.
        /// </summary>
        static List<string> canonicalForms = new List<string>();

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
            Canonical = canonicalForms.IndexOf(value);
            if (Canonical < 0)
            {
                Canonical = canonicalForms.Count;
                canonicalForms.Add(value);
#if DEBUG
                if (canonicalForms.Count == 100)
                    Log.Write("WARNING: 100 distinct CanonizedString values and counting... Consider using a Dictionary");
#endif
            }
        }
    }
}
