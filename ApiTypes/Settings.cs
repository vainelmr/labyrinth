using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Settings used to configure the crawler and the labyrinth (first crawler only).
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets the seed value used to initialize the random number generator used for labyrinth generation.
        /// </summary>
        /// <remarks>
        /// Must be identical for all players (i.e. app keys). 
        /// Produce repeatable sequences of random numbers to generate a determinitic labyrinth.
        /// Ignored if <code>corridor-walls</code>, <code>key-rooms</code> and <code>wall-doors</code> must be defined
        /// </remarks>
        [JsonPropertyName("random-seed")]
        public int? RandomSeed { get; set; }

        /// <summary>
        /// Gets or sets the labyrinth walls on specific tiles.
        /// </summary>
        /// <remarks>
        /// Must be identical for all players (i.e. app. keys). The number of integers defines the number of levels in the labyrinth.
        /// Each integer must be even and less than the number of tiles in the corridors for that level (level 1 &lt; 20, level 2 &lt; 36, ...).
        /// Fields <code>key-rooms</code> and <code>wall-doors</code> must be defined.
        /// </remarks>
        [JsonPropertyName("corridor-walls")]
        public int[]? CorridorWalls { get; set; }

        /// <summary>
        /// Gets or sets the collection of wall door positions for the structure.
        /// </summary>
        /// <remarks>
        /// Must be identical for all players (i.e. app. keys). 
        /// Each array sets the positions of the 2 doors of the corresponding level (1 door if both values are identical).
        /// The integers must be even and less than the number of tiles in the walls for that level (level 1 &lt; 28, level 2 &lt; 44, ...).
        /// Fields <code>corridor-walls</code> and <code>key-rooms</code> must be defined.
        /// </remarks>

        [JsonPropertyName("wall-doors")]
        public int[][]? WallDoors { get; set; }

        /// <summary>
        /// Gets or sets the collection of key rooms, where each inner array represents a set of room positions
        /// to put a key in.
        /// </summary>
        /// <remarks>
        /// Must be identical for all players (i.e. app. keys).
        /// Each array sets the positions of the 2 key rooms of the corresponding level (1 key room if both values are identical).
        /// The integers must be odd and less than the number of tiles in the corridors for that level (level 1 &lt; 20, level 2 &lt; 36, ...).
        /// Fields <code>corridor-walls</code> and <code>wall-doors</code> must be defined.
        /// </remarks>
        [JsonPropertyName("key-rooms")]
        public int[][]? KeyRooms { get; set; }

        private HashCode HashCodeAdd<T>(HashCode hash, T val)
        {
            hash.Add(val);
            return hash;
        }
        private void AggregateArrays<T>(HashCode hash, T[][]? arrays) =>
            arrays?.SelectMany(arr => arr).Aggregate(hash, HashCodeAdd);

        /// <summary>
        /// Same hashcode for settings containing same data.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code that represents the current object.</returns>
        public override int GetHashCode()
        {
            HashCode hash = new HashCode();

            hash.Add(RandomSeed ?? 0);
            CorridorWalls?.Aggregate(hash, HashCodeAdd);
            AggregateArrays(hash, WallDoors);
            AggregateArrays(hash, KeyRooms);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Settings instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current Settings instance.</param>
        /// <returns>true if the specified object is a Settings instance and has the same hash code as the current instance;
        /// otherwise, false.</returns>
        public override bool Equals(object? obj) => 
            obj is Settings other &&
            GetHashCode() == other.GetHashCode();
    }
}
