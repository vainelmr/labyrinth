using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using System.Net.Http.Json;
using Dto = ApiTypes;

namespace Labyrinth.ApiClient
{
    /// <summary>
    /// Represents an active session with a contest server, providing access to crawlers, inventories, and
    /// labyrinth-building functionality for a specific contest run. This class is not thread-safe.
    /// </summary>
    public class ContestSession
    {
        /// <summary>
        /// Opens a new contest session by connecting to the specified server and preparing a labyrinth with the specified settings.
        /// </summary>
        /// <param name="serverUrl">The base URL of the contest server to connect to. Must be a valid absolute URI.</param>
        /// <param name="appKey">The application key used to authenticate the session with the server.</param>
        /// <param name="settings">Optional settings to configure the labyrinth. If null, default settings are used.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a ContestSession instance
        /// connected to the specified server.</returns>
        /// <exception cref="FormatException">Thrown if response format changed</exception>
        public static async Task<ContestSession> Open(Uri serverUrl, Guid appKey, Dto.Settings? settings = null)
        {
            var http = new HttpClient() { BaseAddress = serverUrl };
            Dto.Crawler crawlerDto;
            try
            {
                crawlerDto = await CreateCrawler(http, appKey, settings);
            }
            catch (HttpRequestException ex) when (IsTooManyCrawlers(ex))
            {
                await TryReleaseCrawlersForAppKey(http, appKey);
                crawlerDto = await CreateCrawler(http, appKey, settings);
            }
            return crawlerDto is { } dto
                ? new ContestSession(http, appKey, dto)
                : throw new FormatException("Failed to read a crawler");
        }

        /// <summary>
        /// Asynchronously quit the labyrinth by deleting all crawlers.
        /// </summary>
        /// <remarks>If any crawler fails to close, the returned task will complete in a faulted state
        /// with the corresponding exception. This method can be awaited to ensure that all resources are released
        /// before proceeding.</remarks>
        /// <returns>A task that represents the asynchronous close operation. The task completes when all crawlers have been
        /// closed.</returns>
        public async Task Close()
        {
            var res = await Task.WhenAll(_crawlers.Select(c => c.Crawler.Delete()));
            _crawlers.Clear();
        }

        /// <summary>
        /// Gets an enumerable collection of crawlers available in the current context.
        /// </summary>
        public IEnumerable<ICrawler> Crawlers => _crawlers.Select(c => c.Crawler);

        /// <summary>
        /// Gets a collection of all inventory bags associated with the crawlers.
        /// </summary>
        public IEnumerable<Inventory> Bags => _crawlers.Select(c => c.Bag);

        /// <summary>
        /// Gets the builder instance used to construct contest labyrinths.
        /// </summary>
        public IBuilder Builder => _builder;

        /// <summary>
        /// Creates and adds a new crawler to the session
        /// </summary>
        public async Task<ICrawler> NewCrawler()
        {
            Dto.Crawler newCrawlerDto;
            try
            {
                newCrawlerDto = await CreateCrawler(_http, _appKey);
            }
            catch (HttpRequestException ex) when (IsTooManyCrawlers(ex))
            {
                await TryReleaseCrawlersForAppKey(_http, _appKey);
                newCrawlerDto = await CreateCrawler(_http, _appKey);
            }
            var (crawler, bag) = NewCrawlerAndItsBag(_appKey, newCrawlerDto);
            _crawlers.Add((crawler, bag));
            return crawler;
        }

        private ContestSession(HttpClient http, Guid appKey, Dto.Crawler crawler)
        {
            _http = http;
            _appKey = appKey;
            _crawlers = new List<(ClientCrawler, Inventory)> {
                NewCrawlerAndItsBag(appKey, crawler)
            };
            _builder = new(_crawlers[0].Crawler);
        }

        private (ClientCrawler, Inventory) NewCrawlerAndItsBag(Guid appKey, Dto.Crawler crawlerDto)
        {
            var crawler = new ClientCrawler(_http.BaseAddress!, appKey, crawlerDto, out var inventory);

            crawler.Changed += Crawler_Changed;
            return (crawler, inventory);
        }

