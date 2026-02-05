using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;
using NUnit.Framework;

namespace LabyrinthTest.Crawl;

[TestFixture]
public class CrawlerRulesMinimalTest
{
    private static Labyrinth.Labyrinth NewLabyrinth(string ascii) =>
        new Labyrinth.Labyrinth(new AsciiParser(ascii));

    private static ICrawler NewCrawler(string ascii) =>
        NewLabyrinth(ascii).NewCrawler();

    [Test]
    public async Task Wall_ahead_TryMove_returns_failure_and_position_does_not_change()
    {
        var crawler = NewCrawler("""
            +--+
            | x|
            +--+
            """);

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryMoveAsync(new MyInventory());

        Assert.That(res, Is.InstanceOf<MoveResult.Failure>());
        Assert.That(crawler.X, Is.EqualTo(x0));
        Assert.That(crawler.Y, Is.EqualTo(y0));
    }

    [Test]
    public async Task Room_ahead_TryMove_returns_success_with_inventory_and_position_changes()
    {
        var crawler = NewCrawler("""
            +/-+
            |  |
            |xk|
            +--+
            """);

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryMoveAsync(new MyInventory());

        Assert.That(res, Is.InstanceOf<MoveResult.Success>());
        var success = (MoveResult.Success)res;
        Assert.That(success.TileInventory, Is.Not.Null);
        Assert.That(crawler.X != x0 || crawler.Y != y0, Is.True);
    }

    [Test]
    public async Task Door_ahead_without_key_TryMove_fails_and_position_does_not_change()
    {
        var crawler = NewCrawler("""
            +--+
            |xk|
            +-/|
            """);

        // se mettre face à la porte
        crawler.Direction.TurnRight(); // face à 'k'
        var emptyBag = new MyInventory();
        var moveResult = await crawler.TryMoveAsync(emptyBag); // avance dans la room de clé (mais on NE transfert rien dans le bag)

        crawler.Direction.TurnRight(); // face à la porte

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryMoveAsync(emptyBag);

        Assert.That(res, Is.InstanceOf<MoveResult.Failure>());
        Assert.That(crawler.X, Is.EqualTo(x0));
        Assert.That(crawler.Y, Is.EqualTo(y0));
    }

    [Test]
    public async Task Door_ahead_with_key_TryMove_succeeds_and_position_changes()
    {
        var lab = NewLabyrinth("""
            +--+
            |xk|
            +-/|
            """);

        var crawler = lab.NewCrawler();
        var bag = new MyInventory();

        crawler.Direction.TurnRight(); // face à la room avec la clé
        var moveResult = await crawler.TryMoveAsync(bag); // rentre dans la room avec la clé

        // transférer la clé dans le bag
        Assert.That(moveResult, Is.InstanceOf<MoveResult.Success>());
        var success = (MoveResult.Success)moveResult;
        Assert.That(success.TileInventory, Is.Not.Null);
        var itemTypes = await success.TileInventory!.GetItemTypesAsync();
        await bag.TryMoveItemsFrom(success.TileInventory!, itemTypes.Select(_ => true).ToList());
        Assert.That(bag.HasItems, Is.True);

        crawler.Direction.TurnRight(); // face à la porte

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryMoveAsync(bag);

        Assert.That(res, Is.InstanceOf<MoveResult.Success>());
        Assert.That(crawler.X != x0 || crawler.Y != y0, Is.True);
    }
}