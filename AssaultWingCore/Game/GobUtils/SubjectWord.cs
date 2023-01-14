using System;

namespace AW2.Game.GobUtils
{
    public class SubjectWord : IFormattable
    {
        public static SubjectWord You { get; private set; }

        private string _nominative, _genetive, _genetivePronoun, _reflexivePronoun;

        static SubjectWord()
        {
            You = new SubjectWord(nominative: "you", genetive: "your", genetivePronoun: "your", reflexiveProunoun: "yourself");
        }

        public static SubjectWord FromProperNoun(string name)
        {
            var genetiveSuffix = name.Length > 0 && name[name.Length - 1] == 's' ? "'" : "'s";
            return new SubjectWord(name, name + genetiveSuffix, "his", "himself");
        }

        private SubjectWord(string nominative, string genetive, string genetivePronoun, string reflexiveProunoun)
        {
            _nominative = nominative;
            _genetive = genetive;
            _genetivePronoun = genetivePronoun;
            _reflexivePronoun = reflexiveProunoun;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            switch (format)
            {
                case null: return _nominative;
                case "genetive": return _genetive;
                case "genetivePronoun": return _genetivePronoun;
                case "reflexivePronoun": return _reflexivePronoun;
                default: throw new ArgumentException("Invalid format string '" + format + "'");
            }
        }
    }
}
