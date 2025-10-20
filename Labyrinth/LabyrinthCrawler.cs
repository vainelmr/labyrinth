using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        private class LabyrinthCrawler(int x, int y, Tile[,] tiles) : ICrawler
        {
            public int X => x;

            public int Y => y;

            public Tile FacingTile =>
                IsOut(x + _direction.DeltaX, dimension: 0) ||
                IsOut(y + _direction.DeltaY, dimension: 1)
                    ? Outside.Singleton
                    : _tiles[ x + _direction.DeltaX, 
                              y + _direction.DeltaY ];

            Direction ICrawler.Direction => _direction;

            public Inventory Walk() => throw new NotImplementedException();

            private bool IsOut(int pos, int dimension) =>
                pos < 0 || pos >= _tiles.GetLength(dimension);

            private readonly Direction _direction = Direction.North;
            private readonly Tile[,] _tiles = tiles;
        }
    }
}