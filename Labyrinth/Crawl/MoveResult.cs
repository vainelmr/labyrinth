using Labyrinth.Items;
namespace Labyrinth.Crawl
{
    public abstract record MoveResult{
        public sealed record Success(Inventory? TileInventory) : MoveResult;
        public sealed record Failure : MoveResult;
    }
}