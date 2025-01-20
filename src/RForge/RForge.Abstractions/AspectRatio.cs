namespace RForge.Abstractions;

/// <summary>
/// Used to help render an image by the correct aspect ratio.
/// </summary>
public struct AspectRatio
{
    /// <summary>
    /// The width or x value of the aspect ratio.
    /// </summary>
    public int Width { get; private set; }
    /// <summary>
    /// The height or y value of the aspect ratio.
    /// </summary>
    public int Height { get; private set; }

    private AspectRatio(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Represents a 1:1 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is1By1 = new AspectRatio(1, 1);
    /// <summary>
    /// Represents a square aspect ratio (1:1).
    /// </summary>
    public readonly static AspectRatio Square = Is1By1;

    /// <summary>
    /// Represents a 1:2 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is1By2 = new AspectRatio(1, 2);
    /// <summary>
    /// Represents a 1:3 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is1By3 = new AspectRatio(1, 3);

    /// <summary>
    /// Represents a 2:1 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is2By1 = new AspectRatio(2, 1);
    /// <summary>
    /// Represents a 2:3 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is2By3 = new AspectRatio(2, 3);

    /// <summary>
    /// Represents a 3:1 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is3By1 = new AspectRatio(3, 1);
    /// <summary>
    /// Represents a 3:2 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is3By2 = new AspectRatio(3, 2);
    /// <summary>
    /// Represents a 3:4 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is3By4 = new AspectRatio(3, 4);
    /// <summary>
    /// Represents a 3:5 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is3By5 = new AspectRatio(3, 5);

    /// <summary>
    /// Represents a 4:3 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is4By3 = new AspectRatio(4, 3);
    /// <summary>
    /// Represents a 4:5 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is4By5 = new AspectRatio(4, 5);

    /// <summary>
    /// Represents a 5:3 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is5By3 = new AspectRatio(5, 3);
    /// <summary>
    /// Represents a 5:4 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is5By4 = new AspectRatio(5, 4);

    /// <summary>
    /// Represents a 9:16 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is9By16 = new AspectRatio(9, 16);
    /// <summary>
    /// Represents a 16:9 aspect ratio.
    /// </summary>
    public readonly static AspectRatio Is16By9 = new AspectRatio(16, 9);

    /// <summary>
    /// Creates a custom aspect ratio with the specified width and height.
    /// </summary>
    /// <param name="width">The width of the aspect ratio.</param>
    /// <param name="height">The height of the aspect ratio.</param>
    /// <returns>A new <see cref="AspectRatio"/> instance with the specified width and height.</returns>
    public static AspectRatio Custom(int width, int height)
    {
        if (width <= 0) width = 1;
        if (height <= 0) height = 1;

        return new AspectRatio(width, height);
    }
}
