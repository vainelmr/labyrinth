using Labyrinth.Crawl;
using Labyrinth.Items;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        private class LabyrinthCrawler(Labyrinth labyrinth, int crawlerId) : ICrawler
        {
            public int X => _labyrinth.GetCrawlerX(_crawlerId);
            public int Y => _labyrinth.GetCrawlerY(_crawlerId);
            public Direction Direction => _direction;

            public Task<Type> GetFrontTileTypeAsync() =>
                _labyrinth.GetFrontTileTypeAsync(_crawlerId, _direction);

            public Task<bool> IsFacingExitAsync() =>
                _labyrinth.IsFacingExitAsync(_crawlerId, _direction);

            public Task<MoveResult> TryMoveAsync(Inventory inventory) =>
                _labyrinth.TryMoveAsync(_crawlerId, _direction, inventory);

            private readonly Labyrinth _labyrinth = labyrinth;
            private readonly int _crawlerId = crawlerId;
            private Direction _direction = Direction.North;
        }
    }
}