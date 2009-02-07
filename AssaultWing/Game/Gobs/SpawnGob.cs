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
                Gob newGob = Gob.CreateGob(spawnTypeName);
                Vector2 spawnPos = physics.GetFreePosition(newGob, spawnArea);
                newGob.Pos = spawnPos;
                data.AddGob(newGob);
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
