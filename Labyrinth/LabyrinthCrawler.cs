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

            public Tile FacingTile => _tiles[X, Y - 1];

            Direction ICrawler.Direction => Direction.North;

            public Inventory Walk() => throw new NotImplementedException();

            private readonly Tile[,] _tiles = tiles;
        }
    }
}