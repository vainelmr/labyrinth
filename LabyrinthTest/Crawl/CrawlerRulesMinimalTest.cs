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
    public async Task Wall_ahead_TryWalk_returns_null_and_position_does_not_change()
    {
        var crawler = NewCrawler("""
            +--+
            | x|
            +--+
            """);

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryWalk(new MyInventory());

        Assert.That(res, Is.Null);
        Assert.That(crawler.X, Is.EqualTo(x0));
        Assert.That(crawler.Y, Is.EqualTo(y0));
    }

    [Test]
    public async Task Room_ahead_TryWalk_returns_inventory_and_position_changes()
    {
        var crawler = NewCrawler("""
            +/-+
            |  |
            |xk|
            +--+
            """);

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryWalk(new MyInventory());

        Assert.That(res, Is.Not.Null);
        Assert.That(crawler.X != x0 || crawler.Y != y0, Is.True);
    }

    [Test]
    public async Task Door_ahead_without_key_TryWalk_fails_and_position_does_not_change()
    {
        var crawler = NewCrawler("""
            +--+
            |xk|
            +-/|
            """);

        // se mettre face à la porte (comme dans ton test existant)
        crawler.Direction.TurnRight(); // face à 'k'
        var emptyBag = new MyInventory();
        var inv = await crawler.TryWalk(emptyBag); // avance dans la room de clé (mais on NE transfert rien dans le bag)

        crawler.Direction.TurnRight(); // face à la porte

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryWalk(emptyBag);

        Assert.That(res, Is.Null);
        Assert.That(crawler.X, Is.EqualTo(x0));
        Assert.That(crawler.Y, Is.EqualTo(y0));
    }

    [Test]
    public async Task Door_ahead_with_key_TryWalk_succeeds_and_position_changes()
    {
        var lab = NewLabyrinth("""
            +--+
            |xk|
            +-/|
            """);

        var crawler = lab.NewCrawler();
        var bag = new MyInventory();

        crawler.Direction.TurnRight(); // face à la room avec la clé
        var roomInv = await crawler.TryWalk(bag); // rentre dans la room avec la clé

        // transférer la clé dans le bag
        Assert.That(roomInv, Is.Not.Null);
        await bag.TryMoveItemsFrom(roomInv!, roomInv!.ItemTypes.Select(_ => true).ToList());
        Assert.That(bag.HasItems, Is.True);

        crawler.Direction.TurnRight(); // face à la porte

        var x0 = crawler.X;
        var y0 = crawler.Y;

        var res = await crawler.TryWalk(bag);

        Assert.That(res, Is.Not.Null);
        Assert.That(crawler.X != x0 || crawler.Y != y0, Is.True);
    }
}
