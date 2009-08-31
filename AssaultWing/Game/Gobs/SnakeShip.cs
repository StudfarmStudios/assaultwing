using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A ship that wiggles as it thrusts forward.
    /// </summary>
    public class SnakeShip : Ship
    {
        const float MAX_TAIL_TURN = MathHelper.Pi * 0.3f;
        const float THRUST_NEEDED_FOR_TURN_SHIFT = 15;
        IEnumerable<int> tailIndices;
        float wiggleMainPhase;
        float wiggleFrequency; // wiggles per second
        float[] tailPhases; // for all model bones
        float[] tailAmplitudes; // for all model bones
        InterpolatingValue[] tailTurns; // for all model bones
        float thrustRemainderForTurnShift;
        bool advanceTailTurns;

        #region Properties

        #endregion Properties

        #region SnakeShip constructors

        /// <summary>
        /// Creates an uninitialised snake ship.
        /// </summary>
        /// This constructor is only for serialisation.
        public SnakeShip()
            : base()
        {
        }

        /// <summary>
        /// Creates a new snake ship.
        /// </summary>
        /// <param name="typeName">Type of the snake ship.</param>
        public SnakeShip(CanonicalString typeName)
            : base(typeName)
        {
        }

        #endregion SnakeShip constructors

        #region Methods related to gobs' functionality in the game world

        /// <summary>
        /// Activates the gob, i.e. performs an initialisation rite.
        /// </summary>
        public override void Activate()
        {
            base.Activate();
            var model = AssaultWing.Instance.Content.Load<Model>(ModelName);
            tailIndices = model.Bones
                .Where(bone => bone.Name != null && bone.Name.StartsWith("Tail"))
                .OrderBy(bone => bone.Name)
                .Select(bone => bone.Index);
            tailPhases = new float[model.Bones.Count];
            tailAmplitudes = new float[model.Bones.Count];
            tailTurns = new InterpolatingValue[model.Bones.Count];
            for (int i = 0; i < model.Bones.Count; ++i)
            {
                tailPhases[i] = -1;
                tailAmplitudes[i] = 0;
                tailTurns[i].Reset(Rotation);
                tailTurns[i].Step = MAX_TAIL_TURN / THRUST_NEEDED_FOR_TURN_SHIFT;
                tailTurns[i].AngularInterpolation = true;
            }
            float phase = 0;
            float amplitude = 0.3f;
            foreach (int i in tailIndices)
            {
                tailPhases[i] = phase;
                phase += phase == 0 ? MathHelper.PiOver2 : MathHelper.Pi / 3;
                tailAmplitudes[i] = amplitude;
            }
            wiggleFrequency = 3f;
            wiggleMainPhase = Owner.Id * 1.2345f; // to avoid different ships wiggling synchronously
        }

        /// <summary>
        /// Updates the ship's internal state.
        /// </summary>
        public override void Update()
        {
            base.Update();
            float elapsedTime = (float)AssaultWing.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            wiggleMainPhase = (wiggleMainPhase - elapsedTime * MathHelper.Pi * wiggleFrequency) % MathHelper.TwoPi;
            if (advanceTailTurns)
                foreach (int i in tailIndices)
                    tailTurns[i].Advance();
            advanceTailTurns = false;
            foreach (int i in tailIndices)
                tailAmplitudes[i] = MathHelper.Max(0, tailAmplitudes[i] - 0.01f);
        }

        #endregion Methods related to gobs' functionality in the game world

        #region Protected methods

        /// <summary>
        /// Called when the ship is thrusting.
        /// </summary>
        protected override void Thrusting(float thrustForce)
        {
            thrustRemainderForTurnShift += thrustForce;
            while (thrustRemainderForTurnShift > THRUST_NEEDED_FOR_TURN_SHIFT)
            {
                thrustRemainderForTurnShift -= THRUST_NEEDED_FOR_TURN_SHIFT;

                // Shift tail turn angles forward one step.
                float prevTailTurn = Rotation;
                foreach (int i in tailIndices)
                {
                    float thisTailTurn = tailTurns[i].Target;
                    tailTurns[i].Target = prevTailTurn;
                    prevTailTurn = thisTailTurn;
                }
            }
            advanceTailTurns = true;
            foreach (int i in tailIndices)
                tailAmplitudes[i] = MathHelper.Min(0.3f, tailAmplitudes[i] + 0.03f);
        }

        /// <summary>
        /// Called when the ship is turning.
        /// </summary>
        protected override void Turning(float turnAngle)
        {
            float prevTailTurn = Rotation;
            foreach (int i in tailIndices)
            {
                float turnDifference = MathHelper.WrapAngle(tailTurns[i].Target - prevTailTurn);
                if (Math.Abs(turnDifference) <= MAX_TAIL_TURN) break;
                prevTailTurn = tailTurns[i].Target = 
                    prevTailTurn + MathHelper.Clamp(turnDifference, -MAX_TAIL_TURN, MAX_TAIL_TURN);
            }
            advanceTailTurns = true;
        }

        /// <summary>
        /// Copies a transform of each bone in a model relative to all parent bones of the bone into a given array.
        /// </summary>
        protected override void CopyAbsoluteBoneTransformsTo(Model model, Matrix[] transforms)
        {
            if (transforms == null) throw new ArgumentNullException("Null transformation matrix array");
            if (transforms.Length < model.Bones.Count) throw new ArgumentException("Too short transformation matrix array");

            float[] tailTurnDeltas = new float[model.Bones.Count];
            float prevTailTurn = Rotation;
            foreach (int i in tailIndices)
            {
                tailTurnDeltas[i] = tailTurns[i].Current - prevTailTurn;
                prevTailTurn = tailTurns[i].Current;
            }

            foreach (var bone in model.Bones)
            {
                if (bone.Parent == null)
                    transforms[bone.Index] = bone.Transform;
                else
                {
                    if (bone.Parent.Index >= bone.Index) throw new Exception("Unexpected situation: bone parent doesn't precede the bone itself");
                    if (tailPhases[bone.Index] < 0)
                        transforms[bone.Index] = bone.Transform * transforms[bone.Parent.Index];
                    else
                    {
                        float wigglePhase = wiggleMainPhase + tailPhases[bone.Index];
                        float wiggleAngle = tailTurnDeltas[bone.Index] + tailAmplitudes[bone.Index] * (float)Math.Sin(wigglePhase);
                        var extraTransform = Matrix.CreateRotationZ(wiggleAngle);
                        transforms[bone.Index] = extraTransform * bone.Transform * transforms[bone.Parent.Index];
                    }
                }
            }
        }

        #endregion Protected methods
    }
}
