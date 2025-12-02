using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth.Build
{
    /// <summary>
    /// Manage the creation of doors and key rooms ensuring each door has a corresponding key room.
    /// </summary>
    public sealed class Keymaster : IDisposable
    {
        /// <summary>
        /// Ensure all created doors have a corresponding key room and vice versa.
        /// </summary>
        /// <exception cref="InvalidOperationException">Some keys are missing or are not placed.</exception>
        public void Dispose()
        {
            if (unplacedKey.HasItems || emptyKeyRoom is not null)
            {
                throw new InvalidOperationException("Unmatched key/door creation");
            }
        }

        /// <summary>
        /// Create a new door and place its key in a previously created empty key room (if any).
        /// </summary>
        /// <returns>Created door</returns>
        /// <exception cref="NotSupportedException">Multiple doors before key placement</exception>
        public Door NewDoor()
        {
            if (unplacedKey.HasItems)
            {
                throw new NotSupportedException("Unable to handle multiple doors before key placement");
            }
            var door = new Door();

            door.LockAndTakeKey(unplacedKey);
            PlaceKey();
            return door;
        }

        /// <summary>
        /// Create a new room with key and place the key if a door was previously created.
        /// </summary>
        /// <returns>Created key room</returns>
        /// <exception cref="NotSupportedException">Multiple keyss before key placement</exception>
        public Room NewKeyRoom()
        {
            if (emptyKeyRoom is not null)
            {
                throw new NotSupportedException("Unable to handle multiple keys before door creation");
            }
            var room = emptyKeyRoom = new Room();
            PlaceKey();
            return room;
        }

        private void PlaceKey()
        {
            if (unplacedKey.HasItems && emptyKeyRoom is not null)
            {
                emptyKeyRoom.Pass().MoveItemFrom(unplacedKey);
                emptyKeyRoom = null;
            }
        }

        private readonly MyInventory unplacedKey = new();
        private Room? emptyKeyRoom = null;
    }
}