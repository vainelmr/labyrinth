using Labyrinth.Items;

namespace Labyrinth.Crawl
{
    public interface ICrawler
    {
        int X { get; }
        int Y { get; }
        Direction Direction { get; }

        Task<Type> GetFrontTileTypeAsync();
        Task<bool> IsFacingExitAsync();
        Task<MoveResult> TryMoveAsync(Inventory inventory);
    }
}