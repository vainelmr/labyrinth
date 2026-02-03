using System.Collections.Concurrent;
using TrainingServer.Models;

namespace TrainingServer.Services;

/// <summary>
/// Registry that manages all active crawlers in the training server.
/// </summary>
public class CrawlerRegistry
{
    private readonly ConcurrentDictionary<Guid, CrawlerState> _crawlers = new();

    /// <summary>
    /// Register a new crawler with initial position and direction.
    /// </summary>
    public CrawlerState Register(int x, int y, ApiTypes.Direction direction)
    {
        var crawler = new CrawlerState
        {
            Id = Guid.NewGuid(),
            X = x,
            Y = y,
            Dir = direction,
            Walking = false
        };

        if (!_crawlers.TryAdd(crawler.Id, crawler))
        {
            throw new InvalidOperationException($"Failed to register crawler {crawler.Id}");
        }

        return crawler;
    }

    /// <summary>
    /// Get a crawler by its ID.
    /// </summary>
    public CrawlerState? GetCrawler(Guid id)
    {
        _crawlers.TryGetValue(id, out var crawler);
        return crawler;
    }

    /// <summary>
    /// Update crawler state.
    /// </summary>
    public bool UpdateCrawler(CrawlerState crawler)
    {
        return _crawlers.TryUpdate(crawler.Id, crawler, _crawlers[crawler.Id]);
    }

    /// <summary>
    /// Get all registered crawlers.
    /// </summary>
    public IEnumerable<CrawlerState> GetAll() => _crawlers.Values;

    /// <summary>
    /// Remove a crawler from the registry.
    /// </summary>
    public bool Remove(Guid id)
    {
        return _crawlers.TryRemove(id, out _);
    }

    /// <summary>
    /// Get the number of registered crawlers.
    /// </summary>
    public int Count => _crawlers.Count;
}
