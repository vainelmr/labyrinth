using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace LabyrinthTest.Crawl;

[TestFixture(Description = "Integration test for the crawler implementation in the labyrinth")]
public class LabyrinthCrawlerTest
{
    private static Labyrinth.Labyrinth NewLabyrinth(string ascii_map) =>
        new Labyrinth.Labyrinth(new AsciiParser(ascii_map));

    private static ICrawler NewCrawlerFor(string ascii_map) =>
        NewLabyrinth(ascii_map).NewCrawler();

    private static async Task AssertThatAsync(ICrawler test, int x, int y, Direction dir, Type facingTile)
    {
        using var all = Assert.EnterMultipleScope();
        Assert.That(test.X, Is.EqualTo(x));
        Assert.That(test.Y, Is.EqualTo(y));
        Assert.That(test.Direction, Is.EqualTo(dir));
        Assert.That(await test.GetFrontTileTypeAsync(), Is.EqualTo(facingTile));
    }

    #region Initialization
    [Test]
    public async Task InitWithCenteredX() =>
        await AssertThatAsync(
            NewCrawlerFor("""
                +--+
                | x|
                +--+
                """
            ),
            x: 2, y: 1,
            Direction.North,
            typeof(Wall)
        );

    [Test]
    public async Task InitWithMultipleXUsesLastOne() =>
        await AssertThatAsync(
            NewCrawlerFor("""
                +--+
                | x|
                |x |
                +--+
                """
            ),
            x: 1, y: 2,
            Direction.North,
            typeof(Room)
        );

    [Test]
    public void InitWithNoXThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() =>
            NewLabyrinth("""
                +--+
                |  |
                +--+
                """
            )
        );
    #endregion

    #region Labyrinth borders
    [Test]
    public async Task FacingNorthOnUpperTileReturnsOutside() =>
        await AssertThatAsync(
            NewCrawlerFor("""
                +x+
                | |
                +-+
                """
            ),
            x: 1, y: 0,
            Direction.North,
            typeof(Outside)
        );

    [Test]
    public async Task FacingWestOnFarLeftTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            x |
            +-+
            """
        );
        test.Direction.TurnLeft();
        await AssertThatAsync(test,
            x: 0, y: 1,
            Direction.West,
            typeof(Outside)
        );
    }

    [Test]
    public async Task FacingEastOnFarRightTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            | x
            +-+
            """
        );
        test.Direction.TurnRight();
        await AssertThatAsync(test,
            x: 2, y: 1,
            Direction.East,
            typeof(Outside)
        );
    }

    [Test]
    public async Task FacingSouthOnBottomTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            | |
            +x+
            """
        );
        test.Direction.TurnLeft();
        test.Direction.TurnLeft();
        await AssertThatAsync(test,
            x: 1, y: 2,
            Direction.South,
            typeof(Outside)
        );
    }
    #endregion

    #region Moves
    [Test]
    public async Task TurnLeftFacesWestTile()
    {
        var test = NewCrawlerFor("""
            +---+
            |/xk|
            +---+
            """
        );
        test.Direction.TurnLeft();
        await AssertThatAsync(test,
            x: 2, y: 1,
            Direction.West,
            typeof(Door)
        );
    }
    [Test]
    public async Task WalkReturnsInventoryAndChangesPositionAndFacingTile()
    {
        var test = NewCrawlerFor("""
            +/-+
            |  |
            |xk|
            +--+
            """
        );
        var result = await test.TryMoveAsync(new MyInventory());
        var inventory = result is MoveResult.Success s ? s.TileInventory : null;

        Assert.That(inventory, Is.Not.Null);
        Assert.That(inventory!.HasItems, Is.False);
        await AssertThatAsync(test, x: 1, y: 1, Direction.North, typeof(Door));
    }

    [Test]
    public async Task TurnAndWalkReturnsInventoryChangesPositionAndFacingTile()
    {
        var test = NewCrawlerFor("""
            +--+
            |x |
            +--+
            """
        );
        test.Direction.TurnRight();

        var result = await test.TryMoveAsync(new MyInventory());
        var inventory = result is MoveResult.Success succ ? succ.TileInventory : null;

        Assert.That(inventory, Is.Not.Null);
        Assert.That(inventory!.HasItems, Is.False);
        await AssertThatAsync(test, x: 2, y: 1, Direction.East, typeof(Wall));
    }

    [Test]
    public async Task WalkOnNonTraversableTileReturnsFalseAndDontMove()
    {
        var test = NewCrawlerFor("""
            +--+
            |/-+
            |xk|
            +--+
            """
        );
        var result = await test.TryMoveAsync(new MyInventory());

        Assert.That(result, Is.InstanceOf<MoveResult.Failure>());
        await AssertThatAsync(test, x: 1, y: 2, Direction.North, typeof(Door));
    }

    [Test]
    public async Task WalkOutsideReturnsFalseAndDontMove()
    {
        var test = NewCrawlerFor("""
            |x|
            | |
            +-+
            """
        );
        var result = await test.TryMoveAsync(new MyInventory());

        Assert.That(result, Is.InstanceOf<MoveResult.Failure>());
        await AssertThatAsync(test, x: 1, y: 0, Direction.North, typeof(Outside));
    }
    #endregion

    #region Items and doors
    [Test]
    public async Task WalkInARoomWithAnItem()
    {
        var test = NewCrawlerFor("""
        +---+
        |  k|
        |/ x|
        +---+
        """
        );
        var result = await test.TryMoveAsync(new MyInventory());
        var inventory = result is MoveResult.Success success ? success.TileInventory : null;

        using var all = Assert.EnterMultipleScope();
        Assert.That(inventory, Is.Not.Null);
        Assert.That(inventory?.HasItems, Is.True);
        Assert.That((await inventory!.GetItemTypesAsync()).First(), Is.EqualTo(typeof(Key)));
    }

    [Test]
    public async Task WalkUseAWrongKeyToOpenADoor()
    {
        var test = NewCrawlerFor("""
            +---+
            |/ k|
            |k  |
            |x /|
            +---+
            """);
        var r1 = await test.TryMoveAsync(new MyInventory());
        var inventory = r1 is MoveResult.Success s1 ? s1.TileInventory : null;

        Assert.That(inventory, Is.Not.Null);
        var r2 = await test.TryMoveAsync(inventory!);
        Assert.That(r2, Is.InstanceOf<MoveResult.Failure>());
        Assert.That(inventory.HasItems, Is.True);
    }

    [Test]
    public async Task WalkUseKeyToOpenADoorAndPass()
    {
        var laby = NewLabyrinth("""
                +--+
                |xk|
                +-/|
                """);
        var test = laby.NewCrawler();

        test.Direction.TurnRight();

        var r1 = await test.TryMoveAsync(new MyInventory());
        var inventory = r1 is MoveResult.Success r1s ? r1s.TileInventory : null;

        test.Direction.TurnRight();

        var r2 = await test.TryMoveAsync(inventory!);
        var actual = r2 is MoveResult.Success r2s ? r2s.TileInventory : null;

        using var all = Assert.EnterMultipleScope();
        Assert.That(actual, Is.Not.Null);
        Assert.That(test.X, Is.EqualTo(2));
        Assert.That(test.Y, Is.EqualTo(2));
        Assert.That(test.Direction, Is.EqualTo(Direction.South));
        Assert.That(await test.GetFrontTileTypeAsync(), Is.EqualTo(typeof(Outside)));
    }
    #endregion
}
