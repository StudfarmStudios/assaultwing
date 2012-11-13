using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using AW2.Game.Players;
using AW2.Helpers;

namespace AW2.Game.Logic
{
    [TestFixture]
    public class GameplayModeTest
    {
        private GameplayMode _gameplayMode;
        private Team _team1, _team2, _team3, _team4;
        private Player _player1, _player2, _player3, _player4, _player5, _player6;

        private IEnumerable<Team> Teams { get { return new[] { _team1, _team2, _team3, _team4 }; } }
        private IEnumerable<Spectator> Spectators { get { return new[] { _player1, _player2, _player3, _player4, _player5, _player6 }; } }

        [SetUp]
        public void Setup()
        {
            Spectator.CreateStatsData = spectator => new MockStats();
            _gameplayMode = new GameplayMode(lifeScore: 1, killScore: 4, deathScore: -2, damageCombatPoints: 0, bonusesCombatPoints: 0);
            _team1 = new Team("Avengers", null) { ID = 11 };
            _team2 = new Team("X-Men", null) { ID = 12 };
            _team3 = new Team("Autobots", null) { ID = 13 };
            _team4 = new Team("Decepticons", null) { ID = 14 };
            _player1 = new Player(null, "Player 1", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls()) { ID = 1 };
            _player2 = new Player(null, "Player 2", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls()) { ID = 2 };
            _player3 = new Player(null, "Player 3", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls()) { ID = 3 };
            _player4 = new Player(null, "Player 4", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls()) { ID = 4 };
            _player5 = new Player(null, "Player 5", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls()) { ID = 5 };
            _player6 = new Player(null, "Player 6", CanonicalString.Null, CanonicalString.Null, CanonicalString.Null, new UI.PlayerControls()) { ID = 6 };
        }

        [Test]
        public void TestScore()
        {
            _player1.ArenaStatistics.Kills = 7;
            _player1.ArenaStatistics.Deaths = 3;
            _player1.ArenaStatistics.Lives = 1;
            Assert.AreEqual(23, _gameplayMode.CalculateScore(_player1.ArenaStatistics));
        }

        [Test]
        public void TestRateLocally()
        {
            _player1.ArenaStatistics.Kills = 25;
            _player1.ArenaStatistics.Deaths = 1;
            _player2.ArenaStatistics.Kills = 25;
            _player2.ArenaStatistics.Deaths = 10;
            _player3.ArenaStatistics.Kills = 10;
            _player3.ArenaStatistics.Deaths = 5;
            _player4.ArenaStatistics.Kills = 5;
            _player4.ArenaStatistics.Deaths = 5;
            _player5.ArenaStatistics.Kills = 1;
            _player5.ArenaStatistics.Deaths = 10;
            _player6.ArenaStatistics.Kills = 0;
            _player6.ArenaStatistics.Deaths = 10;
            Assert.AreEqual(9, _gameplayMode.RateLocally(_player1, Spectators));
            Assert.AreEqual(8, _gameplayMode.RateLocally(_player2, Spectators));
            Assert.AreEqual(4, _gameplayMode.RateLocally(_player3, Spectators));
            Assert.AreEqual(3, _gameplayMode.RateLocally(_player4, Spectators));
            Assert.AreEqual(1, _gameplayMode.RateLocally(_player5, Spectators));
            Assert.AreEqual(1, _gameplayMode.RateLocally(_player6, Spectators));
        }

        [Test, Description("When there's been only a little action in an arena, local ratings should not vary much.")]
        public void TestRateLocallyUncertain()
        {
            _player1.ArenaStatistics.Kills = 2;
            _player1.ArenaStatistics.Deaths = 0;
            _player2.ArenaStatistics.Kills = 2;
            _player2.ArenaStatistics.Deaths = 2;
            _player3.ArenaStatistics.Kills = 0;
            _player3.ArenaStatistics.Deaths = 1;
            Assert.AreEqual(6, _gameplayMode.RateLocally(_player1, Spectators));
            Assert.AreEqual(5, _gameplayMode.RateLocally(_player2, Spectators));
            Assert.AreEqual(4, _gameplayMode.RateLocally(_player3, Spectators));
        }

        [Test]
        public void TestTeamCondense()
        {
            AssertTeamBalancing(5, new[] { _player1 }, new[] { _player2 }, new[] { _player3 }, new[] { _player4 });
        }

        [Test]
        public void TestTeamSpreadToEmptyTeams()
        {
            AssertTeamBalancing(5, new[] { _player1, _player2, _player3, _player4 });
        }

        [Test]
        public void TestTeamSpreadToNonemptyTeams()
        {
            AssertTeamBalancing(5, new[] { _player1, _player2, _player3 }, new[] { _player4 });
        }

        [Test]
        public void TestTeamCondenseAndSpread()
        {
            AssertTeamBalancing(5, new[] { _player1, _player2, _player3, _player4 }, new[] { _player5 }, new[] { _player6 });
        }

        [Test]
        public void TestTeamsBalanced()
        {
            AssertTeamBalancing(5, new[] { _player1, _player2 }, new[] { _player3 });
            AssertTeamBalancing(5, new[] { _player1 }, new[] { _player2, _player3 });
        }

        [Test]
        public void TestUnfairTeamBalancingPrevious()
        {
            _player1.PreviousArenaStatistics.Kills = 6;
            _player2.PreviousArenaStatistics.Deaths = 3;
            _player3.PreviousArenaStatistics.Deaths = 3;
            _player4.PreviousArenaStatistics.Deaths = 3;
            AssertTeamBalancing(6, new[] { _player1, _player2 }, new[] { _player3, _player4 });
        }

        [Test]
        public void TestUnfairTeamBalancingCurrent()
        {
            _player1.ArenaStatistics.Kills = 6;
            _player2.ArenaStatistics.Deaths = 3;
            _player3.ArenaStatistics.Deaths = 3;
            _player4.ArenaStatistics.Deaths = 3;
            AssertTeamBalancing(6, new[] { _player1, _player2 }, new[] { _player3, _player4 });
        }

        private void AssertTeamBalancing(int rateTolerance, params Spectator[][] teams)
        {
            foreach (var x in teams.Zip(Teams, (specs, team) => new { specs, team }))
                foreach (var spec in x.specs) spec.AssignTeam(x.team);
            var ops = _gameplayMode.BalanceTeams(Teams).ToArray();
            var remainingTeams = (
                from team in Teams
                let originalSpecs = team.Members.Select(spec => spec.ID)
                let lostSpecs = ops.Where(op => op.Item2 != team.ID).Select(op => op.Item1)
                let gainedSpecs = ops.Where(op => op.Item2 == team.ID).Select(op => op.Item1)
                let remainingSpecs = originalSpecs.Except(lostSpecs).Union(gainedSpecs).Select(FindSpectator).ToArray()
                where remainingSpecs.Any()
                select remainingSpecs).ToArray();
            Assert.AreEqual(2, remainingTeams.Length);
            Assert.AreEqual(
                remainingTeams[0].Sum(spec => _gameplayMode.RateLocally(spec, Spectators)),
                remainingTeams[1].Sum(spec => _gameplayMode.RateLocally(spec, Spectators)),
                rateTolerance);
        }

        private Spectator FindSpectator(int id)
        {
            return Spectators.First(spec => spec.ID == id);
        }
    }
}
