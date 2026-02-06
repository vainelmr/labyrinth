using Labyrinth.ApiClient;
using Labyrinth.Exploration;
using Microsoft.Extensions.Logging;

namespace Client;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configuring logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<Program>();

        if (args.Length < 2)
        {
            logger.LogError("Usage: Client <serverUrl> <appKey>");
            return 1;
        }

        var serverUrl = args[0];
        var appKey = args[1];

        logger.LogInformation("Client starting with server URL: {ServerUrl} and AppKey: {AppKey}", serverUrl, appKey.Substring(0, 8) + "...");

        return await RunExploration(serverUrl, appKey, logger);
    }

    static async Task<int> RunExploration(string serverUrl, string appKey, ILogger logger)
    {
        try
        {
            var session = await ContestSession.Open(new Uri(serverUrl), Guid.Parse(appKey));
            var crawler = session.Crawlers.First();
            var bag = session.Bags.First();
            
            logger.LogInformation("Crawler created at ({X}, {Y})", crawler.X, crawler.Y);

            var map = new SharedMap();
            var pathfinder = new BFSPathfinder(map, logger);

            for (int step = 0; step < 100; step++)
            {
                // 1. Observe and update map
                var currentPos = (crawler.X, crawler.Y);
                var facingTileType = await crawler.GetFrontTileTypeAsync();
                var facingPos = GetFacingPosition(currentPos, crawler.Direction);
                
                map.Update(facingPos.X, facingPos.Y, TileTypeToKnowledge(facingTileType));
                map.Update(currentPos.X, currentPos.Y, TileKnowledge.Room);
                
                logger.LogInformation("Step {Step}: Position ({X},{Y}), Map: {Known} cells, {Frontiers} frontiers", 
                    step, currentPos.X, currentPos.Y, map.KnownCount, map.GetFrontiers().Count());

                // 2. Pathfinding towards the next frontier
                var frontiers = map.GetFrontiers().ToList();
                if (frontiers.Count == 0)
                {
                    logger.LogInformation("No more frontiers to explore. Ending exploration.");
                    break;
                }

                var targetFrontier = frontiers[0];
                var path = pathfinder.FindPath(currentPos, targetFrontier);

                if (path == null || path.Count == 0)
                {
                    logger.LogWarning("No path to frontier {Frontier}", targetFrontier);
                    break;
                }

                // 3. Move towards the next position in the path
                var nextPos = path[0];
                var targetDirection = GetDirectionTo(currentPos, nextPos);
                
                while (crawler.Direction != targetDirection)
                {
                    crawler.Direction.TurnLeft();
                }

                var moveResult = await crawler.TryMoveAsync(bag);
                if (moveResult is not Labyrinth.Crawl.MoveResult.Success)
                {
                    logger.LogWarning("Error moving to ({X},{Y}): {Result}", nextPos.X, nextPos.Y, moveResult);
                    map.Update(facingPos.X, facingPos.Y, TileKnowledge.Wall);
                }
            }

            logger.LogInformation("Exploration over. Final position: ({X},{Y}), Known cells: {Known}, Frontiers left: {Frontiers}", 
                crawler.X, crawler.Y, map.KnownCount, map.GetFrontiers().Count());

            await session.Close();
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during exploration");
            return 1;
        }
    }

    static (int X, int Y) GetFacingPosition((int X, int Y) pos, Labyrinth.Crawl.Direction direction)
    {
        return (pos.X + direction.DeltaX, pos.Y + direction.DeltaY);
    }

    static TileKnowledge TileTypeToKnowledge(Type tileType)
    {
        if (tileType == typeof(Labyrinth.Tiles.Wall)) return TileKnowledge.Wall;
        if (tileType == typeof(Labyrinth.Tiles.Room)) return TileKnowledge.Room;
        if (tileType == typeof(Labyrinth.Tiles.Door)) return TileKnowledge.Door;
        if (tileType == typeof(Labyrinth.Tiles.Outside)) return TileKnowledge.Outside;
        return TileKnowledge.Unknown;
    }

    static Labyrinth.Crawl.Direction GetDirectionTo((int X, int Y) from, (int X, int Y) to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        
        var direction = Labyrinth.Crawl.Direction.North;
        
        if (dx == 1) direction = Labyrinth.Crawl.Direction.East;
        else if (dx == -1) direction = Labyrinth.Crawl.Direction.West;
        else if (dy == 1) direction = Labyrinth.Crawl.Direction.South;
        
        return direction;
    }
}
