using System.Collections.Concurrent;

namespace Labyrinth.Exploration;

/// <summary>
/// Represents the knowledge state of a tile in the labyrinth.
/// </summary>
public enum TileKnowledge { Unknown, Wall, Room, Door, Outside }


/// <summary>
/// Thread-safe shared map for multi-agent exploration.
/// Stores known tiles and frontier cells
/// </summary>
public sealed class SharedMap
{
    private readonly ConcurrentDictionary<(int X, int Y), TileKnowledge> _tiles = new();
    private readonly ConcurrentDictionary<(int X, int Y), bool> _frontiers = new();
    private readonly object _updateLock = new();

    /// <summary>
    /// Gets the knowledge state of a tile at the specified position.
    /// </summary>
    /// <returns>TileKnowledge.Unknown if the position hasn't been explored yet.</returns>
    public TileKnowledge Get(int x, int y)
        => _tiles.TryGetValue((x, y), out var tile) ? tile : TileKnowledge.Unknown;

    /// <summary>
    /// Updates the knowledge state of a tile and recalculates frontiers.
    /// Only updates if the tile was previously Unknown.
    /// </summary>
    public void Update(int x, int y, TileKnowledge value)
    {
        lock (_updateLock)
        {
            var updated = _tiles.AddOrUpdate(
                (x, y), 
                value, 
                (_, old) => old == TileKnowledge.Unknown ? value : old
            );

            if (updated != TileKnowledge.Unknown)
            {
                _frontiers.TryRemove((x, y), out _);
            }

            if (value == TileKnowledge.Room || value == TileKnowledge.Door)
            {
                AddNeighborsToFrontier(x, y);
            }
        }
    }

    /// <summary>
    /// Gets the total number of known tiles
    /// </summary>
    public int KnownCount => _tiles.Count;

    /// <summary>
    /// Gets all frontier cells
    /// </summary>
    public IReadOnlyCollection<(int X, int Y)> GetFrontiers()
    {
        return _frontiers.Keys.ToArray();
    }

    /// <summary>
    /// Gets all known positions and their tile types.
    /// </summary>
    public IReadOnlyDictionary<(int X, int Y), TileKnowledge> GetKnownTiles()
    {
        return _tiles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Checks if a position has been explored.
    /// </summary>
    public bool IsKnown(int x, int y)
    {
        return _tiles.ContainsKey((x, y));
    }

    /// <summary>
    /// Checks if a position is a frontier cell.
    /// </summary>
    public bool IsFrontier(int x, int y)
    {
        return _frontiers.ContainsKey((x, y));
    }

    /// <summary>
    /// Adds unknown neighbors of the given position to the frontier set.
    /// </summary>
    private void AddNeighborsToFrontier(int x, int y)
    {
        var neighbors = new[]
        {
            (x, y - 1), // North
            (x + 1, y), // East
            (x, y + 1), // South
            (x - 1, y)  // West
        };

        foreach (var (nx, ny) in neighbors)
        {
            if (!_tiles.ContainsKey((nx, ny)))
            {
                _frontiers.TryAdd((nx, ny), true);
            }
        }
    }

    /// <summary>
    /// Clears all data from the map.
    /// </summary>
    public void Clear()
    {
        _tiles.Clear();
        _frontiers.Clear();
    }

    /// <summary>
    /// Gets the bounds of the known map.
    /// </summary>
    public (int MinX, int MinY, int MaxX, int MaxY)? GetBounds()
    {
        if (_tiles.IsEmpty)
            return null;

        var positions = _tiles.Keys.ToArray();
        return (
            MinX: positions.Min(p => p.X),
            MinY: positions.Min(p => p.Y),
            MaxX: positions.Max(p => p.X),
            MaxY: positions.Max(p => p.Y)
        );
    }
}

