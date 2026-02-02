using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Sys;

namespace Labyrinth
{
    public class RandExplorer(ICrawler crawler, IEnumRandomizer<RandExplorer.Actions> rnd)
    {
        private readonly ICrawler _crawler = crawler;
        private readonly IEnumRandomizer<Actions> _rnd = rnd;
        
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
            while (n > 0 && !await _crawler.IsFacingExitAsync())
            {
                var action = _rnd.Next();

                if (action == Actions.Walk)
                {
                    var moveResult = await _crawler.TryMoveAsync(bag);
                    if (moveResult is MoveResult.Success success && success.TileInventory is { } roomContent)
                    {
                        var which = (await roomContent.GetItemTypesAsync()).Select(_ => true).ToList();
                        await bag.TryMoveItemsFrom(roomContent, which);
                        PositionChanged?.Invoke(this, new CrawlingEventArgs(_crawler));
                    }
                    else
                    {
                        _crawler.Direction.TurnLeft();
                        DirectionChanged?.Invoke(this, new CrawlingEventArgs(_crawler));
                    }
                }
                else
                {
                    _crawler.Direction.TurnLeft();
                    DirectionChanged?.Invoke(this, new CrawlingEventArgs(_crawler));
                }
                n--;
            }
            return n;
        }

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;
    }

}
