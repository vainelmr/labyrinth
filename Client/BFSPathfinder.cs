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

    public BFSPathfinder(SharedMap map, ILogger logger)
    {
        _map = map;
        _logger = logger;
    }

    /// <summary>
    /// Finds the shortest path from the start position to the target position using BFS.
    /// </summary>
    /// <returns>List of coordinates representing the path, or null if no path is found.</returns>
    public List<(int X, int Y)>? FindPath((int X, int Y) start, (int X, int Y) target)
    {
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
                return ReconstructPath(cameFrom, start, target);
            }

            foreach (var neighbor in GetNeighbors(current))
            {
                if (visited.Contains(neighbor))
                    continue;

                var knowledge = _map.Get(neighbor.X, neighbor.Y);
                
                if (knowledge == TileKnowledge.Wall || knowledge == TileKnowledge.Door)
                    continue;

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        _logger.LogWarning("No path found from ({StartX},{StartY}) to ({TargetX},{TargetY})", 
            start.X, start.Y, target.X, target.Y);
        return null;
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
        
        _logger.LogDebug("Path found : {PathLength} steps", path.Count);
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
