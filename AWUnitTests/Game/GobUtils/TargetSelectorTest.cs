using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using AW2.Helpers;

namespace AW2.Game.GobUtils
{
    [TestFixture]
    public class TargetSelectorTest
    {
        private Player _player1, _player2;
        private Gob _source, _hostileGob1, _hostileGob2, _neutralGob, _friendlyGob;
        private Arena _arena;
        private TargetSelector _targetSelector;

        [SetUp]
        public void Setup()
        {
            _player1 = new Player(null, "Player 1", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player2 = new Player(null, "Player 2", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _arena = new Arena();
            _source = new Gob { ID = 1, Owner = _player1, MaxDamageLevel = 100, Arena = _arena, Pos = Vector2.Zero };
            _hostileGob1 = new Gob { ID = 2, Owner = _player2, MaxDamageLevel = 100, Arena = _arena };
            _hostileGob2 = new Gob { ID = 3, Owner = _player2, MaxDamageLevel = 100, Arena = _arena };
            _neutralGob = new Gob { ID = 4, Owner = null, MaxDamageLevel = 100, Arena = _arena };
            _friendlyGob = new Gob { ID = 5, Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _targetSelector = new TargetSelector(700);
        }

        [Test]
        public void TestTargetAngleWeight()
        {
            _targetSelector.AngleWeight = 2.5f;
            _targetSelector.MaxAngle = MathHelper.Pi;
            var gobs = new[] { _hostileGob1, _hostileGob2 };
            _hostileGob1.Pos = new Vector2(500, 0);
            _hostileGob2.Pos = new Vector2(0, 110);
            Assert.AreSame(_hostileGob1, _targetSelector.ChooseTarget(gobs, _source, 0));
            _hostileGob2.Pos = new Vector2(0, 90);
            Assert.AreSame(_hostileGob2, _targetSelector.ChooseTarget(gobs, _source, 0));
            _hostileGob2.Pos = new Vector2(-60, 0);
            Assert.AreSame(_hostileGob1, _targetSelector.ChooseTarget(gobs, _source, 0));
            _hostileGob2.Pos = new Vector2(-40, 0);
            Assert.AreSame(_hostileGob2, _targetSelector.ChooseTarget(gobs, _source, 0));
        }

        [Test]
        public void TestMaxTargetAngle()
        {
            var gobs = new[] { _hostileGob1 };
            Action<Vector2> accept = pos => { _hostileGob1.Pos = pos; Assert.NotNull(_targetSelector.ChooseTarget(gobs, _source, 0)); };
            Action<Vector2> reject = pos => { _hostileGob1.Pos = pos; Assert.IsNull(_targetSelector.ChooseTarget(gobs, _source, 0)); };
            Action<Action<Vector2>, IEnumerable<Vector2>> apply = (act, list) => { foreach (var x in list) { Console.WriteLine(x); act(x); } };
            _targetSelector.MaxAngle = MathHelper.PiOver2;
            apply(accept, new[] { new Vector2(100, 0), new Vector2(100, 100), new Vector2(1, 100), new Vector2(1, -100), new Vector2(100, -100), Vector2.Zero });
            apply(reject, new[] { new Vector2(-100, 0), new Vector2(-100, 100), new Vector2(-1, 100), new Vector2(-1, -100), new Vector2(-100, -100) });
            _targetSelector.MaxAngle = MathHelper.Pi;
            apply(accept, new[] { new Vector2(100, 0), new Vector2(100, 100), new Vector2(1, 100), new Vector2(1, -100), new Vector2(100, -100), Vector2.Zero,
                                  new Vector2(-100, 0), new Vector2(-100, 100), new Vector2(-1, 100), new Vector2(-1, -100), new Vector2(-100, -100)});
        }
    }
}
