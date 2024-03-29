namespace AW2.Menu
{
    /// <summary>
    /// Type of menu component, one for each subclass of <see cref="MenuComponent"/>.
    /// </summary>
    // TODO !!! Remove this enum. Refer to components by they object references.
    public enum MenuComponentType
    {
        /// <summary>
        /// No menu component is active
        /// </summary>
        Dummy,

        /// <summary>
        /// The main menu component.
        /// </summary>
        Main,

        /// <summary>
        /// The equip menu component.
        /// </summary>
        Equip,

        /// <summary>
        /// The arena select menu component.
        /// </summary>
        Arena,
    }
}
