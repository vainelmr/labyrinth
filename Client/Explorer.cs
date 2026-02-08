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
    private HashSet<(int X, int Y)> _impassableDoorsForMe = new();
    private HashSet<(int X, int Y)> _unreachableRoomsForMe = new();
    private int _stepCount = 0;

    public (int X, int Y)? Target => _currentTarget;

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
    /// <returns>True if the explorer should continue exploring, false if exploration is finished or exit reached.</returns>
    public async Task<bool> StepAsync()
    {
        _stepCount++;
        
        var currentPos = (_crawler.X, _crawler.Y);

        if (_crawler is ClientCrawler clientCrawler)
        {
            try 
            {
                var roomInventory = clientCrawler.CurrentTileInventory;
                var roomItems = await roomInventory.GetItemTypesAsync();
                
                if (roomItems.Count > 0)
                {                    
                    var allTrue = roomItems.Select(_ => true).ToList();
                    var collected = await _bag.TryMoveItemsFrom(roomInventory, allTrue);
                    
                    if (collected)
                    {
                        var newBagItems = await _bag.GetItemTypesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Crawler {Id}: Failed to inspect/collect items on current tile", _id);
            }
        }


        if (_map.Get(currentPos.X, currentPos.Y) == TileKnowledge.Unknown)
        {
            _map.Update(currentPos.X, currentPos.Y, TileKnowledge.Room);
        }

        var bagItemsTypes = await _bag.GetItemTypesAsync();
        bool myHasKey = bagItemsTypes.Contains(typeof(Labyrinth.Items.Key));
        int myBagCount = bagItemsTypes.Count;

        if (_stepCount % 25 == 0)
        {
            await RecheckKnownDoorsAsync(currentPos);
        }

        if (myBagCount > 0 && _impassableDoorsForMe.Count > 0)
        {
            _impassableDoorsForMe.Clear();
            _unreachableRoomsForMe.Clear();
            _map.InvalidatePathCache();
        }

        var facingPos = GetFacingPosition(currentPos, _crawler.Direction);
        if (_map.Get(facingPos.X, facingPos.Y) == TileKnowledge.Unknown)
        {
            var isFacingExit = await _crawler.IsFacingExitAsync();
            
            if (isFacingExit)
            {
                _map.MarkExit(facingPos.X, facingPos.Y);
                _map.Update(facingPos.X, facingPos.Y, TileKnowledge.Outside);
                            }
            else
            {
                var facingTileType = await _crawler.GetFrontTileTypeAsync();
                _map.Update(facingPos.X, facingPos.Y, TileTypeToKnowledge(facingTileType));
            }
        }

        if (_currentTarget.HasValue && currentPos == _currentTarget.Value)
        {
            _coordinator.ReleaseFrontier(_currentTarget.Value);
            _currentTarget = null;
        }

        // Dès que la sortie est trouvée, tous les crawlers la priorisent (avant toute frontière)
        if (_map.ExitFound && _map.ExitPosition.HasValue && _currentTarget != _map.ExitPosition)
        {
            var pathToExit = _pathfinder.FindPath(currentPos, _map.ExitPosition.Value, myHasKey, _impassableDoorsForMe);
            if (pathToExit != null && pathToExit.Count > 0)
            {
                if (_currentTarget.HasValue && _map.IsFrontier(_currentTarget.Value.X, _currentTarget.Value.Y))
                    _coordinator.ReleaseFrontier(_currentTarget.Value);
                _currentTarget = _map.ExitPosition;
            }
        }

        if (!_currentTarget.HasValue)
        {
            var ignoreList = new List<(int X, int Y)>();
            while (true)
            {
                _currentTarget = _coordinator.AssignFrontier(_id, currentPos, ignoreList);
                if (!_currentTarget.HasValue)
                {
                    // 1) Sortie atteignable : la prioriser
                    if (_map.ExitFound && _map.ExitPosition.HasValue)
                    {
                        var pathToExit = _pathfinder.FindPath(currentPos, _map.ExitPosition.Value, myHasKey, _impassableDoorsForMe);
                        if (pathToExit != null && pathToExit.Count > 0)
                        {
                            _currentTarget = _map.ExitPosition;
                            break;
                        }
                    }

                    var knownRooms = _map.GetAllKnown()
                        .Where(pos => _map.Get(pos.X, pos.Y) == TileKnowledge.Room)
                        .Where(pos => pos != currentPos)
                        .Where(pos => !ignoreList.Contains(pos))
                        .Where(pos => !_unreachableRoomsForMe.Contains(pos))
                        .ToList();

                    if (knownRooms.Count > 0)
                    {
                        (int X, int Y)? bestRoom = null;
                        int bestDistance = int.MaxValue;

                        foreach (var room in knownRooms)
                        {
                            var testPath = _pathfinder.FindPath(currentPos, room, myHasKey, _impassableDoorsForMe);
                            if (testPath != null && testPath.Count > 0)
                            {
                                int distance = Math.Abs(room.X - currentPos.X) + Math.Abs(room.Y - currentPos.Y);
                                if (distance < bestDistance)
                                {
                                    bestDistance = distance;
                                    bestRoom = room;
                                }
                            }
                            else
                            {
                                ignoreList.Add(room);
                            }
                        }

                        if (bestRoom.HasValue)
                        {
                            _currentTarget = bestRoom.Value;
                            break;
                        }
                    }

                    // 2) Pas de salles atteignables : retenter la sortie, sinon cibler une porte (avec clé) pour ouvrir et découvrir
                    if (_map.ExitFound && _map.ExitPosition.HasValue)
                    {
                        var pathToExit = _pathfinder.FindPath(currentPos, _map.ExitPosition.Value, myHasKey, _impassableDoorsForMe);
                        if (pathToExit != null && pathToExit.Count > 0)
                        {
                            _currentTarget = _map.ExitPosition;
                            break;
                        }
                    }

                    if (myHasKey)
                    {
                        var knownDoors = _map.GetAllKnown()
                            .Where(pos => _map.Get(pos.X, pos.Y) == TileKnowledge.Door)
                            .ToList();
                        (int X, int Y)? nearestDoor = null;
                        int bestDoorDist = int.MaxValue;
                        foreach (var door in knownDoors)
                        {
                            var pathToDoor = _pathfinder.FindPath(currentPos, door, true, _impassableDoorsForMe);
                            if (pathToDoor != null && pathToDoor.Count > 0)
                            {
                                int d = Math.Abs(door.X - currentPos.X) + Math.Abs(door.Y - currentPos.Y);
                                if (d < bestDoorDist)
                                {
                                    bestDoorDist = d;
                                    nearestDoor = door;
                                }
                            }
                        }
                        if (nearestDoor.HasValue)
                        {
                            _currentTarget = nearestDoor.Value;
                            break;
                        }
                    }

                    return false;
                }

                if (!_map.IsFrontier(_currentTarget.Value.X, _currentTarget.Value.Y) &&
                    _currentTarget != _map.ExitPosition &&
                    _map.Get(_currentTarget.Value.X, _currentTarget.Value.Y) != TileKnowledge.Door)
                {
                    _coordinator.ReleaseFrontier(_currentTarget.Value);
                    _currentTarget = null;
                    continue;
                }

                var checkPath = _pathfinder.FindPath(currentPos, _currentTarget.Value, myHasKey, _impassableDoorsForMe);
                if (checkPath != null && checkPath.Count > 0)
                {
                    break;
                }

                _coordinator.ReleaseFrontier(_currentTarget.Value);
                ignoreList.Add(_currentTarget.Value);
                _currentTarget = null;
            }
        }

        if (_currentTarget.HasValue)
        {
            if (!_map.IsFrontier(_currentTarget.Value.X, _currentTarget.Value.Y) &&
                _currentTarget != _map.ExitPosition &&
                _map.Get(_currentTarget.Value.X, _currentTarget.Value.Y) != TileKnowledge.Door)
            {
                _coordinator.ReleaseFrontier(_currentTarget.Value);
                _currentTarget = null;
                return true;
            }
        }

        var path = _pathfinder.FindPath(currentPos, _currentTarget.Value, myHasKey, _impassableDoorsForMe);
        if (path == null || path.Count == 0)
        {
            if (_map.Get(_currentTarget.Value.X, _currentTarget.Value.Y) == TileKnowledge.Room)
            {
                _unreachableRoomsForMe.Add(_currentTarget.Value);
            }
                        
            if (!_map.ExitFound || _currentTarget != _map.ExitPosition)
            {
                _coordinator.ReleaseFrontier(_currentTarget.Value);
            }
            _currentTarget = null;
            
            return true;
        }

        var nextPos = path[0];
        var targetDirection = GetDirectionTo(currentPos, nextPos);
        
        while (_crawler.Direction != targetDirection)
        {
            _crawler.Direction.TurnLeft();
        }
        
        var moveResult = await _crawler.TryMoveAsync(_bag);
        
        bool actuallyMoved = _crawler.X != currentPos.X || _crawler.Y != currentPos.Y;

        if (moveResult is MoveResult.Success success && actuallyMoved)
        {
            var newPos = (_crawler.X, _crawler.Y);
            if (_currentTarget.HasValue && _map.Get(_currentTarget.Value.X, _currentTarget.Value.Y) == TileKnowledge.Door && newPos != _currentTarget.Value)
            {
                _map.Update(_currentTarget.Value.X, _currentTarget.Value.Y, TileKnowledge.Room);
                _map.InvalidatePathCache();
                _currentTarget = null;
            }
            if (_map.ExitFound && _map.ExitPosition.HasValue && newPos == _map.ExitPosition.Value &&
                _map.GetFrontiers().Count() == 0)
            {                
                for (int i = 0; i < 4; i++)
                {
                    if (await _crawler.IsFacingExitAsync())
                    {
                        var exitResult = await _crawler.TryMoveAsync(_bag);
                        if (exitResult is MoveResult.Success)
                        {
                            return false;
                        }
                        else
                        {
                            break;
                        }
                    }
                    _crawler.Direction.TurnLeft();
                }
            }
            
            if (success.TileInventory is not null)
            {
                 var itemTypes = await success.TileInventory.GetItemTypesAsync();
                if (itemTypes.Count > 0)
                {                    
                    var allTrue = itemTypes.Select(_ => true).ToList();
                    var collected = await _bag.TryMoveItemsFrom(success.TileInventory, allTrue);
                    
                    if (collected)
                    {
                        var bagItems = await _bag.GetItemTypesAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Crawler {Id}: Failed to collect items", _id);
                    }
                }
            }
        }
        else
        {            
            var obstacleTile = await _crawler.GetFrontTileTypeAsync();  
            _map.Update(nextPos.X, nextPos.Y, TileTypeToKnowledge(obstacleTile));
            
            if (obstacleTile.Name == "Door")
            {
                _impassableDoorsForMe.Add(nextPos);
                _map.InvalidatePathCache();
            }

            if (_currentTarget.HasValue && (!_map.ExitFound || _currentTarget != _map.ExitPosition))
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

    /// <summary>
    /// Re-check known doors to see if they've been opened (become rooms)
    /// </summary>
    private async Task RecheckKnownDoorsAsync((int X, int Y) currentPos)
    {
        var knownDoors = _map.GetAllKnown()
            .Where(pos => _map.Get(pos.X, pos.Y) == TileKnowledge.Door)
            .Where(pos => Math.Abs(pos.X - currentPos.X) + Math.Abs(pos.Y - currentPos.Y) == 1)
            .ToList();

        foreach (var doorPos in knownDoors)
        {
            var originalDir = _crawler.Direction;
                        var targetDir = GetDirectionTo(currentPos, doorPos);
            while (_crawler.Direction != targetDir)
            {
                _crawler.Direction.TurnLeft();
            }
            
            var tileType = await _crawler.GetFrontTileTypeAsync();
            if (tileType.Name == "Room")
            {
                _map.Update(doorPos.X, doorPos.Y, TileKnowledge.Room);
                _map.InvalidatePathCache();
            }
            
            while (_crawler.Direction != originalDir)
            {
                _crawler.Direction.TurnLeft();
            }
        }
    }
}
