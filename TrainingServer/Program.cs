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

app.MapGet("/", () => "Hello World!");

//Ajouter POST /crawlers
app.MapPost("/crawlers", (Guid appKey, TrainingServer.Services.CrawlerManager mgr) =>
{
    // appKey est maintenant un vrai paramètre swagger (query)
    var (id, crawler, bag) = mgr.Create();

    var dto = new ApiTypes.Crawler
    {
        Id = id,
        X = crawler.X,
        Y = crawler.Y,
        Dir = ApiTypes.Direction.North,
        Walking = false,
        FacingTile = ApiTypes.TileType.Room,
        Bag = Array.Empty<ApiTypes.InventoryItem>(),
        Items = Array.Empty<ApiTypes.InventoryItem>()
    };

    return Results.Ok(dto);
});



app.Run();


