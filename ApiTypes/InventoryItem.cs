using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Inventory item, including its type and an optional move requirement.
    /// </summary>
    public class InventoryItem
    {
        /// <summary>
        /// The type of the item.
        /// </summary>
        public ItemType Type { get; init; }

        /// <summary>
        /// Indicates whether a move is required for the item. 
        /// Set to <see langword="true"/> if a move is required; <see langword="false"/> 
        /// or <see langword="null"/> or missing if not.
        /// </summary>
        [JsonPropertyName("move-required")]
        public bool? MoveRequired { get; set; }
    }
}
