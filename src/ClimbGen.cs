using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class ClimbGen
{
    static void Main()
    {
        string spritesDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
        string idlePath = Path.Combine(spritesDir, "idle.png");

        using (Bitmap idle = new Bitmap(idlePath))
        {
            int w = idle.Width;
            int h = idle.Height;

            for (int step = 1; step <= 2; step++)
            {
                Bitmap frame = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(frame))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                    float pivotX = w * 0.5f;
                    float pivotY = h * 0.65f;
                    float angle = (step == 1) ? -4.5f : 4.5f;
                    float dy = (step == 1) ? -10f : 10f;

                    g.TranslateTransform(pivotX, pivotY);
                    g.RotateTransform(angle);
                    g.TranslateTransform(-pivotX, -pivotY + dy);

                    g.DrawImage(idle, 0, 0, w, h);
                }

                string outName = (step == 1) ? "climb1.png" : "climb2.png";
                frame.Save(Path.Combine(spritesDir, outName), ImageFormat.Png);
            }
        }
        Console.WriteLine("Climb frames regenerated from idle.png successfully!");
    }
}
