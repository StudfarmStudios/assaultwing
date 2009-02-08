using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// An area that creates gobs.
    /// </summary>
    class SpawnGob : Gob
    {
        #region SpawnGob fields

        /// <summary>
        /// Area in which spawning takes place.
        /// </summary>
        [RuntimeState]
        IGeomPrimitive spawnArea;

        /// <summary>
        /// Time between spawns, in seconds of game time.
        /// </summary>
        [RuntimeState]
        float spawnInterval;

        /// <summary>
        /// Name of the type of gobs to spawn.
        /// </summary>
        [RuntimeState]
        string spawnTypeName;

        /// <summary>
        /// Time of next spawn, in game time.
        /// </summary>
        TimeSpan nextSpawn;

        #endregion SpawnGob fields

        /// <summary>
        /// Creates an uninitialised gob.
        /// </summary>
        /// This constructor is only for serialisation.
        public SpawnGob()
            : base()
        {
            spawnArea = new Everything();
            spawnInterval = 20;
            spawnTypeName = "dummygob";
            nextSpawn = new TimeSpan(0, 1, 2);
        }

        /// <summary>
        /// Creates a spawn gob.
        /// </summary>
        /// <param name="typeName">The type of the spawn gob.</param>
        public SpawnGob(string typeName)
            : base(typeName)
        {
        }

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            nextSpawn = AssaultWing.Instance.GameTime.TotalGameTime + TimeSpan.FromSeconds(spawnInterval);
            base.Activate();
        }

        /// <summary>
        /// Updates the spawn area, perhaps creating a new gob.
        /// </summary>
        public override void Update()
        {
            TimeSpan nowTime = AssaultWing.Instance.GameTime.TotalGameTime;
            while (nextSpawn <= nowTime)
            {
                nextSpawn = nowTime + TimeSpan.FromSeconds(spawnInterval);
                DataEngine data = (DataEngine)AssaultWing.Instance.Services.GetService(typeof(DataEngine));
                Gob.CreateGob(spawnTypeName, newGob =>
                {
                    Vector2 spawnPos = physics.GetFreePosition(newGob, spawnArea);
                    newGob.Pos = spawnPos;
                    data.AddGob(newGob);
                });
            }
            base.Update();
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
                writer.Write((float)spawnInterval);
                writer.Write((string)spawnTypeName, 32, true);
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
                spawnInterval = reader.ReadSingle();
                spawnTypeName = reader.ReadString(32);
            }
        }

        #endregion Methods related to serialisation

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

        #endregion IConsistencyCheckable Members
    }
}
