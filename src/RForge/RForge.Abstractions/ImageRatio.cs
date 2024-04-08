using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForge.Abstractions
{
    public static class ImageRatioCss
    {
        public const string Is1By1 = "is-1by1";
        public const string Is1By2 = "is-1by2";
        public const string Is1By3 = "is-1by3";

        public const string Is2By1 = "is-2by1";
        public const string Is2By3 = "is-2by3";

        public const string Is3By1 = "is-3by1";
        public const string Is3By2 = "is-3by2";
        public const string Is3By4 = "is-3by4";
        public const string Is3By5 = "is-3by5";

        public const string Is4By3 = "is-4by3";
        public const string Is4By5 = "is-4by5";

        public const string Is5By3 = "is-5by3";
        public const string Is5By4 = "is-5by4";

        public const string Is9By16 = "is-9by16";
        public const string Is16By9 = "is-16by9";
    }

    public struct ImageRatio
    {

        public int Width { get; private set; }
        public int Height { get; private set; }

        private ImageRatio(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public static ImageRatio Is1By1 = new ImageRatio(1, 1);
        public static ImageRatio Square = Is1By1;

        public static ImageRatio Is1By2 = new ImageRatio(1, 2);
        public static ImageRatio Is1By3 => new ImageRatio(1, 3);

        public static ImageRatio Is2By1 = new ImageRatio(2, 1);
        public static ImageRatio Is2By3 = new ImageRatio(2, 3);

        public static ImageRatio Is3By1 = new ImageRatio(3, 1);
        public static ImageRatio Is3By2 = new ImageRatio(3, 2);
        public static ImageRatio Is3By4 = new ImageRatio(3, 4);
        public static ImageRatio Is3By5 = new ImageRatio(3, 5);

        public static ImageRatio Is4By3 = new ImageRatio(4, 3);
        public static ImageRatio Is4By5 = new ImageRatio(4, 5);

        public static ImageRatio Is5By3 = new ImageRatio(5, 3);
        public static ImageRatio Is5By4 = new ImageRatio(5, 4);

        public static ImageRatio Is9By16 = new ImageRatio(9, 16);
        public static ImageRatio Is16By9 = new ImageRatio(16, 9);
    }
}
