using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Inventory item types.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ItemType
    {
        /// <summary>
        /// Key item type.
        /// </summary>
        Key
    }
}
