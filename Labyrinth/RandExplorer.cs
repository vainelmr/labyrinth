using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Sys;
using Labyrinth.Tiles;

namespace Labyrinth
{
    public class RandExplorer(ICrawler crawler, IEnumRandomizer<RandExplorer.Actions> rnd)
    {
        private readonly ICrawler _crawler = crawler;
        private readonly IEnumRandomizer<Actions> _rnd = rnd;
        public Type? LastFacingTileType { get; private set; }
        
        public enum Actions
        {
            TurnLeft,
            Walk
        }

        public ICrawler Crawler => _crawler;

        public async Task<int> GetOut(int n, Inventory? bag = null)
{
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0, "n must be strictly positive");

    bag ??= new MyInventory();

    while (n > 0)
    {
        //une lecture par tour
        var facing = await _crawler.FacingTileType;
        LastFacingTileType = facing;

        // Stop si sortie
        if (facing == typeof(Outside))
            break;

        EventHandler<CrawlingEventArgs>? changeEvent;

        if (facing != typeof(Wall)
            && _rnd.Next() == Actions.Walk
            && await _crawler.TryWalk(bag) is Inventory roomContent)
        {
            await bag.TryMoveItemsFrom(
                roomContent,
                roomContent.ItemTypes.Select(_ => true).ToList()
            );
            changeEvent = PositionChanged;
        }
        else
        {
            _crawler.Direction.TurnLeft();
            changeEvent = DirectionChanged;
        }

        changeEvent?.Invoke(this, new CrawlingEventArgs(_crawler));
        n--;
    }

    return n;
}


        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;
    }

}
