namespace Labyrinth.Items
{
    /// <summary>
    /// Inventory of collectable items for rooms and players.
    /// </summary>
    /// <param name="item">Optional initial item in the inventory.</param>
    public abstract class Inventory
    {
        protected Inventory(ICollectable? item = null)
        {
            if(item is not null)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// True if the room has an items, false otherwise.
        /// </summary>
        public bool HasItems => _items.Count>0;

        /// <summary>
        /// Gets the type of the item in the room.
        /// </summary>
        public IEnumerable<Type> ItemTypes => _items.Select(item => item.GetType());

        /// <summary>
        /// Attempts to move selected items from the specified source inventory to the current inventory.
        /// </summary>
        /// <remarks>The method does not modify items in the source inventory that are not selected. The
        /// operation may fail if the current inventory cannot accept the selected items due to capacity or other
        /// constraints.</remarks>
        /// <param name="source">The inventory from which items will be moved. Cannot be null.</param>
        /// <param name="movesRequired">A list of Boolean values indicating which items in the source inventory to move. Each element corresponds to
        /// an item; <see langword="true"/> selects the item for moving, <see langword="false"/> leaves it in the
        /// source.</param>
        /// <returns>true if all selected items were successfully moved; otherwise, false.</returns>
        public abstract Task<bool> TryMoveItemsFrom(
            Inventory source, 
            IList<bool> movesRequired
        );

        protected List<ICollectable> _items = new ();
    }
}
