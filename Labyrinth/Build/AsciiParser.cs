using Labyrinth.Tiles;

namespace Labyrinth.Build
{
    public class AsciiParser
    {
        public static Tile[,] Parse(string ascii_map, ref (int X, int Y) start)
        {
            var lines = ascii_map.Split("\n,\r\n".Split(','), StringSplitOptions.None);
            var width = lines[0].Length;
            var tiles = new Tile[width, lines.Length];
            
            using var km = new Keymaster();

            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                if (lines[y].Length != width)
                {
                    throw new ArgumentException("Invalid map: all lines must have the same length.");
                }
                for (int x = 0; x < tiles.GetLength(0); x++)
                {
                    tiles[x, y] = lines[y][x] switch
                    {
                        'x' => NewStartPos(x, y, out start),
                        ' ' => new Room(),
                        '+' or '-' or '|' => Wall.Singleton,
                        '/' => km.NewDoor(),
                        'k' => km.NewKeyRoom(),
                        _ => throw new ArgumentException($"Invalid map: unknown character '{lines[y][x]}' at line {y}, col {x}.")
                    };
                }
            }
            return tiles;
        }
        private static Room NewStartPos(int x, int y, out (int X, int Y) start)
        {
            start = (x, y);
            return new Room();
        }
    }
}
