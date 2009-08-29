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
        IEnumerable<int> tailIndices;
        float wiggleMainPhase;
        float wiggleFrequency; // wiggles per second
        float[] tailPhases; // for all model bones
        float[] tailAmplitudes; // for all model bones

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
            for (int i = 0; i < model.Bones.Count; ++i)
            {
                tailPhases[i] = -1;
                tailAmplitudes[i] = 0;
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
        }

        #endregion Methods related to gobs' functionality in the game world

        /// <summary>
        /// Copies a transform of each bone in a model relative to all parent bones of the bone into a given array.
        /// </summary>
        protected override void CopyAbsoluteBoneTransformsTo(Model model, Matrix[] transforms)
        {
            if (transforms == null) throw new ArgumentNullException("Null transformation matrix array");
            if (transforms.Length < model.Bones.Count) throw new ArgumentException("Too short transformation matrix array");
            foreach (var bone in model.Bones)
            {
                if (bone.Parent == null)
                {
                    transforms[bone.Index] = bone.Transform;
                }
                else
                {
                    if (bone.Parent.Index >= bone.Index) throw new Exception("Unexpected situation: bone parent doesn't precede the bone itself");
                    if (tailPhases[bone.Index] < 0)
                        transforms[bone.Index] = bone.Transform * transforms[bone.Parent.Index];
                    else
                    {
                        float wigglePhase = wiggleMainPhase + tailPhases[bone.Index];
                        float wiggleAngle = tailAmplitudes[bone.Index] * (float)Math.Sin(wigglePhase);
                        var extraTransform = Matrix.CreateRotationZ(wiggleAngle);
                        transforms[bone.Index] = extraTransform * bone.Transform * transforms[bone.Parent.Index];
                    }
                }
            }
        }
    }
}
