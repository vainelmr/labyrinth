using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Specifies the four cardinal directions: North, East, South, and West.
    /// </summary>
    /// <remarks>This enumeration is commonly used to represent orientation or movement of the crawler. 
    /// When serialized to JSON, the values are represented as their string names (e.g., "North").</remarks>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Direction
    {
        /// <summary>
        /// Represents the north direction.
        /// </summary>
        North,
        /// <summary>
        /// Specifies the east direction.
        /// </summary>
        East,
        /// <summary>
        /// Represents the south direction.
        /// </summary>
        South,
        /// <summary>
        /// Represents the west direction.
        /// </summary>
        West
    }
}
