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
        /// Draws the gob's 3D graphics.
        /// </summary>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public override void Draw(Matrix view, Matrix projection)
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

        #region IConsistencyCheckable Members

        /// <summary>
        /// Makes the instance consistent in respect of fields marked with a
        /// limitation attribute.
        /// </summary>
        /// <param name="limitationAttribute">Check only fields marked with 
        /// this limitation attribute.</param>
        /// <see cref="Serialization"/>
        public override void MakeConsistent(Type limitationAttribute)
        {
            base.MakeConsistent(limitationAttribute);
            if (limitationAttribute == typeof(RuntimeStateAttribute))
            {
                // Make sure there's no null references.
                if (spawnArea == null)
                    spawnArea = new Everything();
            }
        }

        #region Methods related to serialisation

        /// <summary>
        /// Serialises the gob for to a binary writer.
        /// </summary>
        /// <param name="writer">The writer where to write the serialised data.</param>
        /// <param name="mode">Which parts of the gob to serialise.</param>
        public override void Serialize(Net.NetworkBinaryWriter writer, Net.SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode | AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                // TODO: Serialise 'spawnArea'
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        /// <param name="reader">The reader where to read the serialised data.</param>
        /// <param name="mode">Which parts of the gob to deserialise.</param>
        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode)
        {
            base.Deserialize(reader, mode);
            if ((mode | AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                // TODO: Deserialise 'spawnArea'
            }
        }

        #endregion Methods related to serialisation

        #endregion IConsistencyCheckable Members
    }
}
