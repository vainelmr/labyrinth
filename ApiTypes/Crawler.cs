using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Crawler information, including its position, direction, movement status, and items available.
    /// </summary>
    public class Crawler
    {
        /// <summary>The unique identifier of the crawler.</summary>
        public Guid Id { get; set; }

        /// <summary>The horizontal position of the crawler on the map.</summary>
        [JsonPropertyName("x")]
        public int X { get; set; }

        /// <summary>The vertical position of the crawler on the map.</summary>
        [JsonPropertyName("y")]
        public int Y { get; set; }

        /// <summary>The direction the crawler is currently facing.</summary>
        [JsonPropertyName("direction")]
        public Direction Dir { get; set; }

        /// <summary>A value indicating whether the crawler is currently walking. Set to <see langword="true"/> if walking; otherwise, <see langword="false"/>.</summary>
        public bool Walking { get; set; }

        /// <summary>The type of tile the crawler is currently facing.</summary>
        [JsonPropertyName("facing-tile")]
        public TileType FacingTile { get; set; }

        /// <summary>An optional list of items currently held in the crawler's bag. Can be empty but not null.</summary>
        public InventoryItem[]? Bag { get; set; }

        /// <summary>An optional list of items present at the crawler's current location. Can be empty but not null.</summary>
        public InventoryItem[]? Items { get; set; }
    }


}
