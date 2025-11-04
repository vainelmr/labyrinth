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
        
        public enum Actions
        {
            TurnLeft,
            Walk
        }

        public int GetOut(int n)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0, "n must be strictly positive");
            MyInventory bag = new ();

            for( ; n > 0 && _crawler.FacingTile is not Outside; n--)
            {
                if(_crawler.FacingTile.IsTraversable 
                    && _rnd.Next() == Actions.Walk)
                {
                    _crawler.Walk().SwapItems(bag);
                }
                else
                {
                    _crawler.Direction.TurnLeft();
                }
                if (_crawler.FacingTile is Door door && door.IsLocked
                    && bag.HasItem && bag.ItemType == typeof(Key))
                {
                    door.Open(bag);
                }
            }
            return n;
        }
    }
}
