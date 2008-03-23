using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Game
{
    /// <summary>
    /// Choice of laws of physics that should apply.
    /// </summary>
    /// <see cref="AW2.Game.Gob.PhysicsApplyMode"/>
    [Flags]
    public enum PhysicsApplyMode
    {
        /// <summary>
        /// The gob moves with physics.
        /// </summary>
        Move = 0x0001,

        /// <summary>
        /// The gob is affected by gravity.
        /// </summary>
        Gravity = 0x0002,

        /// <summary>
        /// The gob goes along all physical laws.
        /// </summary>
        All = 0x0003,

        /// <summary>
        /// The gob doesn't follow physical laws.
        /// </summary>
        None = 0x0000,

        /// <summary>
        /// The gob wants a physical collision even when only its receptor overlaps.
        /// </summary>
        ReceptorCollidesPhysically = 0x0010,
    }
    
    /// <summary>
    /// Interface for a physics engine. The physics engine takes care of applying
    /// all forces and delta values in the right proportion relative to elapsed time
    /// on each frame. The physics engine also takes care of finding out collisions
    /// between gobs.
    /// </summary>
    public interface PhysicsEngine
    {
        /// <summary>
        /// Game timing information for the current frame.
        /// </summary>
        /// This is meant to be set by LogicEngine at the beginning of each frame
        /// and can be used all over game logic.
        GameTime TimeStep { get; set; }

        /// <summary>
        /// Resets the physics engine for a new arena.
        /// </summary>
        void Reset();

        /// <summary>
        /// Applies drag to the given gob.
        /// </summary>
        /// Drag is the force that resists movement in a medium.
        /// <param name="gob">The gob to apply drag to.</param>
        void ApplyDrag(Gob gob);
        
        /// <summary>
        /// Moves the given gob and takes care of collisions.
        /// </summary>
        /// <param name="gob">The gob to move.</param>
        void Move(Gob gob);
                
        /// <summary>
        /// Performs additional collision checks. Must be called every frame
        /// after all gob movement is done.
        /// </summary>
        void MovesDone();

        /// <summary>
        /// Registers a gob for collisions.
        /// </summary>
        /// <param name="gob">The gob.</param>
        void Register(Gob gob);

        /// <summary>
        /// Removes a previously registered gob from the register.
        /// </summary>
        /// <param name="gob">The gob.</param>
        void Unregister(Gob gob);

        /// <summary>
        /// Applies the given force to the given gob.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more force is needed to give it
        /// a good push.
        /// <param name="gob">The gob to apply the force to.</param>
        /// <param name="force">The force to apply, measured in Newtons.</param>
        void ApplyForce(Gob gob, Vector2 force);
        
        /// <summary>
        /// Applies the given force to a gob, preventing gob speed from
        /// growing beyond a limit.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more force is needed to give it
        /// a good push. Although the gob's speed cannot grow beyond <b>maxSpeed</b>,
        /// it can still maintain its value even if it's larger than <b>maxSpeed</b>.
        /// <param name="gob">The gob to apply the force to.</param>
        /// <param name="force">The force to apply, measured in Newtons.</param>
        /// <param name="maxSpeed">The speed limit beyond which the gob's speed cannot grow.</param>
        void ApplyLimitedForce(Gob gob, Vector2 force, float maxSpeed);

        /// <summary>
        /// Applies the given momentum to the given gob.
        /// </summary>
        /// Note that the larger the mass of the gob is, the more momentum is needed to give it
        /// a good push.
        /// <param name="gob">The gob to apply the momentum to.</param>
        /// <param name="momentum">The momentum to apply, measured in Newton seconds.</param>
        void ApplyMomentum(Gob gob, Vector2 momentum);

        /// <summary>
        /// Returns the scalar amount that represents how much the given scalar change speed
        /// affects during the current frame.
        /// </summary>
        /// <param name="changePerSecond">The speed of change per second.</param>
        /// <returns>The amount of change during the current frame.</returns>
        float ApplyChange(float changePerSecond);

        /// <summary>
        /// Returns a position, near a preferred position, in the game world 
        /// where a gob is overlap consistent (e.g. not inside a wall).
        /// </summary>
        /// <param name="gob">The gob to position.</param>
        /// <param name="preferred">Preferred position, or <b>null</b>.</param>
        /// <returns>A position for the gob where it is overlap consistent.</returns>
        Vector2 GetFreePosition(Gob gob, Vector2? preferred);
    }
}
