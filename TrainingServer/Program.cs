var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TrainingServer.Services.MazeRepository>();
builder.Services.AddSingleton<TrainingServer.Services.CrawlerManager>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "Hello World!");

//Ajouter POST /crawlers
app.MapPost("/crawlers", (HttpRequest req, TrainingServer.Services.CrawlerManager mgr) =>
{
    if (!req.Query.ContainsKey("appKey"))
        return Results.BadRequest("Missing appKey");

    var (id, crawler, bag) = mgr.Create();

    // On renvoie un Dto.Crawler (ce que ton client attend au POST)
    var dto = new ApiTypes.Crawler
    {
        Id = id,
        X = crawler.X,
        Y = crawler.Y,
        Dir = ApiTypes.Direction.North, // valeur par défaut (sera corrigée au PATCH)
        Walking = false,
        FacingTile = ApiTypes.TileType.Room,
        Bag = Array.Empty<ApiTypes.InventoryItem>(),
        Items = Array.Empty<ApiTypes.InventoryItem>()
    };

    return Results.Ok(dto);
});


app.Run();


