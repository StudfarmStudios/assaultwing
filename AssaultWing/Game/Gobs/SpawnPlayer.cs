using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// An area where players' ships can be created.
    /// </summary>
    public class SpawnPlayer : Gob
    {
        #region Fields

        /// <summary>
        /// Area in which spawning takes place.
        /// </summary>
        [RuntimeState]
        IGeomPrimitive spawnArea;

        #endregion Fields

        /// <summary>
        /// Creates an uninitialised gob.
        /// </summary>
        /// This constructor is only for serialisation.
        public SpawnPlayer()
            : base()
        {
            spawnArea = new Everything();
        }

        /// <summary>
        /// Creates a player spawn area.
        /// </summary>
        /// <param name="typeName">The type of the player spawn area.</param>
        public SpawnPlayer(string typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
        }

        /// <summary>
        /// Draws the gob.
        /// </summary>
        /// Assumes that the sprite batch has been Begun already and will be
        /// Ended later by someone else.
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="spriteBatch">The sprite batch to draw sprites with.</param>
        public override void Draw(Matrix view, Matrix projection, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            // We're invisible.
        }

        #region Public interface

        /// <summary>
        /// Returns the safeness measure of the player spawn area.
        /// </summary>
        /// Safeness is not measured in any specific units.
        /// Returned values are mutually comparable;
        /// the larger the return value, the safer the spawn area is
        /// in the sense that it is less likely that a ship freshly
        /// born in the spawn area will be quickly under attack.
        public float GetSafeness()
        {
            DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));

            // Measure safeness by our minimum distance to players' ships.
            float safeness = float.MaxValue;
            data.ForEachPlayer(delegate(Player player)
            {
                if (player.Ship != null)
                    safeness = Math.Min(safeness, Geometry.Distance(new AW2.Helpers.Geometric.Point(player.Ship.Pos), spawnArea));
            });
            return safeness;
        }

        /// <summary>
        /// Positions a player's ship in the spawn as if it has just been born.
        /// </summary>
        /// <param name="ship">The ship to position.</param>
        public void Spawn(Ship ship)
        {
            ship.Pos = physics.GetFreePosition(ship, spawnArea);
        }

        #endregion Public interface
    }
}
