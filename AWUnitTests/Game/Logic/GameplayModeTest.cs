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

        [SetUp]
        public void Setup()
        {
            Spectator.CreateStatsData = spectator => new MockStats();
            _gameplayMode = new GameplayMode();
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
        public void TestTeamCondense()
        {
            AssertTeamBalancing(new[] { _player1 }, new[] { _player2 }, new[] { _player3 }, new[] { _player4 });
        }

        [Test]
        public void TestTeamSpreadToEmptyTeams()
        {
            AssertTeamBalancing(new[] { _player1, _player2, _player3, _player4 });
        }

        [Test]
        public void TestTeamSpreadToNonemptyTeams()
        {
            AssertTeamBalancing(new[] { _player1, _player2, _player3 }, new[] { _player4 });
        }

        [Test]
        public void TestTeamCondenseAndSpread()
        {
            AssertTeamBalancing(new[] { _player1, _player2, _player3, _player4 }, new[] { _player5 }, new[] { _player6 });
        }

        [Test]
        public void TestTeamsBalanced()
        {
            AssertTeamBalancing(new[] { _player1, _player2 }, new[] { _player3 });
            AssertTeamBalancing(new[] { _player1 }, new[] { _player2 , _player3 });
        }

        private void AssertTeamBalancing(params Spectator[][] teams)
        {
            foreach (var x in teams.Zip(Teams, (specs, team) => new { specs, team }))
                foreach (var spec in x.specs) spec.AssignTeam(x.team);
            var ops = _gameplayMode.BalanceTeams(Teams).ToArray();
            var remainingTeams = (
                from team in Teams
                let originalSpecs = team.Members.Select(spec => spec.ID)
                let lostSpecs = ops.Where(op => op.Item2 != team.ID).Select(op => op.Item1)
                let gainedSpecs = ops.Where(op => op.Item2 == team.ID).Select(op => op.Item1)
                let remainingSpecs = originalSpecs.Except(lostSpecs).Union(gainedSpecs).ToArray()
                where remainingSpecs.Any()
                select remainingSpecs).ToArray();
            Assert.AreEqual(2, remainingTeams.Length);
            Assert.AreEqual(remainingTeams[0].Length, remainingTeams[1].Length, 1);
        }
    }
}
