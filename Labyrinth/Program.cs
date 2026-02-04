using Labyrinth;
using Labyrinth.ApiClient;
using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using Labyrinth.Sys;
using Dto = ApiTypes;
using System.Text.Json;

const int OffsetY = 2;

char DirToChar(Direction dir) =>
    "^<v>"[dir.DeltaX * dir.DeltaX + dir.DeltaX + dir.DeltaY + 1];

var TileToChar = new Dictionary<Type, char>
{
    [typeof(Room)] = ' ',
    [typeof(Wall)] = '#',
    [typeof(Door)] = '/'
};

void DrawExplorer(object? sender, CrawlingEventArgs e)
{
    var explorer = (RandExplorer)sender!;
    var crawler = explorer.Crawler;

    var facingTileType = explorer.LastFacingTileType ?? typeof(Outside);

    if (facingTileType != typeof(Outside))
    {
        Console.SetCursorPosition(
            e.X + e.Direction.DeltaX,
            e.Y + e.Direction.DeltaY + OffsetY
        );

        if (TileToChar.TryGetValue(facingTileType, out var tileChar))
            Console.Write(tileChar);
        else
            Console.Write('?');
    }

    Console.SetCursorPosition(e.X, e.Y + OffsetY);
    Console.Write(DirToChar(e.Direction));

    Console.SetCursorPosition(0, 0);
    if (crawler is ClientCrawler cc)
    {
        Console.WriteLine($"Bag : {cc.Bag.ItemTypes.Count()} item(s)");
    }

    Thread.Sleep(100);
}

Labyrinth.Labyrinth labyrinth;
ICrawler crawler;
Inventory? bag = null;
ContestSession? contest = null;

if (args.Length < 2)
{
    Console.WriteLine(
        "Commande line usage : https://apiserver.example appKeyGuid [settings.json]"
    );

    labyrinth = new Labyrinth.Labyrinth(new AsciiParser("""
+---------------+
|      /   k    |
| +-----------+ |
| |  /        | |
| | +-----   +| |
| | | x   k   | |
| | | +---+   | |
| | | |   |   | |
| | +------  +| |
| |        /    |
| +-----------+ 
|      k        
+------
"""
));

    crawler = labyrinth.NewCrawler();
}
else
{
    Dto.Settings? settings = null;

    if (args.Length > 2)
    {
        settings = JsonSerializer.Deserialize<Dto.Settings>(File.ReadAllText(args[2]));
    }

    contest = await ContestSession.Open(new Uri(args[0]), Guid.Parse(args[1]), settings);
    labyrinth = new(contest.Builder);
    crawler = await contest.NewCrawler();
    bag = contest.Bags.First();
}

var prevX = crawler.X;
var prevY = crawler.Y;

var explorerInstance = new RandExplorer(
    crawler,
    new BasicEnumRandomizer<RandExplorer.Actions>()
);

explorerInstance.DirectionChanged += DrawExplorer;
explorerInstance.PositionChanged += (s, e) =>
{
    Console.SetCursorPosition(prevX, prevY);
    Console.Write(' ');

    DrawExplorer(s, e);

    (prevX, prevY) = (e.X, e.Y + OffsetY);
};

Console.Clear();
Console.SetCursorPosition(0, OffsetY);
Console.WriteLine(labyrinth);

await explorerInstance.GetOut(3000, bag);

if (contest is not null)
{
    await contest.Close();
}
