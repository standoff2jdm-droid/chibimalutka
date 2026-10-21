using System;
using System.Drawing;
using System.IO;

class EyeFinder
{
    static void Main()
    {
        string path = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        using (var bmp = new Bitmap(path))
        {
            double sumLX = 0, sumLY = 0, countL = 0;
            double sumRX = 0, sumRY = 0, countR = 0;

            for (int y = 350; y < 600; y++)
            {
                for (int x = 300; x < 720; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    // Star highlight color: bright golden yellow
                    if (c.R > 220 && c.G > 210 && c.B < 160)
                    {
                        if (x < 512)
                        {
                            sumLX += x; sumLY += y; countL++;
                        }
                        else
                        {
                            sumRX += x; sumRY += y; countR++;
                        }
                    }
                }
            }

            Console.WriteLine("Left eye pupil center: ({0:F1}, {1:F1}) [samples={2}]", sumLX / countL, sumLY / countL, countL);
            Console.WriteLine("Right eye pupil center: ({0:F1}, {1:F1}) [samples={2}]", sumRX / countR, sumRY / countR, countR);
        }
    }
}
