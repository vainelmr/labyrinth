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

    public Coordinator(SharedMap map, ILogger logger)
    {
        _map = map;
        _logger = logger;
    }

    /// <summary>
    /// Assign a frontier to a crawler based on its current position. Returns null if no frontiers are available.
    /// </summary>
    public (int X, int Y)? AssignFrontier(int crawlerId, (int X, int Y) currentPosition)
    {
        lock (_lock)
        {
            var availableFrontiers = _map.GetFrontiers()
                .Where(f => !_assignedFrontiers.Contains(f))
                .ToList();

            if (availableFrontiers.Count == 0)
            {
                _logger.LogDebug("Crawler {CrawlerId}: No available frontiers", crawlerId);
                return null;
            }

            var closest = availableFrontiers
                .OrderBy(f => Math.Abs(f.X - currentPosition.X) + Math.Abs(f.Y - currentPosition.Y))
                .First();

            _assignedFrontiers.Add(closest);
            _logger.LogDebug("Crawler {CrawlerId}: Assigned frontier ({X},{Y})", crawlerId, closest.X, closest.Y);
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
            _logger.LogDebug("Released frontier ({X},{Y})", frontier.X, frontier.Y);
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
