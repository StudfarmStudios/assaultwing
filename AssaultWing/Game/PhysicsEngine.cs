using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace AW2.Game
{
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
        /// Applies drag to a gob. Drag is a force that manipulates the gob's
        /// movement closer to that of the medium. Drag constant measures the
        /// amount of this manipulation, 0 meaning no drag and 1 meaning
        /// absolute drag where the gob cannot escape the flow of the medium.
        /// Practical values are very small, under 0.1.
        /// </summary>
        /// Drag is the force that resists movement in a medium.
        /// <param name="gob">The gob to apply drag to.</param>
        /// <param name="flow">Direction and speed of flow of medium
        /// at the gob's location.</param>
        /// <param name="drag">Drag constant for the medium and the gob.</param>
        void ApplyDrag(Gob gob, Vector2 flow, float drag);

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
        /// Removes a previously registered collision area from the register.
        /// </summary>
        /// <param name="area">The collision area.</param>
        void Unregister(CollisionArea area);

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

        /// <summary>
        /// Is a gob overlap consistent (e.g. not inside a wall) at a position. 
        /// </summary>
        /// <param name="gob">The gob.</param>
        /// <param name="position">The position.</param>
        /// <returns><b>true</b> iff the gob is overlap consistent at the position.</returns>
        bool IsFreePosition(Gob gob, Vector2 position);

        /// <summary>
        /// Removes a round area from walls of the current arena, i.e. makes a hole.
        /// </summary>
        /// <param name="holePos">Center of the hole, in world coordinates.</param>
        /// <param name="holeRadius">Radius of the hole, in meters.</param>
        void MakeHole(Vector2 holePos, float holeRadius);
    }
}
