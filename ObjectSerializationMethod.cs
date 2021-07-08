namespace Penguin.Debugging
{
    /// <summary>
    /// Determines how objects passed to the logwriter are serialized
    /// </summary>
    public enum ObjectSerializationMethod
    {
        /// <summary>
        /// Call ToString on objects to serialize
        /// </summary>
        ToString,

        /// <summary>
        /// Call ToString if overridden, otherwise call ObjectSerializationOverride
        /// </summary>
        Auto,

        /// <summary>
        /// Always call ObjectSerializationOverride
        /// </summary>
        Override
    }
}