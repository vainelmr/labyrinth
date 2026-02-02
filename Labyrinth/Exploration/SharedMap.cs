using System.Collections.Concurrent;

namespace Labyrinth.Exploration;

public enum TileKnowledge { Unknown, Wall, Room, Door, Outside }

public sealed class SharedMap
{
    private readonly ConcurrentDictionary<(int x,int y), TileKnowledge> _tiles = new();

    public TileKnowledge Get(int x, int y)
        => _tiles.TryGetValue((x,y), out var v) ? v : TileKnowledge.Unknown;

    public void Update(int x, int y, TileKnowledge value)
    {
        _tiles.AddOrUpdate((x,y), value, (_, old) =>
            old == TileKnowledge.Unknown ? value : old);
    }

    public int KnownCount => _tiles.Count;
}
