using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using System.Threading;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        private class LabyrinthCrawler(Labyrinth labyrinth, int crawlerId) : ICrawler
        {
            private readonly Labyrinth _labyrinth = labyrinth;
            private readonly int _crawlerId = crawlerId;
            private readonly SemaphoreSlim _actionLock = new(1, 1);
            private Direction _direction = Direction.North;
            private static readonly TimeSpan ActionDelay = TimeSpan.FromMilliseconds(80);

            public int X => _labyrinth.GetCrawlerX(_crawlerId);
            public int Y => _labyrinth.GetCrawlerY(_crawlerId);
            public Direction Direction => _direction;

            public async Task<Type> GetFrontTileTypeAsync()
            {
                await _actionLock.WaitAsync();
                try
                {
                    await Task.Delay(ActionDelay);
                    return ProcessFacingTile((x, y, tile) => tile.GetType());
                }
                finally
                {
                    _actionLock.Release();
                }
            }

            public async Task<bool> IsFacingExitAsync()
            {
                await _actionLock.WaitAsync();
                try
                {
                    await Task.Delay(ActionDelay);
                    return ProcessFacingTile((x, y, tile) => tile is Outside);
                }
                finally
                {
                    _actionLock.Release();
                }
            }

            public Task<MoveResult> TryMoveAsync(Inventory inventory) =>
                _labyrinth.TryMoveAsync(_crawlerId, _direction, inventory);

            private T ProcessFacingTile<T>(Func<int, int, Tile, T> process)
            {
                int x = _labyrinth.GetCrawlerX(_crawlerId);
                int y = _labyrinth.GetCrawlerY(_crawlerId);
                int facingX = x + _direction.DeltaX;
                int facingY = y + _direction.DeltaY;
                var tiles = _labyrinth._tiles;

                bool IsOut(int pos, int dimension) => pos < 0 || pos >= tiles.GetLength(dimension);

                return process(
                    facingX, facingY,
                    IsOut(facingX, 0) || IsOut(facingY, 1)
                        ? Outside.Singleton
                        : tiles[facingX, facingY]
                );
            }
        }
    }
}