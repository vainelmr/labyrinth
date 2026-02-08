using Labyrinth.ApiClient;
using Labyrinth.Exploration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Net.Http.Json;
using Dto = ApiTypes;

namespace Client;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configuring logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<Program>();

        if (args.Length < 2)
        {
            logger.LogError("Usage: Client <serverUrl> <appKey> [appKeyTeam2]");
            logger.LogError("  With 2 keys, runs two teams in parallel (competition scenario).");
            return 1;
        }

        var serverUrl = args[0];
        var appKey = args[1];
        var secondAppKey = args.Length > 2 ? args[2] : null;

        if (secondAppKey is null)
        {
            logger.LogInformation("Client starting with server URL: {ServerUrl} and AppKey: {AppKey}", serverUrl, appKey.Substring(0, Math.Min(8, appKey.Length)) + "...");
            try
            {
                await CleanupExistingCrawlers(serverUrl, appKey, logger);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Cleanup warning: {Message}", ex.Message);
            }
            return await RunExploration(serverUrl, appKey, logger);
        }

        // Mode 2 équipes (compétition) : lancer les deux sessions en parallèle
        logger.LogInformation("Competition mode: 2 teams, server {ServerUrl}", serverUrl);
        try
        {
            await CleanupExistingCrawlers(serverUrl, appKey, logger);
            await CleanupExistingCrawlers(serverUrl, secondAppKey, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Cleanup warning: {Message}", ex.Message);
        }

        var loggerTeam1 = loggerFactory.CreateLogger("Team1");
        var loggerTeam2 = loggerFactory.CreateLogger("Team2");
        var tasks = new[]
        {
            RunExploration(serverUrl, appKey, loggerTeam1),
            RunExploration(serverUrl, secondAppKey, loggerTeam2)
        };
        var results = await Task.WhenAll(tasks);
        var exitReached = results.All(r => r == 0);
        logger.LogInformation("Both teams finished. Exit reached by all: {Success}", exitReached);
        return exitReached ? 0 : 1;
    }

    static async Task CleanupExistingCrawlers(string serverUrl, string appKey, ILogger logger)
    {
        using var tempClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        var getUrl = $"/crawlers?appKey={appKey}";
        
        try 
        {
            var crawlers = await tempClient.GetFromJsonAsync<Dto.Crawler[]>(getUrl);
            if (crawlers != null && crawlers.Length > 0)
            {
                foreach (var c in crawlers)
                {
                    await tempClient.DeleteAsync($"/crawlers/{c.Id}?appKey={appKey}");
                    await Task.Delay(500);
                }
            }
        }
        catch (HttpRequestException ex)
        {
             if(ex.StatusCode != System.Net.HttpStatusCode.NotFound)
                logger.LogWarning("Failed to list crawlers during cleanup: {Status} (This is expected if no crawlers exist or key is fresh)", ex.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Cleanup check skipped: {Message}", ex.Message);
        }
    }

    static async Task<int> RunExploration(string serverUrl, string appKey, ILogger logger, bool enableDisplay = true)
    {
        try
        {
            var session = await ContestSession.Open(new Uri(serverUrl), Guid.Parse(appKey));
            
            logger.LogInformation("Spawning additional crawlers...");
            int spawnedCount = 1;
            
            try 
            { 
                await session.NewCrawler(); 
                spawnedCount++;
                logger.LogInformation("Crawler 2 spawned successfully");
            } 
            catch (Exception ex) 
            { 
                logger.LogWarning("Failed to spawn crawler 2: {Msg}", ex.Message);
            }
            
            try 
            { 
                await session.NewCrawler(); 
                spawnedCount++;
                logger.LogInformation("Crawler 3 spawned successfully");
            } 
            catch (Exception ex) 
            { 
                logger.LogWarning("Failed to spawn crawler 3: {Msg}", ex.Message);
            }
            
            logger.LogInformation("Total crawlers spawned: {Count}/3", spawnedCount);

            var crawlers = session.Crawlers.ToList();
            var bags = session.Bags.ToList();
            
            logger.LogInformation("Session opened with {Count} crawler(s)", crawlers.Count);
            
            for (int i = 0; i < crawlers.Count; i++)
            {
                logger.LogInformation("Crawler {Id} initial position: ({X},{Y}) facing {Direction}", 
                    i, crawlers[i].X, crawlers[i].Y, crawlers[i].Direction);
            }

            var map = new SharedMap();
            var coordinator = new Coordinator(map, logger);
            var pathfinder = new BFSPathfinder(map, logger);
            var mapExporter = new MapExporter(map);

            var explorers = new List<Explorer>();
            for (int i = 0; i < crawlers.Count; i++)
            {
                var crawler = crawlers[i];
                var bag = bags[i];
                
                var explorer = new Explorer(i, crawler, bag, map, coordinator, pathfinder, logger);
                explorers.Add(explorer);
                
                logger.LogInformation("Crawler {Id} created at ({X}, {Y})", i, crawler.X, crawler.Y);
            }

            const int maxSteps = 2000;
            bool exitReached = false;
            
            var crawlerColors = new[] { 
                ConsoleColor.Red, 
                ConsoleColor.Cyan, 
                ConsoleColor.Magenta, 
                ConsoleColor.Yellow, 
                ConsoleColor.Green 
            };

            for (int step = 0; step < maxSteps; step++)
            {
                var results = await Task.WhenAll(explorers.Select(e => e.StepAsync()));

                if (results.All(r => !r))
                {
                    logger.LogInformation("Step {Step}: All crawlers finished exploration", step);
                    exitReached = true;
                    break;
                }
                if (map.ExitFound && results.Any(r => !r))
                {
                    logger.LogInformation("Step {Step}: A crawler reached the exit", step);
                    exitReached = true;
                    break;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"═══ Step {step} ═══");
                foreach (var exp in explorers)
                {
                    var index = explorers.IndexOf(exp);
                    var targetStr = exp.Target.HasValue ? $"Target:({exp.Target.Value.X},{exp.Target.Value.Y})" : "No Target";
                    sb.AppendLine($"Crawler {index}: {targetStr}");
                }
                sb.AppendLine($"Map: {map.KnownCount} known, {map.GetFrontiers().Count()} frontiers, {coordinator.AssignedFrontierCount} assigned");
                if (map.ExitFound)
                    sb.AppendLine($"EXIT FOUND: ({map.ExitPosition!.Value.X},{map.ExitPosition!.Value.Y})");
                try { Console.Clear(); } catch { }
                Console.WriteLine(sb.ToString());
                var currentCrawlers = session.Crawlers.ToList();
                var crawlerPositions = currentCrawlers.Select((c, idx) =>
                    (c.X, c.Y, c.Direction.ToString() ?? "Unknown")).ToList();
                mapExporter.PrintMapWithColors(crawlerPositions, crawlerColors);
                await Task.Delay(25);
            }

            Console.WriteLine("\n");
            logger.LogInformation("════════════════════════════════════");
            if (exitReached)
            {
                logger.LogInformation("EXIT REACHED!");
            }
            else
            {
                logger.LogWarning("EXIT NOT REACHED");
            }
            logger.LogInformation("════════════════════════════════════");
            
            var stats = mapExporter.GetStatistics();
            logger.LogInformation("{Stats}", stats);
            
            var finalCrawlers = session.Crawlers.ToList();
            for (int i = 0; i < finalCrawlers.Count; i++)
            {
                var crawler = finalCrawlers[i];
                logger.LogInformation("Crawler {Id} final position: ({X},{Y}) facing {Direction}", 
                    i, crawler.X, crawler.Y, crawler.Direction);
            }
            
            logger.LogInformation("FINAL MAP");
            var finalCrawlerPositions = finalCrawlers.Select((c, idx) => 
                (c.X, c.Y, c.Direction.ToString() ?? "Unknown")).ToList();
            var finalMap = mapExporter.ExportToAscii(finalCrawlerPositions);
            logger.LogInformation("\n{Map}", finalMap);

            await session.Close();
            return exitReached ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during exploration");
            return 1;
        }
    }
}
