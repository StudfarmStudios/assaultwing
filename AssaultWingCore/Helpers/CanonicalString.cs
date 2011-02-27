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
    /// <remarks>Canonical forms are fixed when all <see cref="DisableRegistering"/>
    /// is called. Before that, all canonical strings must be registered by
    /// calling <see cref="Register"/>.</remarks>
    [SerializedType(typeof(string))]
    [System.Diagnostics.DebuggerDisplay("{Value}")]
    public struct CanonicalString
    {
        private static List<string> g_unregisteredCanonicalForms;
        private static Dictionary<string, int> g_canonicalFormsIndices;

        private int _canonical; // set only after CanonicalString registering is disabled

        /// <summary>
        /// The <see cref="CanonicalString"/> instance corresponding to the null <see cref="string"/>.
        /// </summary>
        public static readonly CanonicalString Null;

        /// <summary>
        /// If true, new <see cref="CanonicalString"/>s can be registered.
        /// If false, canonical forms are fixed and the <see cref="Canonical"/> property is available.
        /// </summary>
        public static bool CanRegister { get; private set; }

        /// <summary>
        /// If true, new CanonicalStrings may be registered.
        /// If false, after caonical forms are fixed they are ordered suitably for communicating
        /// between several Assault Wing processes.
        /// Set this property only at application startup.
        /// </summary>
        public static bool IsForLocalUseOnly { get; set; }

        /// <summary>
        /// The index of a string in this list is its canonical form.
        /// Zero index is reserved for the uninitialised string.
        /// </summary>
        public static List<string> CanonicalForms { get; private set; }

        /// <summary>
        /// The string.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// The canonical integer form of the string.
        /// </summary>
        public int Canonical
        {
            get
            {
                if (CanRegister && !IsForLocalUseOnly) throw new InvalidOperationException("Canonical forms are not yet fixed");
                if (_canonical < 0) _canonical = GetCanonicalValue(Value);
                return _canonical;
            }
        }

        public bool IsNull { get { return Canonical == 0; } }

        static CanonicalString()
        {
            g_unregisteredCanonicalForms = new List<string>();
            CanonicalForms = new List<string> { null };
            g_canonicalFormsIndices = new Dictionary<string, int>(); // 'null' not added here but handled as a special case
            CanRegister = true;
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
        /// instances as listed in <see cref="CanonicalForms"/>. This method fixes all
        /// canonical forms.
        /// </summary>
        /// <remarks>Does nothing if <see cref="IsForLocalUseOnly"/> is true.</remarks>
        public static void DisableRegistering()
        {
            if (IsForLocalUseOnly) return;
            CanRegister = false;
            CanonicalForms = new List<string> { null };
            CanonicalForms.AddRange(g_unregisteredCanonicalForms.Except(new string[] { null }).Distinct().OrderBy(v => v, StringComparer.InvariantCultureIgnoreCase));
            g_canonicalFormsIndices.Clear();
            for (int i = 1; i < CanonicalForms.Count; ++i)
                g_canonicalFormsIndices.Add(CanonicalForms[i], i);
            g_unregisteredCanonicalForms = null;
        }

        /// <summary>
        /// Creates a canonical string from a string value.
        /// </summary>
        public CanonicalString(string value)
            : this()
        {
            Value = value;
            if (CanRegister && !IsForLocalUseOnly)
            {
                _canonical = -1;
                g_unregisteredCanonicalForms.Add(value);
            }
            else
                _canonical = GetCanonicalValue(value);
        }

        /// <summary>
        /// Creates a canonical string from its canonical form.
        /// </summary>
        public CanonicalString(int canonical)
            : this()
        {
            if (CanRegister && !IsForLocalUseOnly) throw new InvalidOperationException("Canonical forms are not yet fixed");
            if (canonical < 0 || canonical >= CanonicalForms.Count)
                throw new InvalidOperationException("No CanonicalString has canonical form " + canonical);
            _canonical = canonical;
            Value = CanonicalForms[canonical];
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
            if (g_canonicalFormsIndices.TryGetValue(value, out canonical)) return canonical;
            if (IsForLocalUseOnly)
            {
                canonical = CanonicalForms.Count;
                CanonicalForms.Add(value);
                g_canonicalFormsIndices.Add(value, canonical);
                return canonical;
            }
            throw new InvalidOperationException("No canonical form for '" + value + "'");
        }
    }
}
