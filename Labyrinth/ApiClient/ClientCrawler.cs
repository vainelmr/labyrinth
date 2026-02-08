using Labyrinth.Crawl;
using Labyrinth.Items;
using System.Diagnostics.CodeAnalysis;
using Dto = ApiTypes;
using System.Net.Http.Json;

namespace Labyrinth.ApiClient
{
    public class ClientCrawler : ICrawler
    {
        public ClientCrawler(Uri serverUrl, Guid appKey, Dto.Crawler createdCrawler, out Inventory bag)
        {
            _appKey = appKey;
            _httpClient = new HttpClient() { BaseAddress = serverUrl };
            bag = _bag   = new RemoteInventory(this, "bag");
            _items = new RemoteInventory(this, "items");
            UpdateCache(createdCrawler);
        }

        public int X => _cache.X;

        public int Y => _cache.Y;

        public Direction Direction => _direction;

        public Task<Type> GetFrontTileTypeAsync() => GetFacingTileTypeAsync();

        public async Task<bool> IsFacingExitAsync()
        {
            if (_cacheDirection != _direction && !await UpdateRemote())
            {
                while (_direction != _cacheDirection)
                    _direction.TurnLeft();
            }
            return _cache.FacingTile == Dto.TileType.Outside;
        }

        public async Task<MoveResult> TryMoveAsync(Inventory myInventory)
        {
            if (myInventory != _bag)
            {
                throw new ArgumentException("Can only walk with own bag inventory (returned by labyrinth).", nameof(myInventory));
            }
            _cache.Walking = true;
            return await UpdateRemote()
                ? new MoveResult.Success(_items)
                : new MoveResult.Failure();
        }

        public async Task<bool> Delete() =>
            (await _httpClient.DeleteAsync(
                $"/crawlers/{_cache.Id}?appKey={_appKey}"
            )).IsSuccessStatusCode;
        
        public event EventHandler? Changed;

        public Inventory Bag => _bag;
        public Inventory CurrentTileInventory => _items;

        private async Task<Type> GetFacingTileTypeAsync()
        {
            if (_cacheDirection != _direction && !await UpdateRemote())
            {
                while(_direction!=_cacheDirection)
                {
                    _direction.TurnLeft();
                }
            }
            return _cache.FacingTile.GetCrawlerFacingTileType();
        }

        private async Task<bool> UpdateRemote()
        {
            _cache.Direction = _direction.GetApiDirection();
            _cache.Bag = _bag.ToDtoArray();

            HttpResponseMessage? response = null;
            var url = $"/crawlers/{_cache.Id}?appKey={_appKey}";

            for (int i = 0; i < 3; i++)
            {
                try 
                {
                    response = await _httpClient.PatchAsJsonAsync(url, _cache);
                    if (response.IsSuccessStatusCode) break;

                    var body = "";
                    try { body = await response.Content.ReadAsStringAsync(); } catch {}

                    if ((int)response.StatusCode == 403 || (int)response.StatusCode == 429)
                    {
                        await Task.Delay(2000 * (i + 1));
                    }
                    else
                    {
                        await Task.Delay(500); 
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"[API-EXCEPTION] PATCH {url} : {ex.Message}");
                    await Task.Delay(500);
                }
            }

            if (response != null && response.IsSuccessStatusCode &&
                await response.Content.ReadFromJsonAsync<Dto.Crawler>() is Dto.Crawler lastState)
            {
                UpdateCache(lastState);
                Changed?.Invoke(this, EventArgs.Empty);
                return true;
            }
            else
            {
                _cache.Walking = false;
                return false;
            }
        }

        [MemberNotNull(nameof(_cacheDirection), nameof(_direction), nameof(_cache))]
        private void UpdateCache(Dto.Crawler updatedCrawler)
        {
            _cache = updatedCrawler;
            _cacheDirection = _cache.Direction.GetCrawlerDirection();
            _direction = (Direction)_cacheDirection.Clone();
            if (_cache.Bag   is not null) _bag  .UpdateList(_cache.Bag.Length);
            if (_cache.Items is not null) _items.UpdateList(_cache.Items.Length);
        }

