using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        private class LabyrinthCrawler(int x, int y, Tile[,] tiles) : ICrawler
        {
            public int X => _x;

            public int Y => _y;

            public Tile FacingTile =>
                IsOut(_x + _direction.DeltaX, dimension: 0) ||
                IsOut(_y + _direction.DeltaY, dimension: 1)
                    ? Outside.Singleton
                    : _tiles[ _x + _direction.DeltaX, 
                              _y + _direction.DeltaY ];

            Direction ICrawler.Direction => _direction;

            public Inventory Walk() => _tiles[
                _x += _direction.DeltaX,
                _y += _direction.DeltaY
            ].Pass();

            private bool IsOut(int pos, int dimension) =>
                pos < 0 || pos >= _tiles.GetLength(dimension);

            private int _x = x;
            private int _y = y;

            private readonly Direction _direction = Direction.North;
            private readonly Tile[,] _tiles = tiles;
        }
    }
}