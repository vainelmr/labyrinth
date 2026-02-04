using Labyrinth.Items;

namespace Labyrinth.Crawl
{
    /// <summary>
    /// Labyrinth crawler interface.
    /// </summary>
    public interface ICrawler
    {
        /// <summary>
        /// Gets the current X position.
        /// </summary>
        int X { get; }
        /// <summary>
        /// Gets the current Y position.
        /// </summary>
        int Y { get; }
        /// <summary>
        /// Gets the current direction.
        /// </summary>
        Direction Direction { get; }
        /// <summary>
        /// Gets the tile in front of the crawler.
        /// </summary>
        Task<Type> GetFrontTileTypeAsync();
        /// <summary>
        /// Tells if the crawler is facing an exit or not
        /// </summary>
        Task<bool> IsFacingExitAsync();
        Task<MoveResult> TryMoveAsync(Inventory inventory);
    }
}
