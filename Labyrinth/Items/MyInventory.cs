namespace Labyrinth.Items
{
    /// <summary>
    /// Inventory class that exposes the item it contains.
    /// </summary>
    /// <param name="item">Optional initial item in the inventory.</param>
    public class MyInventory(ICollectable? item = null) : LocalInventory(item)
    {
        /// <summary>
        /// Items in the inventory.
        /// </summary>
        public IReadOnlyList<ICollectable> Items => _items;
    }
}
