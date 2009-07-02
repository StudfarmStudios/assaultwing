using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AW2.Helpers;
using Microsoft.Xna.Framework;

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
        CanonicalString wallModelName;

        /// <summary>
        /// Target of the move.
        /// </summary>
        [RuntimeState]
        Vector2 targetPos;

        /// <summary>
        /// Moving curve. Acts as linear coefficient of position from the original
        /// positino towards the target position. Curve argument is game time in 
        /// seconds, measured from the beginning of the movement.
        /// </summary>
        [RuntimeState]
        Curve movingCurve;

        Vector2 startPos;
        TimeSpan startTime;

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
            targetPos = new Vector2(100, 200);
            movingCurve = new Curve();
            movingCurve.Keys.Add(new CurveKey(0, 0));
            movingCurve.Keys.Add(new CurveKey(3, 1));
            movingCurve.ComputeTangents(CurveTangent.Linear);
            movingCurve.PreLoop = CurveLoopType.Constant;
            movingCurve.PostLoop = CurveLoopType.Constant;
        }

        /// <summary>
        /// Creates a gob of the specified gob type.
        /// </summary>
        public MovingWall(string typeName)
            : base(typeName)
        {
            wallModelName = (CanonicalString)"dummymodel";
            startTime = new TimeSpan(-1);
        }

        /// <summary>
        /// Triggers an predefined action on the ActionGob.
        /// </summary>
        public override void Act()
        {
            startPos = pos;
            startTime = AssaultWing.Instance.GameTime.TotalGameTime;
        }

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            ModelName = wallModelName;
            base.Activate();
        }

        /// <summary>
        /// Updates the gob according to physical laws.
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (startTime.Ticks >= 0)
            {
                float seconds = (float)(AssaultWing.Instance.GameTime.TotalGameTime - startTime).TotalSeconds;
                pos = Vector2.Lerp(startPos, targetPos, movingCurve.Evaluate(seconds));
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
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                writer.Write((int)wallModelName.Canonical);
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
            if ((mode & AW2.Net.SerializationModeFlags.ConstantData) != 0)
            {
                ModelName = wallModelName = new CanonicalString(reader.ReadInt32());
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
