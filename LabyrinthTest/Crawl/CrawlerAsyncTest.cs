using Labyrinth.Crawl;
using Labyrinth.Build;

namespace LabyrinthTest.Crawl;

public class CrawlerAsyncTest
{
    [Test]
    public async Task GetFrontTileTypeAsync_FacingWall_ReturnsWallType()
    {
        // Arrange
        var ascii = "+---+\n| x |\n+---+";
        var labyrinth = new Labyrinth.Labyrinth(new AsciiParser(ascii));
        var crawler = labyrinth.NewCrawler();

        // Act
        var tileType = await crawler.GetFrontTileTypeAsync();

        // Assert
        Assert.That(tileType, Is.EqualTo(typeof(Labyrinth.Tiles.Wall)));
    }

    [Test]
    public async Task IsFacingExitAsync_FacingWall_ReturnsFalse()
    {
        // Arrange
        var ascii = "+---+\n| x |\n+---+";
        var labyrinth = new Labyrinth.Labyrinth(new AsciiParser(ascii));
        var crawler = labyrinth.NewCrawler();

        // Act
        var isFacingExit = await crawler.IsFacingExitAsync();

        // Assert
        Assert.That(isFacingExit, Is.False);
    }

    [Test]
    public async Task IsFacingExitAsync_FacingRoom_ReturnsFalse()
    {
        // Arrange
        var ascii = "+-----+\n| x   |\n+-----+";
        var labyrinth = new Labyrinth.Labyrinth(new AsciiParser(ascii));
        var crawler = labyrinth.NewCrawler();

        // Act
        var isFacingExit = await crawler.IsFacingExitAsync();

        // Assert
        Assert.That(isFacingExit, Is.False);
    }

    [Test]
    public async Task AsyncOperations_HaveDelay_TakesTime()
    {
        // Arrange
        var ascii = "+-----+\n| x   |\n+-----+";
        var labyrinth = new Labyrinth.Labyrinth(new AsciiParser(ascii));
        var crawler = labyrinth.NewCrawler();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await crawler.GetFrontTileTypeAsync();
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(70), 
            $"Operation should take at least 70ms, but took {stopwatch.ElapsedMilliseconds}ms");
    }
}


