using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHIPInterpreter
{
    internal class Display
    {
        internal void Draw(bool[,] buffer)
        {
            Console.Clear();
            for(int y = 0; y < 32; y++)
            {
                for(int x = 0; x < 64; x++)
                {
                    Console.Write(buffer[x, y] ? "█" : " ");
                }
                Console.Write("\n");
            }
            Console.SetWindowPosition(0, 0);
        }
    }
}
