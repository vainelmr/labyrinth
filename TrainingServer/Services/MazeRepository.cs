using Labyrinth.Build;

namespace TrainingServer.Services;

public sealed class MazeRepository
{
    private const string MazeAscii = """
+---------------+
|      /   k    |
| +-----------+ |
| |  /        | |
| | +-----   +| |
| | | x   k   | |
| | | +---+   | |
| | | |   |   | |
| | +------  +| |
| |        /    |
| +-----------+ |
|      k        |
+---------------+
""";


    public Labyrinth.Labyrinth CreateLabyrinth()
        => new(new AsciiParser(MazeAscii));
}
