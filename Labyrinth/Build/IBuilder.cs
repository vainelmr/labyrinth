using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth.Build
{
    public interface IBuilder
    {
        Tile[,] Build();

        event EventHandler<StartEventArgs>? StartPositionFound;
    }
}
