using ApiTypes;

namespace TrainingServer.Models;

/// <summary>
/// Represents the state of a crawler in the training server.
/// </summary>
public class CrawlerState
{
    public Guid Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Dir { get; set; }
    public bool Walking { get; set; }
    public List<InventoryItem> Bag { get; set; } = new();

    /// <summary>
    /// Converts the state to API Crawler format.
    /// </summary>
    public Crawler ToCrawler(TileType facingTile, InventoryItem[]? items)
    {
        return new Crawler
        {
            Id = Id,
            X = X,
            Y = Y,
            Dir = Dir,
            Walking = Walking,
            FacingTile = facingTile,
            Bag = Bag.ToArray(),
            Items = items
        };
    }
}
