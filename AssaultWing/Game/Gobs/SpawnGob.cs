using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Geometric;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A spawn type as one of many possible choices.
    /// </summary>
    public struct SpawnType : INetworkSerializable
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

        #region INetworkSerializable Members

        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((float)weight);
                writer.Write((CanonicalString)spawnTypeName);
            }
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                weight = reader.ReadSingle();
                spawnTypeName = (CanonicalString)reader.ReadInt32();
            }
        }

        #endregion
    }

    /// <summary>
    /// An area that creates gobs.
    /// </summary>
    public class SpawnGob : Gob
    {
        #region SpawnGob fields

        /// <summary>
        /// Area in which spawning takes place.
        /// </summary>
        [RuntimeState]
        private IGeomPrimitive _spawnArea;

        /// <summary>
        /// Time between spawns, in seconds of game time.
        /// </summary>
        [RuntimeState]
        private float _spawnInterval;

        /// <summary>
        /// Name of the type of gobs to spawn.
        /// </summary>
        [RuntimeState]
        private SpawnType[] _spawnTypes;

        /// <summary>
        /// Time of next spawn, in game time.
        /// </summary>
        private TimeSpan _nextSpawn;

        #endregion SpawnGob fields

        public override BoundingSphere DrawBounds { get { return new BoundingSphere(); } }

        /// <summary>
        /// This constructor is only for serialisation.
        /// </summary>
        public SpawnGob()
        {
            _spawnArea = new Everything();
            _spawnInterval = 20;
            _spawnTypes = new SpawnType[1] { new SpawnType() };
            _nextSpawn = new TimeSpan(0, 1, 2);
        }

        public SpawnGob(CanonicalString typeName)
            : base(typeName)
        {
        }

        public override void Activate()
        {
            _nextSpawn = Arena.TotalTime + TimeSpan.FromSeconds(_spawnInterval);
            base.Activate();
        }

        private CanonicalString GetRandomSpawnType()
        {
            float massTotal = _spawnTypes.Sum(spawnType => spawnType.weight);
            float choice = RandomHelper.GetRandomFloat(0, massTotal);
            massTotal = 0;
            SpawnType poss = new SpawnType();
            for (int i = 0; i < _spawnTypes.Length && choice >= massTotal; ++i)
            {
                poss = _spawnTypes[i];
                massTotal += poss.weight;
            }
            return poss.spawnTypeName;
        }

        public override void Update()
        {
            while (_nextSpawn <= Arena.TotalTime)
            {
                _nextSpawn = Arena.TotalTime + TimeSpan.FromSeconds(_spawnInterval);
                Gob.CreateGob<Gob>(Game, GetRandomSpawnType(), newGob =>
                {
                    var spawnPos = Arena.GetFreePosition(newGob, _spawnArea);
                    newGob.ResetPos(spawnPos, Vector2.Zero, Gob.DEFAULT_ROTATION);
                    Arena.Gobs.Add(newGob);
                });
            }
            base.Update();
        }

        #region Methods related to serialisation

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                // TODO: Serialise 'spawnArea'
                writer.Write((float)_spawnInterval);
                writer.Write((int)_spawnTypes.Length);
                foreach (var spawnType in _spawnTypes)
                    spawnType.Serialize(writer, SerializationModeFlags.ConstantData);
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                // TODO: Deserialise 'spawnArea'
                _spawnInterval = reader.ReadSingle();
                int spawnTypesCount = reader.ReadInt32();
                _spawnTypes = new SpawnType[spawnTypesCount];
                for (int i = 0; i < spawnTypesCount; ++i)
                    _spawnTypes[i].Deserialize(reader, SerializationModeFlags.ConstantData, framesAgo);
            }
        }

        #endregion Methods related to serialisation
    }
}
