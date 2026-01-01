namespace Labyrinth.Tiles
{
    public class Unknown : Tile
    {
        public override bool IsTraversable => throw new InvalidOperationException(
            "No crawler should return this tile"
        );
    }
}
