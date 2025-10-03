using System.Diagnostics.CodeAnalysis;

namespace Labyrinth.Items
{
    /// <summary>
    /// Inventory of collectable items for rooms and players.
    /// </summary>
    /// <param name="item">Optional initial item in the inventory.</param>
    public abstract class Inventory(ICollectable? item = null)
    {
        /// <summary>
        /// True if the room has an item, false otherwise.
        /// </summary>
        [MemberNotNullWhen(true, nameof(_item))]
        public bool HasItem => _item != null;

        /// <summary>
        /// Gets the type of the item in the room.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the room has no item (check with <see cref="HasItem"/>).</exception>
        public Type ItemType => (_item ?? throw new InvalidOperationException("No item in the room")).GetType();

        /// <summary>
        /// Places an item in the inventory, removing it from another one.
        /// </summary>
        /// <param name="from">The inventory from which the item is taken. The item is removed from this inventory.</param>
        /// <exception cref="InvalidOperationException">Thrown if the room already contains an item (check with <see cref="HasItem"/>).</exception>
        [MemberNotNull(nameof(_item))]
        public void MoveItemFrom(Inventory from)
        {
            if (HasItem)
            {
                throw new InvalidOperationException("Room already has an item.");
            }
            if (!from.HasItem)
            {
                throw new InvalidOperationException("No item to take from the source inventory");
            }
            _item = from._item;
            from._item = null;
        }

        protected ICollectable? _item = item;
    }
}
