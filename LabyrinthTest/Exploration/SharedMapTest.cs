using Labyrinth.Exploration;

namespace LabyrinthTest.Exploration;

public class SharedMapTest
{
    [Test]
    public void Get_UnknownPosition_ReturnsUnknown()
    {
        // Arrange
        var map = new SharedMap();

        // Act
        var result = map.Get(5, 10);

        // Assert
        Assert.That(result, Is.EqualTo(TileKnowledge.Unknown));
    }

    [Test]
    public void Update_NewPosition_StoresTileKnowledge()
    {
        // Arrange
        var map = new SharedMap();

        // Act
        map.Update(0, 0, TileKnowledge.Room);

        // Assert
        Assert.That(map.Get(0, 0), Is.EqualTo(TileKnowledge.Room));
        Assert.That(map.KnownCount, Is.EqualTo(1));
    }

    [Test]
    public void Update_ExistingPosition_DoesNotOverwrite()
    {
        // Arrange
        var map = new SharedMap();
        map.Update(0, 0, TileKnowledge.Wall);

        // Act
        map.Update(0, 0, TileKnowledge.Room);

        // Assert
        Assert.That(map.Get(0, 0), Is.EqualTo(TileKnowledge.Wall));
    }

    [Test]
    public void Update_RoomTile_AddsNeighborsToFrontiers()
    {
        // Arrange
        var map = new SharedMap();

        // Act
        map.Update(5, 5, TileKnowledge.Room);

        // Assert
        var frontiers = map.GetFrontiers();
        Assert.That(frontiers, Does.Contain((5, 4))); // North
        Assert.That(frontiers, Does.Contain((6, 5))); // East
        Assert.That(frontiers, Does.Contain((5, 6))); // South
        Assert.That(frontiers, Does.Contain((4, 5))); // West
        Assert.That(frontiers.Count, Is.EqualTo(4));
    }

    [Test]
    public void Update_DoorTile_AddsNeighborsToFrontiers()
    {
        // Arrange
        var map = new SharedMap();

        // Act
        map.Update(10, 10, TileKnowledge.Door);

        // Assert
        var frontiers = map.GetFrontiers();
        Assert.That(frontiers.Count, Is.EqualTo(4));
        Assert.That(map.IsFrontier(10, 9), Is.True);
        Assert.That(map.IsFrontier(11, 10), Is.True);
        Assert.That(map.IsFrontier(10, 11), Is.True);
        Assert.That(map.IsFrontier(9, 10), Is.True);
    }

    [Test]
    public void Update_WallTile_DoesNotAddNeighborsToFrontiers()
    {
        // Arrange
        var map = new SharedMap();

        // Act
        map.Update(0, 0, TileKnowledge.Wall);

        // Assert
        var frontiers = map.GetFrontiers();
        Assert.That(frontiers, Is.Empty);
    }

    [Test]
    public void Update_FrontierCell_RemovesItFromFrontiers()
    {
        // Arrange
        var map = new SharedMap();
        map.Update(0, 0, TileKnowledge.Room);

        // Act
        map.Update(0, 1, TileKnowledge.Wall);

        // Assert
        Assert.That(map.IsFrontier(0, 1), Is.False);
        Assert.That(map.IsKnown(0, 1), Is.True);
    }

    [Test]
    public void IsKnown_UnexploredPosition_ReturnsFalse()
    {
        // Arrange
        var map = new SharedMap();

        // Act & Assert
        Assert.That(map.IsKnown(100, 100), Is.False);
    }

    [Test]
    public void IsKnown_ExploredPosition_ReturnsTrue()
    {
        // Arrange
        var map = new SharedMap();
        map.Update(5, 5, TileKnowledge.Room);

        // Act & Assert
        Assert.That(map.IsKnown(5, 5), Is.True);
    }

