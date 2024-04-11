using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public readonly static AspectRatio Is1By1 = new AspectRatio(1, 1);
    public readonly static AspectRatio Square = Is1By1;

    public readonly static AspectRatio Is1By2 = new AspectRatio(1, 2);
    public readonly static AspectRatio Is1By3 = new AspectRatio(1, 3);

    public readonly static AspectRatio Is2By1 = new AspectRatio(2, 1);
    public readonly static AspectRatio Is2By3 = new AspectRatio(2, 3);

    public readonly static AspectRatio Is3By1 = new AspectRatio(3, 1);
    public readonly static AspectRatio Is3By2 = new AspectRatio(3, 2);
    public readonly static AspectRatio Is3By4 = new AspectRatio(3, 4);
    public readonly static AspectRatio Is3By5 = new AspectRatio(3, 5);

    public readonly static AspectRatio Is4By3 = new AspectRatio(4, 3);
    public readonly static AspectRatio Is4By5 = new AspectRatio(4, 5);

    public readonly static AspectRatio Is5By3 = new AspectRatio(5, 3);
    public readonly static AspectRatio Is5By4 = new AspectRatio(5, 4);

    public readonly static AspectRatio Is9By16 = new AspectRatio(9, 16);
    public readonly static AspectRatio Is16By9 = new AspectRatio(16, 9);

    public static AspectRatio Custom(int width, int height)
    {
        if (width <= 0) width = 1;
        if (height <= 0) height = 1;

        return new AspectRatio(width, height);
    }
}
