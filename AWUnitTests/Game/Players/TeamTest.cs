using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AW2.Game.Gobs;
using AW2.Game.GobUtils;
using AW2.Helpers;

namespace AW2.Game.Players
{
    [TestFixture]
    public class TeamTest
    {
        private Team _avengers, _xmen;
        private Player _player1, _player2, _player3;
        private Ship _ship1, _ship2, _ship3;
        private Arena _arena;

        [SetUp]
        public void Setup()
        {
            CanonicalString.IsForLocalUseOnly = true;
            Spectator.CreateStatsData = spectator => new MockStats();
            _arena = new Arena();
            _ship1 = new Ship((CanonicalString)"Bugger") { Owner = _player1, MaxDamageLevel = 100, Arena = _arena };
            _ship2 = new Ship((CanonicalString)"Bugger") { Owner = _player2, MaxDamageLevel = 100, Arena = _arena };
            _ship3 = new Ship((CanonicalString)"Bugger") { Owner = _player3, MaxDamageLevel = 100, Arena = _arena };
            _avengers = new Team("Avengers");
            _xmen = new Team("X-Men");
            _player1 = new Player(null, "Player 1", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player2 = new Player(null, "Player 2", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            _player3 = new Player(null, "Player 3", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls());
            SeizeShip(_player1, _ship1);
            SeizeShip(_player2, _ship2);
            SeizeShip(_player3, _ship3);
        }

        [Test]
        public void TestAssignTeam()
        {
            Assert.AreEqual("Avengers", _avengers.Name);
            Assert.IsEmpty(_avengers.Members.ToArray());
            _player1.AssignTeam(_avengers);
            Assert.AreEqual(_avengers, _player1.Team);
            _player2.AssignTeam(_avengers);
            _player3.AssignTeam(_avengers);
            Assert.AreEqual(new[] { _player1, _player2, _player3 }, _avengers.Members.ToArray());
        }

        [Test]
        public void TestAssignTeamRepeatedly()
        {
            _player1.AssignTeam(_avengers);
            _player2.AssignTeam(_avengers);
            _player3.AssignTeam(_xmen);
            Assert.AreEqual(new[] { _player1, _player2 }, _avengers.Members.ToArray());
            Assert.AreEqual(new[] { _player3 }, _xmen.Members.ToArray());
            _player1.AssignTeam(_xmen);
            Assert.AreEqual(new[] { _player2 }, _avengers.Members.ToArray());
            Assert.AreEqual(new[] { _player3, _player1 }, _xmen.Members.ToArray());
            _player3.AssignTeam(_xmen);
            Assert.AreEqual(new[] { _player2 }, _avengers.Members.ToArray());
            Assert.AreEqual(new[] { _player3, _player1 }, _xmen.Members.ToArray());
        }

        [Test]
        public void TestTeamScoring()
        {
            _player1.AssignTeam(_avengers);
            _player2.AssignTeam(_avengers);
            _player3.AssignTeam(_xmen);
            AssertKills(player1: 0, player2: 0, player3: 0, avengers: 0, xmen: 0);
            AssertDeaths(player1: 0, player2: 0, player3: 0, avengers: 0, xmen: 0);
            new Coroner(new DamageInfo(_ship3).Bind(_ship1));
            AssertKills(player1: 0, player2: 0, player3: 1, avengers: 0, xmen: 1);
            AssertDeaths(player1: 1, player2: 0, player3: 0, avengers: 1, xmen: 0);
            new Coroner(new DamageInfo(_ship2).Bind(_ship1));
            AssertKills(player1: 0, player2: 0, player3: 1, avengers: 0, xmen: 1);
            AssertDeaths(player1: 2, player2: 0, player3: 0, avengers: 2, xmen: 0);
        }

        [Test]
        public void TestTeamScoringAfterTeamSwitch()
        {
            _player1.AssignTeam(_avengers);
            _player2.AssignTeam(_avengers);
            _player3.AssignTeam(_xmen);
            new Coroner(new DamageInfo(_ship3).Bind(_ship1));
            new Coroner(new DamageInfo(_ship2).Bind(_ship1));
            AssertKills(player1: 0, player2: 0, player3: 1, avengers: 0, xmen: 1);
            AssertDeaths(player1: 2, player2: 0, player3: 0, avengers: 2, xmen: 0);
            _player2.AssignTeam(_xmen);
            AssertKills(player1: 0, player2: 0, player3: 1, avengers: 0, xmen: 1);
            AssertDeaths(player1: 2, player2: 0, player3: 0, avengers: 2, xmen: 0);
            new Coroner(new DamageInfo(_ship2).Bind(_ship1));
            AssertKills(player1: 0, player2: 1, player3: 1, avengers: 0, xmen: 2);
            AssertDeaths(player1: 3, player2: 0, player3: 0, avengers: 3, xmen: 0);
            new Coroner(new DamageInfo(_ship3).Bind(_ship2));
            AssertKills(player1: 0, player2: 1, player3: 1, avengers: 0, xmen: 2);
            AssertDeaths(player1: 3, player2: 1, player3: 0, avengers: 3, xmen: 1);
        }

        /// <summary>
        /// Like <see cref="Player.SeizeShip"/> but does less so that an AssaultWingCore instance isn't required.
        /// </summary>
        private void SeizeShip(Player player, Ship ship)
        {
            player.Ship = ship;
            ship.Owner = player;
        }

        private void AssertKills(int player1, int player2, int player3, int avengers, int xmen)
        {
            if (_player1.ArenaStatistics.Kills != player1 ||
                _player2.ArenaStatistics.Kills != player2 ||
                _player3.ArenaStatistics.Kills != player3 ||
                _avengers.ArenaStatistics.Kills != avengers ||
                _xmen.ArenaStatistics.Kills != xmen)
                Assert.Fail(
                    "Kills expected player1: {0}, player2: {1}, player3: {2}, avengers: {3}, xmen: {4}\n" +
                    "but was        player1: {5}, player2: {6}, player3: {7}, avengers: {8}, xmen: {9}",
                    player1, player2, player3, avengers, xmen,
                    _player1.ArenaStatistics.Kills, _player2.ArenaStatistics.Kills, _player3.ArenaStatistics.Kills,
                    _avengers.ArenaStatistics.Kills, _xmen.ArenaStatistics.Kills);
        }

        private void AssertDeaths(int player1, int player2, int player3, int avengers, int xmen)
        {
            if (_player1.ArenaStatistics.Deaths != player1 ||
                _player2.ArenaStatistics.Deaths != player2 ||
                _player3.ArenaStatistics.Deaths != player3 ||
                _avengers.ArenaStatistics.Deaths != avengers ||
                _xmen.ArenaStatistics.Deaths != xmen)
                Assert.Fail(
                    "Deaths expected player1: {0}, player2: {1}, player3: {2}, avengers: {3}, xmen: {4}\n" +
                    "but was        player1: {5}, player2: {6}, player3: {7}, avengers: {8}, xmen: {9}",
                    player1, player2, player3, avengers, xmen,
                    _player1.ArenaStatistics.Deaths, _player2.ArenaStatistics.Deaths, _player3.ArenaStatistics.Deaths,
                    _avengers.ArenaStatistics.Deaths, _xmen.ArenaStatistics.Deaths);
        }
    }
}
