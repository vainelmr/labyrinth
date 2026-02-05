using Labyrinth.Items;

namespace LabyrinthTest.Items;

public class InventoryAsyncTest
{
    [Test]
    public async Task GetItemTypesAsync_EmptyInventory_ReturnsEmptyList()
    {
        // Arrange
        var inventory = new MyInventory();

        // Act
        var types = await inventory.GetItemTypesAsync();

        // Assert
        Assert.That(types.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetItemTypesAsync_WithItem_ReturnsItemType()
    {
        // Arrange
        var key = new Key();
        var inventory = new MyInventory(key);

        // Act
        var types = await inventory.GetItemTypesAsync();

        // Assert
        Assert.That(types.Count, Is.EqualTo(1));
        Assert.That(types[0], Is.EqualTo(typeof(Key)));
    }

    [Test]
    public async Task TryMoveItemsFrom_ValidMove_ReturnsTrue()
    {
        // Arrange
        var source = new MyInventory(new Key());
        var destination = new MyInventory();

        // Act
        var result = await destination.TryMoveItemsFrom(source, new[] { true });

        // Assert
        Assert.That(result, Is.True);
        Assert.That(destination.Items.Count, Is.EqualTo(1));
        Assert.That(source.Items.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ConcurrentAccess_MultipleReads_Succeeds()
    {
        // Arrange
        var inventory = new MyInventory(new Key());
        var tasks = new List<Task<IReadOnlyList<Type>>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(inventory.GetItemTypesAsync());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.That(results.Length, Is.EqualTo(10));
        foreach (var types in results)
        {
            Assert.That(types.Count, Is.EqualTo(1));
            Assert.That(types[0], Is.EqualTo(typeof(Key)));
        }
    }
}


