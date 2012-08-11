using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AW2.Graphics.Content;

namespace AW2.Game.Gobs
{
    /// <summary>
    /// A ship that wiggles as it thrusts forward.
    /// </summary>
    public class SnakeShip : Ship
    {
        private const float MAX_TAIL_TURN = MathHelper.Pi * 0.3f;
        private const float THRUST_NEEDED_FOR_TURN_SHIFT = 15;
        private const float MIN_TAIL_AMPLITUDE = 0.03f;
        private const float MAX_TAIL_AMPLITUDE = 0.3f;

        private IEnumerable<int> _tailIndices;
        private float _wiggleMainPhase;
        private float _wiggleFrequency; // wiggles per second
        private float[] _tailPhases; // for all model bones
        private float[] _tailAmplitudes; // for all model bones
        private InterpolatingValue[] _tailTurns; // for all model bones
        private float _thrustRemainderForTurnShift;
        private bool _advanceTailTurns;
        private float _oldDrawRotation;

        private bool TailStateInitialized { get { return _tailIndices != null; } }

        /// <summary>
        /// Only for serialisation.
        /// </summary>
        public SnakeShip()
        {
        }

        public SnakeShip(CanonicalString typeName)
            : base(typeName)
        {
            Thrusting = StraightenTail;
            IsModelBonesMoving = true;
        }

        public override void Activate()
        {
            InitializeTailState();
            base.Activate();
            _wiggleFrequency = 3f;
            _wiggleMainPhase = ID * 1.2345f; // to avoid different ships wiggling synchronously
        }

        public override void Update()
        {
            base.Update();
            if (Rotation + DrawRotationOffset != _oldDrawRotation)
            {
                _oldDrawRotation = Rotation + DrawRotationOffset;
                UpdateTailTurnTargets();
            }
            float elapsedTime = (float)Game.GameTime.ElapsedGameTime.TotalSeconds;
            _wiggleMainPhase = (_wiggleMainPhase - elapsedTime * MathHelper.Pi * _wiggleFrequency) % MathHelper.TwoPi;
            if (_advanceTailTurns)
                foreach (int i in _tailIndices)
                    _tailTurns[i].Advance();
            _advanceTailTurns = false;
            foreach (int i in _tailIndices)
                _tailAmplitudes[i] = MathHelper.Max(MIN_TAIL_AMPLITUDE, _tailAmplitudes[i] - 0.01f);
        }

        private void StraightenTail(float proportionalForce)
        {
            _thrustRemainderForTurnShift += proportionalForce;
            while (_thrustRemainderForTurnShift > THRUST_NEEDED_FOR_TURN_SHIFT)
            {
                _thrustRemainderForTurnShift -= THRUST_NEEDED_FOR_TURN_SHIFT;

                // Shift tail turn angles forward one step.
                float prevTailTurn = Rotation + DrawRotationOffset;
                foreach (int i in _tailIndices)
                {
                    float thisTailTurn = _tailTurns[i].Target;
                    _tailTurns[i].Target = prevTailTurn;
                    prevTailTurn = thisTailTurn;
                }
            }
            _advanceTailTurns = true;
            foreach (int i in _tailIndices)
                _tailAmplitudes[i] = MathHelper.Min(MAX_TAIL_AMPLITUDE, _tailAmplitudes[i] + 0.03f);
        }

        protected override void CopyAbsoluteBoneTransformsTo(ModelGeometry skeleton, Matrix[] transforms)
        {
            if (transforms == null) throw new ArgumentNullException("Null transformation matrix array");
            if (transforms.Length < skeleton.Bones.Length) throw new ArgumentException("Too short transformation matrix array");

            float[] tailTurnDeltas = new float[skeleton.Bones.Length];
            float prevTailTurn = Rotation + DrawRotationOffset;
            foreach (int i in _tailIndices)
            {
                tailTurnDeltas[i] = MathHelper.WrapAngle(_tailTurns[i].Current - prevTailTurn);
                prevTailTurn = _tailTurns[i].Current;
            }

            foreach (var bone in skeleton.Bones)
            {
                if (bone.Parent == null)
                    transforms[bone.Index] = bone.Transform;
                else
                {
                    if (bone.Parent.Index >= bone.Index) throw new Exception("Unexpected situation: bone parent doesn't precede the bone itself");
                    if (_tailPhases[bone.Index] < 0)
                        transforms[bone.Index] = bone.Transform * transforms[bone.Parent.Index];
                    else
                    {
                        float wigglePhase = _wiggleMainPhase + _tailPhases[bone.Index];
                        float turnDamping = 1 - Math.Abs(tailTurnDeltas[bone.Index]) / MAX_TAIL_TURN; // wiggle less when bent
                        float tailAmplitude = MIN_TAIL_AMPLITUDE + turnDamping * (_tailAmplitudes[bone.Index] - MIN_TAIL_AMPLITUDE);
                        float wiggleAngle = tailTurnDeltas[bone.Index] + tailAmplitude * (float)Math.Sin(wigglePhase);
                        var extraTransform = Matrix.CreateRotationZ(wiggleAngle);
                        transforms[bone.Index] = extraTransform * bone.Transform * transforms[bone.Parent.Index];
                    }
                }
            }
        }

        private void InitializeTailState()
        {
            var model = Game.Content.Load<ModelGeometry>(ModelName);
            _tailIndices = model.Bones
                .Where(bone => bone.Name != null && bone.Name.StartsWith("Tail"))
                .OrderBy(bone => bone.Name)
                .Select(bone => bone.Index);
            _tailPhases = new float[model.Bones.Length];
            _tailAmplitudes = new float[model.Bones.Length];
            _tailTurns = new InterpolatingValue[model.Bones.Length];
            for (int i = 0; i < model.Bones.Length; i++)
            {
                _tailPhases[i] = -1;
                _tailAmplitudes[i] = 0;
                _tailTurns[i].Reset(Rotation + DrawRotationOffset);
                _tailTurns[i].Step = 1.2f * MAX_TAIL_TURN / THRUST_NEEDED_FOR_TURN_SHIFT;
                _tailTurns[i].AngularInterpolation = true;
            }
            float phase = 0;
            float amplitude = MIN_TAIL_AMPLITUDE;
            foreach (int i in _tailIndices)
            {
                _tailPhases[i] = phase;
                phase += phase == 0 ? MathHelper.PiOver2 : MathHelper.Pi / 3;
                _tailAmplitudes[i] = amplitude;
            }
        }

        private void UpdateTailTurnTargets()
        {
            float prevTailTurn = Rotation + DrawRotationOffset;
            foreach (int i in _tailIndices)
            {
                float turnDifference = MathHelper.WrapAngle(_tailTurns[i].Target - prevTailTurn);
                if (Math.Abs(turnDifference) <= MAX_TAIL_TURN) break;
                prevTailTurn = _tailTurns[i].Target =
                    prevTailTurn + MathHelper.Clamp(turnDifference, -MAX_TAIL_TURN, MAX_TAIL_TURN);
            }
            _advanceTailTurns = true;
        }
    }
}
