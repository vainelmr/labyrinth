using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using System.Text;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        /// <summary>
        /// Labyrinth with walls, doors and collectable items.
        /// </summary>
        /// <param name="builder">Builder used to create the labyrinth.</param>
        /// <exception cref="ArgumentException">Thrown when string argument reveals inconsistent map sizes or characters with no starting position.</exception>
        /// <exception cref="NotSupportedException">Thrown for multiple doors (resp. key locations) before key locations (resp. doors).</exception>
        public Labyrinth(IBuilder builder)
        {
            builder.StartPositionFound+= (s, e) => _start = (e.X, e.Y);
            _tiles = builder.Build();
            if (_tiles.GetLength(0) < 3 || _tiles.GetLength(1) < 3)
            {
                throw new ArgumentException("Labyrinth must be at least 3x3");
            }
            if (_start == (-1, -1))
            {
                throw new ArgumentException("Labyrinth must have at least one starting position marked with x");
            }
        }

        /// <summary>
        /// Labyrinth width (number of columns).
        /// </summary>
        public int Width { get; private init; }

        /// <summary>
        /// Labyrinth height (number of rows).
        /// </summary>
        public int Height { get; private init; }

        /// <summary>
        /// An ascii representation of the labyrinth.
        /// </summary>
        /// <returns>Formatted string</returns>
        public override string ToString()
        {
            var res = new StringBuilder();

            for (int y = 0; y < _tiles.GetLength(1); y++)
            {
                for (int x = 0; x < _tiles.GetLength(0); x++)
                {
                    res.Append(_tiles[x, y] switch
                    {
                        Unknown => '?',
                        Room => ' ',
                        Wall => '#',
                        Door => '/',
                        _ => throw new NotSupportedException("Unknown tile type")
                    });
                }
                res.AppendLine();
            }
            return res.ToString();
        }


        public ICrawler NewCrawler()
        {
            var id = _crawlerNextId++;
            _crawlerPositions[id] = (_start.X, _start.Y);
            return new LabyrinthCrawler(this, id);
        }

        /// <summary>
        /// Serveur : retourne uniquement le TYPE de la case devant le crawler.
        /// </summary>
        internal Task<Type> GetFrontTileTypeAsync(int crawlerId, Direction direction)
        {
            var (x, y) = _crawlerPositions[crawlerId];
            var tile = GetFacingTile(x, y, direction);
            return Task.FromResult(tile.GetType());
        }

        /// <summary>
        /// Serveur : indique si la case devant le crawler est la sortie (outside).
        /// </summary>
        internal Task<bool> IsFacingExitAsync(int crawlerId, Direction direction)
        {
            var (x, y) = _crawlerPositions[crawlerId];
            var tile = GetFacingTile(x, y, direction);
            return Task.FromResult(tile is Outside);
        }

        /// <summary>
        /// Serveur : tente de déplacer le crawler. Valide à partir de l'inventaire fourni (ex. clé pour porte).
        /// </summary>
        internal async Task<MoveResult> TryMoveAsync(int crawlerId, Direction direction, Inventory inventory)
        {
            var (x, y) = _crawlerPositions[crawlerId];
            int facingX = x + direction.DeltaX, facingY = y + direction.DeltaY;
            var tile = GetFacingTile(x, y, direction);

            if (tile is Wall or Outside)
                return new MoveResult.Failure();

            if (tile is Door door)
            {
                if (inventory is not LocalInventory keyRing)
                    return new MoveResult.Failure();
                var keyCount = (await inventory.GetItemTypesAsync()).Count;
                var opened = false;
                for (var k = keyCount; k > 0 && !opened; k--)
                    opened = door.Open(keyRing);
                if (!opened)
                    return new MoveResult.Failure();
            }

            if (tile.IsTraversable)
            {
                _crawlerPositions[crawlerId] = (facingX, facingY);
                return new MoveResult.Success(tile.Pass());
            }
            return new MoveResult.Failure();
        }

        internal int GetCrawlerX(int crawlerId) => _crawlerPositions[crawlerId].X;
        internal int GetCrawlerY(int crawlerId) => _crawlerPositions[crawlerId].Y;

        private Tile GetFacingTile(int x, int y, Direction direction)
        {
            int fx = x + direction.DeltaX, fy = y + direction.DeltaY;
            if (fx < 0 || fx >= _tiles.GetLength(0) || fy < 0 || fy >= _tiles.GetLength(1))
                return Outside.Singleton;
            return _tiles[fx, fy];
        }

        private (int X, int Y) _start = (-1, -1);
        private readonly Tile[,] _tiles;
        private readonly Dictionary<int, (int X, int Y)> _crawlerPositions = new();
        private int _crawlerNextId;
    }
}
