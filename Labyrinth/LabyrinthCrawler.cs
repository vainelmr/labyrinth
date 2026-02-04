using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using System.Threading;




namespace Labyrinth
{
    public partial class Labyrinth
    {
        private class LabyrinthCrawler(int x, int y, Tile[,] tiles) : ICrawler
        {
            public int X => _x;

            public int Y => _y;

            //public Task<Type> FacingTileType => Task.FromResult(ProcessFacingTile((x, y, tile) => tile.GetType()));
            // 
           
            public Task<Type> FacingTileType => GetFacingTileTypeAsync();
            private async Task<Type> GetFacingTileTypeAsync()
            {
                await _actionLock.WaitAsync();
                try
                {
                    // Simulate action delay
                    await Task.Delay(ActionDelay);
                    return ProcessFacingTile((x, y, tile) => tile.GetType());
                }
                finally
                {
                    _actionLock.Release();
                }
            }




           public Direction Direction => _direction;


            public async Task<Inventory?> TryWalk(Inventory walkerInventory)
            {
                await _actionLock.WaitAsync();
                try
                {
                    await Task.Delay(ActionDelay);
                    return ProcessFacingTile((facingX, facingY, tile) =>
                    {
                        Inventory? tileContent = null;
                        if (tile is Door door)
                        {
                            Open(door, walkerInventory);
                        }
                        if (tile.IsTraversable)
                        {
                            tileContent = tile.Pass();
                            _x = facingX;
                             _y = facingY;
                        }
                        return tileContent;
                    });
                }
                finally
                {
                    _actionLock.Release();
                }
            }
            
            private bool Open(Door door, Inventory walkerInventory)
            {
                if (walkerInventory is not LocalInventory keyRing)
                {
                    throw new NotSupportedException("Local inventories only");
                }
                for(var maxKeys = walkerInventory.ItemTypes.Count(); maxKeys > 0; maxKeys--)
                {
                    if (door.Open(keyRing))
                    {
                        return true;
                    }
                }
                return false;
            }

            private bool IsOut(int pos, int dimension) =>
                pos < 0 || pos >= _tiles.GetLength(dimension);

            private T ProcessFacingTile<T>(Func<int, int, Tile, T> process)
            {
                int facingX = _x + _direction.DeltaX,
                    facingY = _y + _direction.DeltaY;

                return process(
                    facingX, facingY,
                    IsOut(facingX, dimension: 0) ||
                    IsOut(facingY, dimension: 1)
                        ? Outside.Singleton
                        : _tiles[facingX, facingY]
                 );
            }

            private int _x = x;
            private int _y = y;

            
            private Direction _direction = Direction.North;
            //public Direction Direction => _direction; // ou explicite si ton ICrawler impose explicite

            private readonly SemaphoreSlim _actionLock = new(1, 1);
            private readonly Tile[,] _tiles = tiles;
            private static readonly TimeSpan ActionDelay = TimeSpan.FromMilliseconds(80);

        }
    }
}