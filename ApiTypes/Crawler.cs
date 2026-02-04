using System.Text.Json.Serialization;

namespace ApiTypes;

public class Crawler
{
    public Guid Id { get; set; }

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("direction")]
    public Direction Direction { get; set; }

    [JsonPropertyName("walking")]
    public bool Walking { get; set; }

    [JsonPropertyName("facing-tile")]
    public TileType FacingTile { get; set; }

    [JsonPropertyName("bag")]
    public InventoryItem[] Bag { get; set; } = Array.Empty<InventoryItem>();

    [JsonPropertyName("items")]
    public InventoryItem[] Items { get; set; } = Array.Empty<InventoryItem>();
}