        private async Task<bool> UpdateRemoteInventories(RemoteInventory from, RemoteInventory to, IList<bool> movesRequired)
        {            
            var targetEndpoint = from.TypeName;
            var url = $"/crawlers/{_cache.Id}/{targetEndpoint}?appKey={_appKey}";
            
            var previousCount = (await from.GetItemTypesAsync()).Count;
            
            var payload = movesRequired.Select(moveReq => new Dto.InventoryItem { 
                    Type = Dto.ItemType.Key, 
                    MoveRequired = moveReq 
                }).ToArray();
            
            HttpResponseMessage? response = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    response = await _httpClient.PutAsJsonAsync(url, payload);
                    if (response.IsSuccessStatusCode) break;
                    
                    var body = "";
                    try { body = await response.Content.ReadAsStringAsync(); } catch {}

                     if ((int)response.StatusCode == 403 || (int)response.StatusCode == 429)
                    {
                        var waitTime = 2000 * (i+1);
                        await Task.Delay(waitTime);
                    }
                    else
                    {
                        await Task.Delay(500);
                    }
                }
                catch (Exception)
                {
                    await Task.Delay(500);
                }
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                var newCount = (await response.Content.ReadFromJsonAsync<Dto.InventoryItem[]>())?.Length ?? 0;
                
                int takenCount = previousCount - newCount;
                
                if (takenCount < 0 || takenCount == 0 && movesRequired.Any(x => x))
                {
                    takenCount = movesRequired.Count(x => x);
                }


                from.UpdateList(newCount);
                
                var currentToCount = (await to.GetItemTypesAsync()).Count;
                to.UpdateList(currentToCount + takenCount);
                
                return true;
            }
            return false;
        }

        private Dto.Crawler _cache;
        private Direction _cacheDirection, _direction;
        private readonly RemoteInventory _bag, _items;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Guid _appKey;

        private class RemoteInventory(ClientCrawler parent, string type) : Inventory
        {
            public string TypeName { get; init; } = type;

            public override async Task<IReadOnlyList<Type>> GetItemTypesAsync()
            {
                if (TypeName == "items")
                {
                    try
                    {
                        var items = await _parent.FetchInventoryAsync("items");
                        UpdateList(items.Length);
                    }
                    catch
                    {
                    }
                }

                return _items.Select(i => i.GetType()).ToList();
            }

            public override async Task<bool> TryMoveItemsFrom(Inventory from, IList<bool> movesRequired)
            {
                if(from != _parent._bag && from != _parent._items)
                {
                    throw new ArgumentException("Can only move items between the inventories of the crawler", nameof(from));
                }
                return await _parent.UpdateRemoteInventories((RemoteInventory)from, this, movesRequired);
            }

            public void UpdateList(int itemCount)
            {
                while(_items.Count > itemCount)
                    _items.RemoveAt(_items.Count - 1);
                while(_items.Count < itemCount)
                    _items.Add(new Key()); 
            }

            internal Dto.InventoryItem[] ToDtoArray()
            {
                return _items.Select(i => new Dto.InventoryItem 
                { 
                    Type = Dto.ItemType.Key 
                }).ToArray();
            }

            private ClientCrawler _parent = parent;
        }

        private async Task<Dto.InventoryItem[]> FetchInventoryAsync(string type)
{
    var res = await _httpClient.GetAsync($"/crawlers/{_cache.Id}/{type}?appKey={_appKey}");
    res.EnsureSuccessStatusCode();
    return await res.Content.ReadFromJsonAsync<Dto.InventoryItem[]>() ?? Array.Empty<Dto.InventoryItem>();
}

    }
}
