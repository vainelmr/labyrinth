using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using Dto = ApiTypes;

namespace Labyrinth.ApiClient
{
    public static class DtoHelperExtension
    {
        public static Dto.Direction GetApiDirection(this Direction dir) =>
                (Dto.Direction)(dir.DeltaY + 1 + dir.DeltaX * (dir.DeltaX - 1));

        public static Dto.InventoryItem[] GetApiInventoryItems(this Inventory content) =>
            [.. content.ItemTypes.Select(type => new Dto.InventoryItem
            {
                Type = Dto.ItemType.Key
            })];

        public static Dto.TileType GetApiTileType(this Type tileType) =>
            _tileTypeMap[tileType];

        public static Tile NewTile(this Type tileType) =>
            tileType == typeof(Room   ) ? new Room()     :
            tileType == typeof(Wall   ) ? Wall.Singleton :
            tileType == typeof(Door   ) ? new Door()     :
            throw new ArgumentException("Unknown tile type");

        public static Direction GetCrawlerDirection(this Dto.Direction dtoDir) =>
            fromDtoDirection[(int)dtoDir];

        public static Type GetCrawlerFacingTileType(this Dto.TileType dtoTileType) =>
            fromDtoTileType[(int)dtoTileType];

        private static readonly Direction[] fromDtoDirection = [
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        ];

        private static readonly Type[] fromDtoTileType = [
            typeof(Outside),
            typeof(Room   ),
            typeof(Wall   ),
            typeof(Door   )
        ];

        private static readonly IDictionary<Type, Dto.TileType> _tileTypeMap = new Dictionary<Type, Dto.TileType> {
            { typeof(Outside), Dto.TileType.Outside },
            { typeof(Room   ), Dto.TileType.Room },
            { typeof(Wall   ), Dto.TileType.Wall },
            { typeof(Door   ), Dto.TileType.Door }
        };
    }
}
