namespace AW2.Game
{
    /// <summary>
    /// Type of material of a collision area.
    /// </summary>
    /// The collision material determines the behaviour of a physical collision
    /// area in a physical collision. A material consists of elasticity, friction
    /// and damage factor.
    public enum CollisionMaterialType
    {
        /// <summary>
        /// Quite elastic, with moderate friction, normal damage
        /// </summary>
        Regular,

        /// <summary>
        /// Rather inelastic, with strong friction, normal damage
        /// </summary>
        Rough,

        /// <summary>
        /// Excessively elastic, with moderate friction, normal damage
        /// </summary>
        Bouncy,

        /// <summary>
        /// Very inelastic, with high friction, no damage
        /// </summary>
        Sticky,
    }
}
