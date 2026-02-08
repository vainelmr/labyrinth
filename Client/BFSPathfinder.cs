using Labyrinth.Exploration;
using Microsoft.Extensions.Logging;

namespace Client;

/// <summary>
/// Pathfinder using Breadth-First Search (BFS) to find the shortest path in the labyrinth.
/// </summary>
public class BFSPathfinder
{
    private readonly SharedMap _map;
    private readonly ILogger _logger;
    private readonly Dictionary<(int, int, int, bool, int), List<(int, int)>?> _pathCache = new();
    private int _cachedMapVersion = -1;
    private readonly object _pathLock = new();

    public BFSPathfinder(SharedMap map, ILogger logger)
    {
        _map = map;
        _logger = logger;
    }

    /// <summary>
    /// Finds the shortest path from the start position to the target position using BFS.
    /// Thread-safe when multiple crawlers call it in parallel.
    /// </summary>
    /// <param name="canPassThroughDoors">If true, doors are treated as passable tiles. If false, doors block the path.</param>
    /// <param name="impassableDoorsForMe">Set of specific door positions that are impassable for this crawler.</param>
    /// <returns>List of coordinates representing the path, or null if no path is found.</returns>
    public List<(int X, int Y)>? FindPath((int X, int Y) start, (int X, int Y) target, bool canPassThroughDoors = true, HashSet<(int X, int Y)>? impassableDoorsForMe = null)
    {
        impassableDoorsForMe ??= new HashSet<(int X, int Y)>();

        lock (_pathLock)
        {
            if (_cachedMapVersion != _map.Version)
            {
                _pathCache.Clear();
                _cachedMapVersion = _map.Version;
            }

            int doorSetHash = impassableDoorsForMe.Count > 0 ?
                impassableDoorsForMe.Select(d => d.X * 1000 + d.Y).OrderBy(h => h).Aggregate(0, (acc, h) => acc ^ h) : 0;
            var cacheKey = (start.X, start.Y, target.X * 1000 + target.Y, canPassThroughDoors, doorSetHash);
            if (_pathCache.TryGetValue(cacheKey, out var cachedPath))
            {
                return cachedPath;
            }

            var queue = new Queue<(int X, int Y)>();
            var cameFrom = new Dictionary<(int, int), (int, int)?>();
            var visited = new HashSet<(int, int)>();

            queue.Enqueue(start);
            cameFrom[start] = null;
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == target)
                {
                    var path = ReconstructPath(cameFrom, start, target);
                    _pathCache[cacheKey] = path;
                    return path;
                }

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (visited.Contains(neighbor))
                        continue;

                    var knowledge = _map.Get(neighbor.X, neighbor.Y);

                    if (knowledge == TileKnowledge.Wall)
                        continue;

                    if (knowledge == TileKnowledge.Door && impassableDoorsForMe.Contains(neighbor) && neighbor != target)
                        continue;

                    if (knowledge == TileKnowledge.Door && !canPassThroughDoors && neighbor != target)
                        continue;

                    if (knowledge == TileKnowledge.Outside && neighbor != target)
                        continue;

                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            _pathCache[cacheKey] = null;
            return null;
        }
    }

    private List<(int X, int Y)> ReconstructPath(
        Dictionary<(int, int), (int, int)?> cameFrom,
        (int X, int Y) start,
        (int X, int Y) target)
    {
        var path = new List<(int X, int Y)>();
        var current = target;

        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current]!.Value;
        }

        path.Reverse();
        
        return path;
    }

    private IEnumerable<(int X, int Y)> GetNeighbors((int X, int Y) pos)
    {
        yield return (pos.X, pos.Y - 1); // North
        yield return (pos.X, pos.Y + 1); // South
        yield return (pos.X - 1, pos.Y); // West
        yield return (pos.X + 1, pos.Y); // East
    }
}
