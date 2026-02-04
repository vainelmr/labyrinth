
using Labyrinth.ApiClient;
using TrainingServer.Services; 

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});


builder.Services.AddSingleton<TrainingServer.Services.MazeRepository>();
builder.Services.AddSingleton<TrainingServer.Services.CrawlerManager>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();


app.MapGet("/", () => "TrainingServer is running");

//app.MapGet("/", () => "Hello World!");

//Ajouter POST /crawlers
app.MapPost("/crawlers", (Guid appKey, TrainingServer.Services.CrawlerManager mgr) =>
{
    var (id, crawler, bag) = mgr.Create();

    var dto = new ApiTypes.Crawler
    {
        Id = id,
        X = crawler.X,
        Y = crawler.Y,
        Direction = ApiTypes.Direction.North,
        Walking = false,
        FacingTile = ApiTypes.TileType.Room,
        Bag = bag.GetApiInventoryItems(),
        Items = Array.Empty<ApiTypes.InventoryItem>()
    };

    return Results.Ok(dto);
});

// PATCH /crawlers/{id}?appKey=...
app.MapMethods("/crawlers/{id:guid}", new[] { "PATCH" },
async (Guid id, Guid appKey, ApiTypes.Crawler incoming,
       TrainingServer.Services.CrawlerManager mgr) =>
{
    var updated = await mgr.PatchCrawlerAsync(id, incoming);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

// DELETE /crawlers/{id}?appKey=...
app.MapDelete("/crawlers/{id:guid}", (Guid id, Guid appKey, TrainingServer.Services.CrawlerManager mgr) =>
{
    return mgr.Delete(id) ? Results.Ok() : Results.NotFound();
});

// PUT /crawlers/{id}/bag?appKey=...   (source=bag -> destination=items)
app.MapPut("/crawlers/{id:guid}/bag",
async (Guid id, Guid appKey, ApiTypes.InventoryItem[] moves,
       TrainingServer.Services.CrawlerManager mgr) =>
{
    try
    {
        var movesRequired = moves.Select(m => m.MoveRequired ?? false).ToList();
        var result = await mgr.MoveItemsAsync(id, "bag", movesRequired);

        if (result is null) return Results.NotFound();

        var (ok, remaining) = result.Value;
        return ok ? Results.Ok(remaining) : Results.Conflict(remaining);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// PUT /crawlers/{id}/items?appKey=... (source=items -> destination=bag)
app.MapPut("/crawlers/{id:guid}/items",
async (Guid id, Guid appKey, ApiTypes.InventoryItem[] moves,
       TrainingServer.Services.CrawlerManager mgr) =>
{
    try
    {
        var movesRequired = moves.Select(m => m.MoveRequired ?? false).ToList();
        var result = await mgr.MoveItemsAsync(id, "items", movesRequired);

        if (result is null) return Results.NotFound();

        var (ok, remaining) = result.Value;
        return ok ? Results.Ok(remaining) : Results.Conflict(remaining);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});




app.Run();