    [Test]
    public void GetKnownTiles_ReturnsAllStoredTiles()
    {
        // Arrange
        var map = new SharedMap();
        map.Update(0, 0, TileKnowledge.Room);
        map.Update(1, 0, TileKnowledge.Wall);
        map.Update(0, 1, TileKnowledge.Door);

        // Act
        var known = map.GetKnownTiles();

        // Assert
        Assert.That(known.Count, Is.EqualTo(3));
        Assert.That(known[(0, 0)], Is.EqualTo(TileKnowledge.Room));
        Assert.That(known[(1, 0)], Is.EqualTo(TileKnowledge.Wall));
        Assert.That(known[(0, 1)], Is.EqualTo(TileKnowledge.Door));
    }

    [Test]
    public void GetBounds_EmptyMap_ReturnsNull()
    {
        // Arrange
        var map = new SharedMap();

        // Act
        var bounds = map.GetBounds();

        // Assert
        Assert.That(bounds, Is.Null);
    }

    [Test]
    public void GetBounds_WithTiles_ReturnsCorrectBounds()
    {
        // Arrange
        var map = new SharedMap();
        map.Update(-5, -3, TileKnowledge.Room);
        map.Update(10, 8, TileKnowledge.Room);
        map.Update(0, 0, TileKnowledge.Wall);

        // Act
        var bounds = map.GetBounds();

        // Assert
        Assert.That(bounds, Is.Not.Null);
        Assert.That(bounds.Value.MinX, Is.EqualTo(-5));
        Assert.That(bounds.Value.MinY, Is.EqualTo(-3));
        Assert.That(bounds.Value.MaxX, Is.EqualTo(10));
        Assert.That(bounds.Value.MaxY, Is.EqualTo(8));
    }

    [Test]
    public void Clear_RemovesAllData()
    {
        // Arrange
        var map = new SharedMap();
        map.Update(0, 0, TileKnowledge.Room);
        map.Update(1, 1, TileKnowledge.Wall);

        // Act
        map.Clear();

        // Assert
        Assert.That(map.KnownCount, Is.EqualTo(0));
        Assert.That(map.GetFrontiers(), Is.Empty);
        Assert.That(map.Get(0, 0), Is.EqualTo(TileKnowledge.Unknown));
    }

    [Test]
    public void ThreadSafety_ConcurrentUpdates_HandlesCorrectly()
    {
        // Arrange
        var map = new SharedMap();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            int x = i;
            tasks.Add(Task.Run(() => map.Update(x, 0, TileKnowledge.Room)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.That(map.KnownCount, Is.EqualTo(100));
        for (int i = 0; i < 100; i++)
        {
            Assert.That(map.Get(i, 0), Is.EqualTo(TileKnowledge.Room));
        }
    }

    [Test]
    public void ThreadSafety_ConcurrentReads_DoNotThrow()
    {
        // Arrange
        var map = new SharedMap();
        map.Update(0, 0, TileKnowledge.Room);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var _ = map.Get(0, 0);
                var __ = map.GetFrontiers();
                var ___ = map.IsKnown(0, 0);
            }));
        }

        // Assert
        Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()));
    }

    [Test]
    public void ComplexScenario_ExploringLabyrinth()
    {
        // Arrange
        var map = new SharedMap();

        // Act
        map.Update(0, 0, TileKnowledge.Room);  // Start position
        map.Update(0, -1, TileKnowledge.Wall); // North wall
        map.Update(1, 0, TileKnowledge.Room);  // Move east
        map.Update(1, -1, TileKnowledge.Wall); // North wall
        map.Update(2, 0, TileKnowledge.Door);  // Door to east
        map.Update(2, -1, TileKnowledge.Wall); // North wall

        // Assert
        Assert.That(map.KnownCount, Is.EqualTo(6));
        
        var frontiers = map.GetFrontiers();
        Assert.That(frontiers, Does.Contain((0, 1)));   // South of start
        Assert.That(frontiers, Does.Contain((-1, 0)));  // West of start
        Assert.That(frontiers, Does.Contain((3, 0)));   // East of door
        
        Assert.That(frontiers, Does.Not.Contain((0, -2)));
    }
}
