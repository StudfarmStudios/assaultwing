using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using System.Linq;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A spawn type as one of many possible choices.
    /// </summary>
    public struct SpawnType
    {
        /// <summary>
        /// The probability weight of this spawn type 
        /// relative to other spawn type possibilities.
        /// </summary>
        public float weight;

        /// <summary>
        /// Spawn Type that is selected
        /// </summary>
        public CanonicalString spawnTypeName;

        /// <summary>
        /// Creates a new spawn type.
        /// </summary>
        /// <param name="weight">The probability weight of this spawn type 
        /// relative to other spawn types.</param>
        /// <param name="spawnTypeName">Spawn type to be created in case this spawn type is chosen.</param>
        public SpawnType(float weight, CanonicalString spawnTypeName)
        {
            this.weight = weight;
            this.spawnTypeName = spawnTypeName;
        }
    }

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
        SpawnType[] spawnTypes;

        /// <summary>
        /// Time of next spawn, in game time.
        /// </summary>
        TimeSpan nextSpawn;

        #endregion SpawnGob fields

        /// <summary>
        /// Bounding volume of the 3D visuals of the gob, in world coordinates.
        /// </summary>
        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }

        /// <summary>
        /// Creates an uninitialised gob.
        /// </summary>
        /// This constructor is only for serialisation.
        public SpawnGob()
            : base()
        {
            spawnArea = new Everything();
            spawnInterval = 20;
            spawnTypes = new SpawnType[1]{new SpawnType()};
            nextSpawn = new TimeSpan(0, 1, 2);
        }

        /// <summary>
        /// Creates a spawn gob.
        /// </summary>
        /// <param name="typeName">The type of the spawn gob.</param>
        public SpawnGob(CanonicalString typeName)
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
        /// returns a random SpawnType
        /// </summary>
        private CanonicalString GetRandomSpawnType()
        {
            float massTotal = spawnTypes.Sum(spawnType => spawnType.weight);
            float choice = RandomHelper.GetRandomFloat(0, massTotal);
            massTotal = 0;
            SpawnType poss = new SpawnType();
            for (int i = 0; i < spawnTypes.Length && choice >= massTotal; ++i)
            {
                poss = spawnTypes[i];
                massTotal += poss.weight;
            }
            return poss.spawnTypeName;
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
                Gob.CreateGob(GetRandomSpawnType(), newGob =>
                {
                    Vector2 spawnPos = Arena.GetFreePosition(newGob, spawnArea);
                    newGob.Pos = spawnPos;
                    Arena.Gobs.Add(newGob);
                });
            }
            base.Update();
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
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                // TODO: Serialise 'spawnArea'
                writer.Write((float)spawnInterval);
                foreach(SpawnType spawnType in spawnTypes)
                    writer.Write((string)spawnType.spawnTypeName, 32, true);
            }
        }

        /// <summary>
        /// Deserialises the gob from a binary writer.
        /// </summary>
        /// <param name="reader">The reader where to read the serialised data.</param>
        /// <param name="mode">Which parts of the gob to deserialise.</param>
        public override void Deserialize(Net.NetworkBinaryReader reader, Net.SerializationModeFlags mode, TimeSpan messageAge)
        {
            base.Deserialize(reader, mode, messageAge);
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                // TODO: Deserialise 'spawnArea'
                spawnInterval = reader.ReadSingle();
                //TODO: refactor, this doesn't work correncty
                //spawnTypes[0].spawnTypeName = (CanonicalString)reader.ReadString(32);
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
