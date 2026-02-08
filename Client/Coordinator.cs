using Labyrinth.Exploration;
using Microsoft.Extensions.Logging;

namespace Client;

/// <summary>
/// Coordinator responsible for assigning unique frontiers to crawlers and managing their assignments.
/// </summary>
public class Coordinator
{
    private readonly SharedMap _map;
    private readonly ILogger _logger;
    private readonly HashSet<(int X, int Y)> _assignedFrontiers = new();
    private readonly object _lock = new();
    private bool _keyFound = false;

    public Coordinator(SharedMap map, ILogger logger)
    {
        _map = map;
        _logger = logger;
    }

    /// <summary>
    /// Notifies that a key has been found. All crawlers can now attempt to pass through doors.
    /// </summary>
    public void NotifyKeyFound(int crawlerId)
    {
        lock (_lock)
        {
            if (!_keyFound)
            {
                _keyFound = true;                
                _map.InvalidatePathCache();
            }
        }
    }

    /// <summary>
    /// Checks if a key has been found by the team.
    /// </summary>
    public bool IsKeyAvailable()
    {
        lock (_lock)
        {
            return _keyFound;
        }
    }

    /// <summary>
    /// Assign a frontier to a crawler based on its current position. Returns null if no frontiers are available.
    /// </summary>
    public (int X, int Y)? AssignFrontier(int crawlerId, (int X, int Y) currentPosition, IEnumerable<(int X, int Y)>? ignoreList = null)
    {
        lock (_lock)
        {
            var availableFrontiers = _map.GetFrontiers()
                .Where(f => !_assignedFrontiers.Contains(f));

            if (ignoreList != null)
            {
                var ignoreSet = ignoreList.ToHashSet();
                availableFrontiers = availableFrontiers.Where(f => !ignoreSet.Contains(f));
            }

            var availableList = availableFrontiers.ToList();

            if (availableList.Count == 0)
            {
                return null;
            }

            var closest = availableList
                .OrderBy(f => Math.Abs(f.X - currentPosition.X) + Math.Abs(f.Y - currentPosition.Y))
                .First();

            _assignedFrontiers.Add(closest);
            return closest;
        }
    }

    /// <summary>
    /// Free a frontier when a crawler has finished exploring it or is no longer targeting it, allowing other crawlers to be assigned to it.
    /// </summary>
    public void ReleaseFrontier((int X, int Y) frontier)
    {
        lock (_lock)
        {
            _assignedFrontiers.Remove(frontier);
        }
    }

    /// <summary>
    /// Return the number of currently assigned frontiers, useful for monitoring and debugging purposes.
    /// </summary>
    public int AssignedFrontierCount
    {
        get
        {
            lock (_lock)
            {
                return _assignedFrontiers.Count;
            }
        }
    }
}
