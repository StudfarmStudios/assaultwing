using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Game.Gobs;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.GobUtils
{
    /// <summary>
    /// Visualizes amount of damage of a gob for example by enabling a smoke effect
    /// when the gob is very damaged.
    /// </summary>
    [LimitedSerialization]
    public class CoughEngine
    {
        private const float RELATIVE_COUGH_TRESHOLD = 0.8f;

        [TypeParameter, ShallowCopy]
        private CanonicalString[] _pengNames;
        private List<Peng> _pengs;
        private bool _pengsEnabled;

        public Gob Owner { get; private set; }

        /// <summary>
        /// Only for serialization.
        /// </summary>
        public CoughEngine()
        {
            _pengNames = new CanonicalString[0];
        }

        public void Activate(Gob owner)
        {
            Owner = owner;
            _pengs = new List<Peng>();
            foreach (var name in _pengNames)
            {
                Gob.CreateGob<Peng>(Owner.Game, name, peng =>
                {
                    peng.Emitter.Pause();
                    peng.Leader = Owner;
                    Owner.Arena.Gobs.Add(peng);
                    _pengs.Add(peng);
                });
            }
        }

        public void Update()
        {
            var coughArgument = MathHelper.Clamp(
                (Owner.DamageLevel / Owner.MaxDamageLevel - RELATIVE_COUGH_TRESHOLD) / (1 - RELATIVE_COUGH_TRESHOLD),
                0, 1);
            var mustEnable = !_pengsEnabled && coughArgument > 0;
            var mustDisable = _pengsEnabled && coughArgument == 0;
            _pengsEnabled = coughArgument > 0;
            foreach (var coughEngine in _pengs)
            {
                coughEngine.Input = coughArgument;
                if (mustEnable) coughEngine.Emitter.Resume();
                if (mustDisable) coughEngine.Emitter.Pause();
            }
        }
    }
}
