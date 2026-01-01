using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth.Build
{
    public interface IBuilder
    {
        Tile[,] Build(LocalInventory? unplacedItems = null);

        event EventHandler<StartEventArgs>? StartPositionFound;
    }
}
