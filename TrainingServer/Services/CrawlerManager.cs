
using System.Collections.Concurrent;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Dto = ApiTypes;

namespace TrainingServer.Services;

public sealed class CrawlerManager
{
    private readonly MazeRepository _mazeRepo;
    private readonly ConcurrentDictionary<Guid, (ICrawler crawler, Inventory bag)> _crawlers = new();

    public CrawlerManager(MazeRepository mazeRepo)
    {
        _mazeRepo = mazeRepo;
    }

    public (Guid id, ICrawler crawler, Inventory bag) Create()
    {
        var lab = _mazeRepo.CreateLabyrinth();
        var crawler = lab.NewCrawler();
        var bag = new MyInventory();
        var id = Guid.NewGuid();

        _crawlers[id] = (crawler, bag);
        return (id, crawler, bag);
    }

    public bool TryGet(Guid id, out ICrawler crawler, out Inventory bag)
    {
        if (_crawlers.TryGetValue(id, out var value))
        {
            crawler = value.crawler;
            bag = value.bag;
            return true;
        }
        crawler = null!;
        bag = null!;
        return false;
    }

    public bool Delete(Guid id) => _crawlers.TryRemove(id, out _);
}