        private void Crawler_Changed(object? sender, EventArgs e)
        {
            if(sender is ClientCrawler crawler)
            {
                _builder.UpdateFacingTile(crawler);
            }
        }

        private static bool IsTooManyCrawlers(HttpRequestException ex) =>
            ex.Message.Contains("Trop de crawlers", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Too many crawler", StringComparison.OrdinalIgnoreCase);

        private static async Task TryReleaseCrawlersForAppKey(HttpClient http, Guid appKey)
        {
            try
            {
                var response = await http.GetAsync($"/crawlers?appKey={appKey}");
                if (!response.IsSuccessStatusCode) return;
                var crawlers = await response.Content.ReadFromJsonAsync<Dto.Crawler[]>();
                if (crawlers is null or { Length: 0 }) return;
                foreach (var c in crawlers)
                    await http.DeleteAsync($"/crawlers/{c.Id}?appKey={appKey}");
            }
            catch { /* serveur peut ne pas exposer GET /crawlers */ }
        }

        private static async Task<Dto.Crawler> CreateCrawler(HttpClient http, Guid appKey, Dto.Settings? settings = null)
        {
            var response = await http.PostAsJsonAsync(
                $"/crawlers?appKey={appKey}", settings
            );
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var msg = body.Contains("Too many crawler", StringComparison.OrdinalIgnoreCase)
                    ? "Trop de crawlers pour cette clé. Fermez toutes les instances du client (Ctrl+C) puis réessayez."
                    : $"Le serveur a répondu {(int)response.StatusCode} ({response.ReasonPhrase}). {body}";
                throw new HttpRequestException(msg);
            }
            return await response.Content.ReadFromJsonAsync<Dto.Crawler>() is Dto.Crawler crawlerDto
                ? crawlerDto
                : throw new FormatException("Failed to read a crawler");
        }

        private readonly HttpClient _http;
        private readonly Guid _appKey;
        private readonly RemoteContestLabyrinthBuilder _builder;
        private readonly IList<(ClientCrawler Crawler, Inventory Bag)> _crawlers;

        private class RemoteContestLabyrinthBuilder : IBuilder
        {
            public RemoteContestLabyrinthBuilder(ICrawler first)
            {
                XStart = first.X;
                YStart = first.Y;
                Height = YStart * 2 + 1;
                Width = Height + 2;
                Tiles = new Tile[Width, Height];
            }

            public readonly int XStart, YStart;

            public readonly int Width, Height;

            public readonly Tile[,] Tiles;

            public Tile[,] Build()
            {
                for (var x = 0; x < Width; x++)
                {
                    for (var y = 0; y < Height; y++)
                    {
                        if (int.IsOddInteger(x + y))
                            Tiles[x, y] = new Unknown();
                        else if (int.IsEvenInteger(Math.Min(
                            Math.Min(x, Width  + 1 - x),
                            Math.Min(y, Height + 1 - y)
                        )))
                            Tiles[x, y] = Wall.Singleton;
                        else
                            Tiles[x, y] = new Room();
                    }
                }

                var center = Height / 2;

                StartPositionFound?.Invoke(this, new StartEventArgs(XStart, center));
                return Tiles;
            }
            public event EventHandler<StartEventArgs>? StartPositionFound;

            private bool IsInRange(int val, int max) => 0 <= val && val < max;
            public async void UpdateFacingTile(ClientCrawler crawler)
            {
                var x = crawler.X + crawler.Direction.DeltaX;
                var y = crawler.Y + crawler.Direction.DeltaY;

                if (IsInRange(x, Width) && IsInRange(y, Height) && Tiles[x, y] is Unknown)
                {
                    var tileType = await crawler.GetFrontTileTypeAsync();
                    Tiles[x, y] = tileType.NewTile();
                }
            }
        }
    }
}
