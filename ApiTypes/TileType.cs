using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Labyrinth tile types.
    /// </summary>
    /// <remarks>
    /// This enumeration defines the different types of tiles that can be found in a labyrinth.
    /// </remarks>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TileType
    {
        /// <summary>
        /// Specifies that a value or element is located outside the labyrinth, this is your goal.
        /// </summary>
        Outside,
        /// <summary>
        /// Represents a tile that always be passed and can have collectable items.
        /// </summary>
        Room,
        /// <summary>
        /// Represents a wall, a not traversable tile.
        /// </summary>
        Wall,
        /// <summary>
        /// Represents a door that can be passed by a crawler if its inventory contains the right key.
        /// </summary>
        Door
    }
}
