using Labyrinth.Items;
using Labyrinth.Tiles;
using Labyrinth;
using Labyrinth.Sys;


namespace Labyrinth.Exploration;

public static class MultiAgentRunner
{
    public static async Task<SharedMap> RunTwoAsync(Labyrinth lab, int steps)

    {
        var map = new SharedMap();

        var c1 = lab.NewCrawler();
        var c2 = lab.NewCrawler();

        //var e1 = new RandExplorer(c1, new Labyrinth.Sys.BasicEnumRandomizer<RandExplorer.Actions>());
        //var e2 = new RandExplorer(c2, new Labyrinth.Sys.BasicEnumRandomizer<RandExplorer.Actions>());

        var e1 = new RandExplorer(c1, new BasicEnumRandomizer<RandExplorer.Actions>());
        var e2 = new RandExplorer(c2, new BasicEnumRandomizer<RandExplorer.Actions>());


        e1.PositionChanged += (_, __) => UpdateFacing(map, e1);
        e1.DirectionChanged += (_, __) => UpdateFacing(map, e1);

        e2.PositionChanged += (_, __) => UpdateFacing(map, e2);
        e2.DirectionChanged += (_, __) => UpdateFacing(map, e2);

        await Task.WhenAll(
            e1.GetOut(steps, new MyInventory()),
            e2.GetOut(steps, new MyInventory())
        );

        return map;
    }

    private static void UpdateFacing(SharedMap map, RandExplorer exp)
    {
        var t = exp.LastFacingTileType;
        if (t is null) return;

        var k =
            t == typeof(Outside) ? TileKnowledge.Outside :
            t == typeof(Wall) ? TileKnowledge.Wall :
            t == typeof(Door) ? TileKnowledge.Door :
            TileKnowledge.Room;

        var c = exp.Crawler;
        map.Update(c.X + c.Direction.DeltaX, c.Y + c.Direction.DeltaY, k);
    }
}
