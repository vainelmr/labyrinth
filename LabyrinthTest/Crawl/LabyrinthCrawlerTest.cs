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

    private static void AssertThat(ICrawler test, int x, int y, Direction dir, Type facingTile)
    {
        using var all = Assert.EnterMultipleScope();

        Assert.That(test.X, Is.EqualTo(x));
        Assert.That(test.Y, Is.EqualTo(y));
        Assert.That(test.Direction, Is.EqualTo(dir));
        Assert.That(test.FacingTileType.Result, Is.EqualTo(facingTile));
    }

    #region Initialization
    [Test]
    public void InitWithCenteredX() =>
        AssertThat(
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
    public void InitWithMultipleXUsesLastOne() =>
        AssertThat(
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
    public void FacingNorthOnUpperTileReturnsOutside() =>
         AssertThat(
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
    public void FacingWestOnFarLeftTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            x |
            +-+
            """
        );
        test.Direction.TurnLeft();
        AssertThat(test,
            x: 0, y: 1,
            Direction.West,
            typeof(Outside)
        );
    }

    [Test]
    public void FacingEastOnFarRightTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            | x
            +-+
            """
        );
        test.Direction.TurnRight();
        AssertThat(test,
            x: 2, y: 1,
            Direction.East,
            typeof(Outside)
        );
    }

    [Test]
    public void FacingSouthOnBottomTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            | |
            +x+
            """
        );
        test.Direction.TurnLeft();
        test.Direction.TurnLeft();
        AssertThat(test,
            x: 1, y: 2,
            Direction.South,
            typeof(Outside)
        );
    }
    #endregion

    #region Moves
    [Test]
    public void TurnLeftFacesWestTile()
    {
        var test = NewCrawlerFor("""
            +---+
            |/xk|
            +---+
            """
        );
        test.Direction.TurnLeft();
        AssertThat(test,
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
        var inventory = await test.TryWalk(new MyInventory());

        Assert.That(inventory, Is.Not.Null);
        Assert.That(inventory.HasItems, Is.False);
        AssertThat(test,
            x: 1, y: 1,
            Direction.North,
            typeof(Door)
        );
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

        var inventory = await test.TryWalk(new MyInventory());

        Assert.That(inventory, Is.Not.Null);
        Assert.That(inventory.HasItems, Is.False);
        AssertThat(test,
            x: 2, y: 1,
            Direction.East,
            typeof(Wall)
        );
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
        var inventory = await test.TryWalk(new MyInventory());

        Assert.That(inventory, Is.Null);
        AssertThat(test,
            x: 1, y: 2,
            Direction.North,
            typeof(Door)
        );
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
        var inventory = await test.TryWalk(new MyInventory());

        Assert.That(inventory, Is.Null);
        AssertThat(test,
            x: 1, y: 0,
            Direction.North,
            typeof(Outside)
        );
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
        var inventory = await test.TryWalk(new MyInventory());

        using var all = Assert.EnterMultipleScope();

        Assert.That(inventory, Is.Not.Null);
        Assert.That(inventory?.HasItems, Is.True);
        Assert.That(inventory?.ItemTypes.First(), Is.EqualTo(typeof(Key)));
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
        var inventory = await test.TryWalk(new MyInventory());

        Assert.That(inventory, Is.Not.Null);
        Assert.That(await test.TryWalk(inventory), Is.Null);
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

        var inventory = await test.TryWalk(new MyInventory());

        test.Direction.TurnRight();

        var actual = await test.TryWalk(inventory!);

        using var all = Assert.EnterMultipleScope();

        Assert.That(actual, Is.Not.Null);
        Assert.That(test.X, Is.EqualTo(2));
        Assert.That(test.Y, Is.EqualTo(2));
        Assert.That(test.Direction, Is.EqualTo(Direction.South));
        Assert.That(test.FacingTileType.Result, Is.EqualTo(typeof(Outside)));
    }
    #endregion
}
