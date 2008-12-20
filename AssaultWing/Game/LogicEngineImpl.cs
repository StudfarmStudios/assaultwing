using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Events;
using AW2.UI;
using AW2.Game.Particles;
using Microsoft.Xna.Framework.Input;

namespace AW2.Game
{
    /// <summary>
    /// Basic implementation of game logic.
    /// </summary>
    class LogicEngineImpl : GameComponent, LogicEngine
    {
        Control escapeControl;

        public LogicEngineImpl(Microsoft.Xna.Framework.Game game) : base(game)
        {
            escapeControl = new KeyboardKey(Keys.Escape);
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
            TypeLoader particleLoader = new TypeLoader(typeof(Gob), "particledefs");
            Gob[] particleEngines = (Gob[])particleLoader.LoadAllTypes();
            foreach (Gob particleEngine in particleEngines)
                data.AddTypeTemplate(typeof(Gob), particleEngine.TypeName, particleEngine);
            TypeLoader arenaLoader = new TypeLoader(typeof(Arena), "arenas");
            Arena[] arenas = (Arena[])arenaLoader.LoadAllTypes();
            List<string> arenaNames = new List<string>();
            foreach (Arena arena in arenas)
                if (arena.Name != "dummyarena")
                    arenaNames.Add(arena.Name);
            data.ArenaPlaylist = arenaNames;
            base.Initialize();
        }

        /// <summary>
        /// Resets the logic engine for a new arena.
        /// </summary>
        public void Reset()
        {
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
            
            UpdateControls();

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

            // Update players.
            Action<Player> updatePlayer = delegate(Player player)
            {
                player.Update();
            };
            data.ForEachPlayer(updatePlayer);

            // Check for receptor collisions.
            physics.MovesDone();

            // Check for game end.
            int playersAlive = 0;
            data.ForEachPlayer(player => { if (player.Lives > 0) ++playersAlive; });
            if (playersAlive <= 1)
                AssaultWing.Instance.FinishArena();
        }

        /// <summary>
        /// Checks general game controls and reacts to them.
        /// </summary>
        private void UpdateControls()
        {
            DataEngine data = (DataEngine)Game.Services.GetService(typeof(DataEngine));
            EventEngine eventEngine = (EventEngine)Game.Services.GetService(typeof(EventEngine));

            // Check general game controls.
            if (escapeControl.Pulse)
            {
                AW2.Graphics.CustomOverlayDialogData dialogData = new AW2.Graphics.CustomOverlayDialogData(
                    "Quit to Main Menu? (Yes/No)",
                    new TriggeredCallback(TriggeredCallback.GetYesControl(),
                        delegate() { AssaultWing.Instance.ShowMenu(); }),
                    new TriggeredCallback(TriggeredCallback.GetNoControl(),
                        delegate() { AssaultWing.Instance.ResumePlay(); }));
                AssaultWing.Instance.ShowDialog(dialogData);
            }
        }
    }
}
