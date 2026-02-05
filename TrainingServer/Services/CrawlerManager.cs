using System.Collections.Concurrent;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Dto = ApiTypes;
using Labyrinth.ApiClient;

namespace TrainingServer.Services;

public sealed class CrawlerManager
{
    private readonly MazeRepository _mazeRepo;

    // Pour chaque crawler on stocke :
    // - l'ICrawler (mouvement + vision)
    // - le bag (inventaire perso du crawler)
    // - le items (inventaire de la case où le crawler se trouve)
    private readonly ConcurrentDictionary<Guid, CrawlerState> _crawlers = new();

    private sealed class CrawlerState
    {
        public required ICrawler Crawler { get; init; }
        public required Inventory Bag { get; init; }

        // Inventaire de la tuile actuelle (room/door) où est le crawler.
        // Null tant que le crawler n'a pas réussi à entrer quelque part via TryWalk.
        public Inventory? Items { get; set; }
    }

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

        _crawlers[id] = new CrawlerState
        {
            Crawler = crawler,
            Bag = bag,
            Items = null
        };

        return (id, crawler, bag);
    }

    public bool Delete(Guid id) => _crawlers.TryRemove(id, out _);

    public async Task<Dto.Crawler?> PatchCrawlerAsync(Guid id, Dto.Crawler incoming)
    {

        
        if (!_crawlers.TryGetValue(id, out var state))
            return null;


        var crawler = state.Crawler;
        var bag = state.Bag;

        Console.WriteLine($"PATCH: incoming walking={incoming.Walking} direction={incoming.Direction}");
        Console.WriteLine($"Before: ({crawler.X},{crawler.Y}) dir={crawler.Direction}");

        // 1) direction
        var target = incoming.Direction.GetCrawlerDirection();
        for (int i = 0; i < 4 && crawler.Direction != target; i++)
        {
            crawler.Direction.TurnLeft();
        }

        // 2) marche
        if (incoming.Walking)
        {
            var moveResult = await crawler.TryMoveAsync(bag);
            if (moveResult is MoveResult.Success success && success.TileInventory is not null)
            {
                state.Items = success.TileInventory;
            }
        }

        

        // 3) vision
        var facingType = await crawler.GetFrontTileTypeAsync();

        // 4) réponse
        return new Dto.Crawler
        {
            Id = id,
            X = crawler.X,
            Y = crawler.Y,
            Direction = crawler.Direction.GetApiDirection(),
            Walking = false,
            FacingTile = facingType.GetApiTileType(),
            Bag = bag.GetApiInventoryItems(),
            Items = state.Items is null ? Array.Empty<Dto.InventoryItem>() : state.Items.GetApiInventoryItems()
        };
    }

    /// <summary>
    /// Déplace des items entre bag et items.
    /// L'endpoint est appelé SUR LA SOURCE.
    /// PUT /.../bag   => source=bag   destination=items
    /// PUT /.../items => source=items destination=bag
    /// </summary>
    public async Task<(bool ok, Dto.InventoryItem[] remainingInSource)?> MoveItemsAsync(
        Guid id,
        string sourceTypeName,
        IList<bool> movesRequired
    )
    {
        if (!_crawlers.TryGetValue(id, out var state))
            return null;

        var bag = state.Bag;
        var items = state.Items;

        if (items is null)
            throw new InvalidOperationException("Crawler has no active room inventory (walk into a room first).");

        Inventory source, target;

        if (string.Equals(sourceTypeName, "bag", StringComparison.OrdinalIgnoreCase))
        {
            source = bag;
            target = items;
        }
        else if (string.Equals(sourceTypeName, "items", StringComparison.OrdinalIgnoreCase))
        {
            source = items;
            target = bag;
        }
        else
        {
            throw new ArgumentException("sourceTypeName must be 'bag' or 'items'.", nameof(sourceTypeName));
        }

        // Le client envoie exactement 1 bool par item dans la SOURCE
        var sourceItemTypes = await source.GetItemTypesAsync();
        if (movesRequired.Count != sourceItemTypes.Count)
        {
            return (false, source.GetApiInventoryItems());
        }

        var ok = await target.TryMoveItemsFrom(source, movesRequired);
        return (ok, source.GetApiInventoryItems());
    }
}
