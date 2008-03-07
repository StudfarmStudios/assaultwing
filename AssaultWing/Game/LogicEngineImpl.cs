using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Events;
using AW2.UI;
using AW2.Game.Particles;

namespace AW2.Game
{
    /// <summary>
    /// Basic implementation of game logic.
    /// </summary>
    class LogicEngineImpl : GameComponent, LogicEngine
    {
        /// <summary>
        /// Time for creating another bonus.
        /// </summary>
        TimeSpan nextBonus;

        public LogicEngineImpl(Microsoft.Xna.Framework.Game game) : base(game)
        {
        }

        public override void Initialize()
        {
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            Helpers.Log.Write("Loading user-defined types");
            TypeLoader gobLoader = new TypeLoader(typeof(Gob), "gobdefs");
            Gob[] gobs = (Gob[])gobLoader.LoadAllTypes();
            foreach (Gob gob in gobs)
                data.AddTypeTemplate(typeof(Gob), gob.TypeName, gob);
            TypeLoader weaponLoader = new TypeLoader(typeof(Weapon), "weapondefs");
            Weapon[] weapons = (Weapon[])weaponLoader.LoadAllTypes();
            foreach (Weapon weapon in weapons)
                data.AddTypeTemplate(typeof(Weapon), weapon.TypeName, weapon);
            TypeLoader particleLoader = new TypeLoader(typeof(ParticleEngine), "particledefs");
            ParticleEngine[] particleEngines = (ParticleEngine[])particleLoader.LoadAllTypes();
            foreach (ParticleEngine particleEngine in particleEngines)
                data.AddTypeTemplate(typeof(Gob), particleEngine.TypeName, particleEngine);
            TypeLoader arenaLoader = new TypeLoader(typeof(Arena), "arenas");
            Arena[] arenas = (Arena[])arenaLoader.LoadAllTypes();
            foreach (Arena arena in arenas)
                data.AddArena(arena.Name, arena);

            base.Initialize();
        }

        /// <summary>
        /// Resets the logic engine for a new arena.
        /// </summary>
        public void Reset()
        {
            nextBonus = new TimeSpan(0, 0, 10);
        }

        /// <summary>
        /// Performs game logic.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            EventEngine eventer = (EventEngine)Game.Services.GetService(typeof(EventEngine));
            PhysicsEngine physics = (PhysicsEngine)Game.Services.GetService(typeof(PhysicsEngine));
            physics.TimeStep = gameTime;
            
            // Process player input.
            bool doneThrust = false;
            bool doneLeft = false;
            bool doneRight = false;
            bool doneDown = false;
            bool doneFire1 = false;
            bool doneFire2 = false;
            bool doneExtra = false;
            for (PlayerControlEvent controlEve = eventer.GetEvent<PlayerControlEvent>(); controlEve != null;
                controlEve = eventer.GetEvent<PlayerControlEvent>())
            {
                Player player = data.GetPlayer(controlEve.PlayerName);
                if (player == null) continue;
                switch (controlEve.ControlType)
                {
                    case PlayerControlType.Thrust:
                        if (doneThrust) break;
                        doneThrust = true;
                        player.Ship.Thrust(controlEve.Force);
                        break;
                    case PlayerControlType.Left:
                        if (doneLeft) break;
                        doneLeft = true;
                        player.Ship.TurnLeft(controlEve.Force);
                        break;
                    case PlayerControlType.Right:
                        if (doneRight) break;
                        doneRight = true;
                        player.Ship.TurnRight(controlEve.Force);
                        break;
                    case PlayerControlType.Down:
                        if (doneDown) break;
                        doneDown = true;
                        // This has no effect during a game.
                        break;
                    case PlayerControlType.Fire1:
                        if (doneFire1) break;
                        doneFire1 = true;
                        if (controlEve.Pulse)
                            player.Ship.Fire1();
                        break;
                    case PlayerControlType.Fire2:
                        if (doneFire2) break;
                        doneFire2 = true;
                        if (controlEve.Pulse)
                            player.Ship.Fire2();
                        break;
                    case PlayerControlType.Extra:
                        if (doneExtra) break;
                        doneExtra = true;
                        if (controlEve.Pulse)
                            player.Ship.DoExtra();
                        break;
                    default:
                        throw new ArgumentException("Unexpected player control type " + 
                            Enum.GetName(typeof(PlayerControlType), controlEve.ControlType));
                }
            }

