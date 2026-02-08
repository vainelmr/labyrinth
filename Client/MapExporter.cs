using Labyrinth.Exploration;
using System.Text;

namespace Client;

/// <summary>
/// Exports the map in ASCII format for debugging and visualization
/// </summary>
public class MapExporter
{
    private readonly SharedMap _map;

    public MapExporter(SharedMap map)
    {
        _map = map;
    }

    /// <summary>
    /// Exports the map in ASCII format with crawlers
    /// </summary>
    public string ExportToAscii(IEnumerable<(int X, int Y, string Direction)> crawlers)
    {
        var bounds = _map.GetBounds();
        if (bounds == null)
            return "Map is empty";

        var (minX, minY, maxX, maxY) = bounds.Value;
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        var sb = new StringBuilder();
        sb.AppendLine($"Map ({width}x{height}) - Exit: {(_map.ExitFound ? $"({_map.ExitPosition!.Value.X},{_map.ExitPosition!.Value.Y})" : "Not found")}");
        sb.AppendLine();

        var crawlerDict = new Dictionary<(int, int), string>();
        foreach (var c in crawlers)
        {
            crawlerDict[(c.X, c.Y)] = c.Direction;
        }

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (crawlerDict.TryGetValue((x, y), out var direction))
                {
                    sb.Append(DirectionToChar(direction));
                }
                else if (_map.ExitPosition.HasValue && _map.ExitPosition.Value == (x, y))
                {
                    sb.Append('E');
                }
                else
                {
                    var knowledge = _map.Get(x, y);
                    sb.Append(TileKnowledgeToChar(knowledge));
                }
            }
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("Legend: # = Wall, . = Room, / = Door, ? = Unknown, X = Outside, E = Exit");
        sb.AppendLine("        ^ = North, > = East, v = South, < = West");

        return sb.ToString();
    }

    /// <summary>
    /// Exports the map without crawlers
    /// </summary>
    public string ExportToAscii()
    {
        return ExportToAscii(Enumerable.Empty<(int X, int Y, string Direction)>());
    }

    /// <summary>
    /// Generates a summary of map statistics
    /// </summary>
    public string GetStatistics()
    {
        var bounds = _map.GetBounds();
        if (bounds == null)
            return "No data";

        var tiles = _map.GetKnownTiles();
        var wallCount = tiles.Count(kvp => kvp.Value == TileKnowledge.Wall);
        var roomCount = tiles.Count(kvp => kvp.Value == TileKnowledge.Room);
        var doorCount = tiles.Count(kvp => kvp.Value == TileKnowledge.Door);
        var outsideCount = tiles.Count(kvp => kvp.Value == TileKnowledge.Outside);
        var frontierCount = _map.GetFrontiers().Count;

        var sb = new StringBuilder();
        sb.AppendLine("=== Map Statistics ===");
        sb.AppendLine($"Known tiles: {_map.KnownCount}");
        sb.AppendLine($"  Walls: {wallCount}");
        sb.AppendLine($"  Rooms: {roomCount}");
        sb.AppendLine($"  Doors: {doorCount}");
        sb.AppendLine($"  Outside: {outsideCount}");
        sb.AppendLine($"Frontiers: {frontierCount}");
        sb.AppendLine($"Exit found: {(_map.ExitFound ? $"Yes at ({_map.ExitPosition!.Value.X},{_map.ExitPosition!.Value.Y})" : "No")}");

        return sb.ToString();
    }

    private static char TileKnowledgeToChar(TileKnowledge knowledge)
    {
        return knowledge switch
        {
            TileKnowledge.Wall => '#',
            TileKnowledge.Room => ' ',
            TileKnowledge.Door => '/',
            TileKnowledge.Outside => 'X',
            TileKnowledge.Unknown => '?',
            _ => '.'
        };
    }

    private static char DirectionToChar(string direction)
    {
        var normalized = direction?.Trim().ToLowerInvariant() ?? "";
        
        return normalized switch
        {
            "north" => '^',
            "south" => 'v',
            "east" => '>',
            "west" => '<',
            _ => '@'
        };
    }

    /// <summary>
    /// Displays the map directly in the console with colors for crawlers
    /// </summary>
    public void PrintMapWithColors(IList<(int X, int Y, string Direction)> crawlers, ConsoleColor[] crawlerColors)
    {
        var bounds = _map.GetBounds();
        if (bounds == null)
        {
            Console.WriteLine("Map is empty");
            return;
        }

        var (minX, minY, maxX, maxY) = bounds.Value;
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;

        Console.WriteLine($"Map ({width}x{height}) - Exit: {(_map.ExitFound ? $"({_map.ExitPosition!.Value.X},{_map.ExitPosition!.Value.Y})" : "Not found")}");
        Console.WriteLine();

        var crawlerAtPos = new Dictionary<(int, int), int>();
        for (int i = 0; i < crawlers.Count; i++)
        {
             crawlerAtPos[(crawlers[i].X, crawlers[i].Y)] = i; 
        }

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (crawlerAtPos.TryGetValue((x, y), out int crawlerIndex))
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = crawlerColors[crawlerIndex % crawlerColors.Length];
                    Console.Write(DirectionToChar(crawlers[crawlerIndex].Direction));
                    Console.ForegroundColor = originalColor;
                }
                else if (_map.ExitPosition.HasValue && _map.ExitPosition.Value == (x, y))
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write('E');
                    Console.ForegroundColor = originalColor;
                }
                else
                {
                    var knowledge = _map.Get(x, y);
                    char c = TileKnowledgeToChar(knowledge);
                    
                    if (c == '#') // Wall
                    {
                        var originalColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(c);
                        Console.ForegroundColor = originalColor;
                    }
                    else if (c == '/') // Door
                    {
                         var originalColor = Console.ForegroundColor;
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.Write(c);
                         Console.ForegroundColor = originalColor;
                    }
                    else
                    {
                        Console.Write(c);
                    }
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("Legend: # = Wall, . = Room, / = Door, ? = Unknown, X = Outside, E = Exit");
        Console.WriteLine("        ^ = North, > = East, v = South, < = West");
    }
}
