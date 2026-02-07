using Labyrinth.ApiClient;
using Labyrinth.Crawl;
using Labyrinth.Exploration;
using Labyrinth.Items;
using Microsoft.Extensions.Logging;

namespace Client;

/// <summary>
/// Explorer class responsible for controlling a crawler, 
/// updating the shared map, and coordinating with other explorers to explore the labyrinth efficiently.
/// </summary>
public class Explorer
{
    private readonly int _id;
    private readonly ICrawler _crawler;
    private readonly Inventory _bag;
    private readonly SharedMap _map;
    private readonly Coordinator _coordinator;
    private readonly BFSPathfinder _pathfinder;
    private readonly ILogger _logger;
    private (int X, int Y)? _currentTarget;

    public Explorer(
        int id,
        ICrawler crawler,
        Inventory bag,
        SharedMap map,
        Coordinator coordinator,
        BFSPathfinder pathfinder,
        ILogger logger)
    {
        _id = id;
        _crawler = crawler;
        _bag = bag;
        _map = map;
        _coordinator = coordinator;
        _pathfinder = pathfinder;
        _logger = logger;
    }

    /// <summary>
    /// Executes a step of exploration.
    /// </summary>
    /// <returns>True if the explorer should continue exploring, false if there are no more frontiers.</returns>
    public async Task<bool> StepAsync()
    {
        var currentPos = (_crawler.X, _crawler.Y);

        var facingTileType = await _crawler.GetFrontTileTypeAsync();
        var facingPos = GetFacingPosition(currentPos, _crawler.Direction);
        
        _map.Update(facingPos.X, facingPos.Y, TileTypeToKnowledge(facingTileType));
        _map.Update(currentPos.X, currentPos.Y, TileKnowledge.Room);

        if (_currentTarget.HasValue && currentPos == _currentTarget.Value)
        {
            _coordinator.ReleaseFrontier(_currentTarget.Value);
            _currentTarget = null;
            _logger.LogInformation("Crawler {Id}: Reached target ({X},{Y})", _id, currentPos.X, currentPos.Y);
        }

        if (!_currentTarget.HasValue)
        {
            _currentTarget = _coordinator.AssignFrontier(_id, currentPos);
            if (!_currentTarget.HasValue)
            {
                _logger.LogInformation("Crawler {Id}: No more frontiers available", _id);
                return false; // No more frontiers, exploration finished
            }
        }

        var path = _pathfinder.FindPath(currentPos, _currentTarget.Value);
        if (path == null || path.Count == 0)
        {
            _logger.LogWarning("Crawler {Id}: No path to frontier ({X},{Y}), releasing", 
                _id, _currentTarget.Value.X, _currentTarget.Value.Y);
            _coordinator.ReleaseFrontier(_currentTarget.Value);
            _currentTarget = null;
            return true;
        }

        var nextPos = path[0];
        var targetDirection = GetDirectionTo(currentPos, nextPos);
        
        while (_crawler.Direction != targetDirection)
        {
            _crawler.Direction.TurnLeft();
            _logger.LogDebug("Crawler {Id}: Turned to {Direction}", _id, _crawler.Direction);
        }

        var moveResult = await _crawler.TryMoveAsync(_bag);
        if (moveResult is MoveResult.Success)
        {
            _logger.LogDebug("Crawler {Id}: Moved to ({X},{Y})", _id, _crawler.X, _crawler.Y);
        }
        else
        {
            _logger.LogWarning("Crawler {Id}: Failed to move to ({X},{Y}): {Result}", 
                _id, nextPos.X, nextPos.Y, moveResult);
            _map.Update(facingPos.X, facingPos.Y, TileKnowledge.Wall);
            
            if (_currentTarget.HasValue)
            {
                _coordinator.ReleaseFrontier(_currentTarget.Value);
                _currentTarget = null;
            }
        }

        return true;
    }

    private static (int X, int Y) GetFacingPosition((int X, int Y) pos, Direction direction)
    {
        return (pos.X + direction.DeltaX, pos.Y + direction.DeltaY);
    }

    private static TileKnowledge TileTypeToKnowledge(Type tileType)
    {
        if (tileType == typeof(Labyrinth.Tiles.Wall)) return TileKnowledge.Wall;
        if (tileType == typeof(Labyrinth.Tiles.Room)) return TileKnowledge.Room;
        if (tileType == typeof(Labyrinth.Tiles.Door)) return TileKnowledge.Door;
        if (tileType == typeof(Labyrinth.Tiles.Outside)) return TileKnowledge.Outside;
        return TileKnowledge.Unknown;
    }

    private static Direction GetDirectionTo((int X, int Y) from, (int X, int Y) to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        
        if (dx == 1) return Direction.East;
        if (dx == -1) return Direction.West;
        if (dy == 1) return Direction.South;
        return Direction.North;
    }
}