            /* UNDONE: Bonuses are more practical to handle straight by player's bonus time counters.
            // Process bonus events.
            for (BonusExpiryEvent eve = eventer.GetEvent<BonusExpiryEvent>(); eve != null;
                eve = eventer.GetEvent<BonusExpiryEvent>())
            {
                Player player = data.GetPlayer(eve.PlayerName);
                if (player == null) continue;
                switch (eve.Bonus)
                {
                    case PlayerBonus.Weapon1LoadTime:
                        player.DeupgradeWeapon1LoadTime();
                        break;
                    case PlayerBonus.Weapon2LoadTime:
                        player.DeupgradeWeapon2LoadTime();
                        break;
                    case PlayerBonus.Weapon1Upgrade:
                        player.DeupgradeWeapon1();
                        break;
                    case PlayerBonus.Weapon2Upgrade:
                        player.DeupgradeWeapon2();
                        break;
                    default:
                        Helpers.Log.Write("Warning: Don't know how to handle BonusExpiryEvent for bonus " +
                            eve.Bonus);
                        break;
                }
            }
            */

            // Player bonus expirations.
            data.ForEachPlayer(delegate(Player player)
            {
                foreach (PlayerBonus playerBonus in Enum.GetValues(typeof(PlayerBonus)))
                    if (playerBonus != PlayerBonus.None &&
                        (player.Bonuses & playerBonus) != 0 &&
                        player.BonusTimeouts[playerBonus] <= AssaultWing.Instance.GameTime.TotalGameTime)
                        switch (playerBonus)
                        {
                            case PlayerBonus.Weapon1LoadTime:
                                player.DeupgradeWeapon1LoadTime();
                                break;
                            case PlayerBonus.Weapon2LoadTime:
                                player.DeupgradeWeapon2LoadTime();
                                break;
                            case PlayerBonus.Weapon1Upgrade:
                                player.DeupgradeWeapon1();
                                break;
                            case PlayerBonus.Weapon2Upgrade:
                                player.DeupgradeWeapon2();
                                break;
                            default:
                                Helpers.Log.Write("Warning: Don't know how to handle expiration of player bonus " +
                                    playerBonus);
                                break;
                        }
            });

            // Update gobs.
            Action<Gob> updateGob = delegate(Gob gob)
            {
                gob.Update();
            };
            data.ForEachGob(updateGob);

            // Update weapons.
            Action<Weapon> updateWeapon = delegate(Weapon weapon)
            {
                weapon.Update();
            };
            data.ForEachWeapon(updateWeapon);

            // Update particle engines.
            Action<ParticleEngine> updateParticleEngine = delegate(ParticleEngine pEng)
            {
                pEng.Update();
            };
            data.ForEachParticleEngine(updateParticleEngine);

            // Create new bonuses.
            if (nextBonus <= physics.TimeStep.TotalGameTime)
            {
                nextBonus = physics.TimeStep.TotalGameTime + new TimeSpan(0, 0, 10);
                Gob bonus = Gob.CreateGob("bonus");
                bonus.Pos = physics.GetFreePosition(bonus, null);
                data.AddGob(bonus);
            }

            // Check for receptor collisions.
            physics.MovesDone();

            // Check for game end.
            int playersAlive = 0;
            Player alive = null; // any player who's alive
            data.ForEachPlayer(delegate(Player player)
            {
                if (player.Lives > 0)
                    ++playersAlive;
                alive = player;
            });
            if (playersAlive <= 1)
            {
                // TODO: End game by displaying dialog.
                AssaultWing.Instance.ToggleDialog();
            }
        }
    }
}
