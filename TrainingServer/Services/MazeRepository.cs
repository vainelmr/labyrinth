using Labyrinth.Build;

namespace TrainingServer.Services;

public sealed class MazeRepository
{
    private const string MazeAscii = """
+---------------+
|      /        |
| +-----------+ |
| |  /        | |
| | +-------+ | |
| | | x   k | | |
| | | +---+ | | |
| | | |   | | | |
| | +-------+ | |
| |        /  | |
| +-----------+ |
|      k   k    |
+---------------+
""";

    public Labyrinth.Labyrinth CreateLabyrinth()
        => new(new AsciiParser(MazeAscii));
}
