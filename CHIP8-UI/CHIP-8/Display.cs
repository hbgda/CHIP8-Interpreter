using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CHIPInterpreter
{
    internal class Display
    {
        internal Image chipDisplay;
        internal WriteableBitmap chipFrame;
        public Display(Image chipDisplay)
        {
            this.chipDisplay = chipDisplay;
            this.chipFrame = new WriteableBitmap(64, 32, 96, 96, PixelFormats.BlackWhite, null);
        }


        internal void DrawFrameToConsole(bool[,] buffer)
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

        internal void BufferToBitmap(bool[,] buffer, ref WriteableBitmap bmp)
        {
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    try
                    {
                        byte[] colourData = new byte[] { 0, 0, 0, 0 };
                        if (buffer[x, y])
                        {
                            colourData = new byte[] { 255, 255, 255, 255 };
                        }

                        Int32Rect rect = new Int32Rect(x, y, 1, 1);
                        bmp.WritePixels(rect, colourData, 4, 0);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        internal void DrawFrame(bool[,] buffer)
        {
            BufferToBitmap(buffer, ref chipFrame);
            chipDisplay.Source = chipFrame;
        }
    }
}
