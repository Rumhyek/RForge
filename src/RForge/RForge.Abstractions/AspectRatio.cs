using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForge.Abstractions
{
    public struct AspectRatio
    {

        public int Width { get; private set; }
        public int Height { get; private set; }

        private AspectRatio(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public static AspectRatio Is1By1 = new AspectRatio(1, 1);
        public static AspectRatio Square = Is1By1;

        public static AspectRatio Is1By2 = new AspectRatio(1, 2);
        public static AspectRatio Is1By3 => new AspectRatio(1, 3);

        public static AspectRatio Is2By1 = new AspectRatio(2, 1);
        public static AspectRatio Is2By3 = new AspectRatio(2, 3);

        public static AspectRatio Is3By1 = new AspectRatio(3, 1);
        public static AspectRatio Is3By2 = new AspectRatio(3, 2);
        public static AspectRatio Is3By4 = new AspectRatio(3, 4);
        public static AspectRatio Is3By5 = new AspectRatio(3, 5);

        public static AspectRatio Is4By3 = new AspectRatio(4, 3);
        public static AspectRatio Is4By5 = new AspectRatio(4, 5);

        public static AspectRatio Is5By3 = new AspectRatio(5, 3);
        public static AspectRatio Is5By4 = new AspectRatio(5, 4);

        public static AspectRatio Is9By16 = new AspectRatio(9, 16);
        public static AspectRatio Is16By9 = new AspectRatio(16, 9);
    }
}
