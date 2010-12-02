using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AW2.Helpers;
using AW2.Helpers.Serialization;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A wall with the triggerable action of moving to a set position.
    /// </summary>
    public class MovingWall : ActionGob
    {
        /// <summary>
        /// The name of the 3D model to draw the wall with.
        /// </summary>
        /// Note: This field overrides the type parameter <see cref="Gob.modelName"/>.
        [RuntimeState]
        private CanonicalString wallModelName;

        /// <summary>
        /// Collision primitives, translated according to the gob's location.
        /// </summary>
        /// Note: This field overrides the type parameter <see cref="Gob.collisionAreas"/>.
        [RuntimeState, LimitationSwitch(typeof(RuntimeStateAttribute), typeof(TypeParameterAttribute))]
        private CollisionArea[] wallCollisionAreas;

        /// <summary>
        /// Target of the move.
        /// </summary>
        [RuntimeState]
        private Vector2 targetPos;

        /// <summary>
        /// Amount of time it takes to move, in game time seconds.
        /// </summary>
        private const float movementTime = 2;

        /// <summary>
        /// Original starting position.
        /// </summary>
        private Vector2 startPos;

        private MovementCurve movingCurve;
        private TimeSpan startTime;
        private bool goingToTarget; // if false, then going to startPos

        /// <summary>
        /// Names of all models that this gob type will ever use.
        /// </summary>
        public override IEnumerable<CanonicalString> ModelNames
        {
            get { return base.ModelNames.Union(new CanonicalString[] { wallModelName }); }
        }

        /// <summary>
        /// Creates an uninitialised MovingWall.
        /// </summary>
        /// This constructor is only for serialisation.
        public MovingWall()
        {
            wallModelName = (CanonicalString)"dummymodel";
            wallCollisionAreas = new CollisionArea[0];
            targetPos = new Vector2(100, 200);
        }

        /// <summary>
        /// Creates a gob of the specified gob type.
        /// </summary>
        public MovingWall(CanonicalString typeName)
            : base(typeName)
        {
            Gravitating = false;
            wallModelName = (CanonicalString)"dummymodel";
            startTime = new TimeSpan(-1);
        }

        /// <summary>
        /// Triggers a predefined action on the ActionGob.
        /// </summary>
        public override void Act()
        {
            if ((Arena.TotalTime - startTime).TotalSeconds < 1) return;
            movingCurve.SetTarget(goingToTarget ? startPos : targetPos,
                Arena.TotalTime, movementTime, MovementCurve.Curvature.SlowFastSlow);
            goingToTarget = !goingToTarget;
            startTime = Arena.TotalTime;
        }

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            ModelName = wallModelName;
            _collisionAreas = wallCollisionAreas;
            foreach (var area in wallCollisionAreas) area.Owner = this;
            startPos = Pos;
            movingCurve = new MovementCurve(startPos);
            base.Activate();
        }

        public override void Update()
        {
            if (startTime.Ticks >= 0)
            {
                var nextPos = movingCurve.Evaluate(Arena.TotalTime);
                Move = (nextPos - Pos) / (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
                Move = Move.Clamp(0, 500); // limit movement speed to reasonable bounds
            }
            base.Update();
        }

        #region Methods related to serialisation

        public override void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            base.Serialize(writer, mode);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)wallModelName.Canonical);
                writer.Write((byte)wallCollisionAreas.Length);
                foreach (var area in wallCollisionAreas)
                    area.Serialize(writer, SerializationModeFlags.All);
            }
        }

        public override void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            base.Deserialize(reader, mode, framesAgo);
            if ((mode & SerializationModeFlags.ConstantData) != 0)
            {
                ModelName = wallModelName = new CanonicalString(reader.ReadInt32());
                int collisionAreaCount = reader.ReadByte();
                _collisionAreas = wallCollisionAreas = new CollisionArea[collisionAreaCount];
                for (int i = 0; i < collisionAreaCount; ++i)
                {
                    wallCollisionAreas[i] = new CollisionArea();
                    wallCollisionAreas[i].Deserialize(reader, SerializationModeFlags.All, framesAgo);
                }
                foreach (var area in wallCollisionAreas) area.Owner = this;
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
            if (limitationAttribute == typeof(TypeParameterAttribute))
            {
                // Make sure there's no null references.

                // 'wallModelName' is actually part of our runtime state,
                // but its value is passed onwards by 'ModelNames' even
                // if we were only a gob template. The real problem is
                // that we don't make a difference between gob templates
                // and actual gob instances (that have a proper runtime state).
                if (wallModelName == null)
                    wallModelName = (CanonicalString)"dummymodel";
            }
            if (limitationAttribute == typeof(RuntimeStateAttribute))
            {
                // Make sure there's no null references.
                if (wallModelName == null)
                    wallModelName = (CanonicalString)"dummymodel";
            }
        }

        #endregion IConsistencyCheckable Members
    }
}
