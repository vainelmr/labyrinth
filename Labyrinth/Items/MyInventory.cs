namespace Labyrinth.Items
{
    /// <summary>
    /// Inventory class that exposes the item it contains.
    /// </summary>
    /// <param name="item">Optional initial item in the inventory.</param>
    public class MyInventory(ICollectable? item = null) : Inventory(item)
    {
        /// <summary>
        /// Item in the inventory, or null if empty.
        /// </summary>
        public ICollectable? Item => _item;
    }
}
