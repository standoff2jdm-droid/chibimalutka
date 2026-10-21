using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public class StarEyeTest
{
    public static void Main()
    {
        string spritesDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
        string idlePath = Path.Combine(spritesDir, "idle.png");

        using (Bitmap idle = new Bitmap(idlePath))
        {
            int w = idle.Width;
            int h = idle.Height;

            // 1. Extract clean star pupil (48x48)
            int starSize = 48;
            Bitmap star = new Bitmap(starSize, starSize, PixelFormat.Format32bppArgb);
            using (Graphics gs = Graphics.FromImage(star))
            {
                gs.DrawImage(idle, new Rectangle(0, 0, starSize, starSize), new Rectangle(447 - 24, 300 - 24, starSize, starSize), GraphicsUnit.Pixel);
            }

            for (int y = 0; y < starSize; y++)
            {
                for (int x = 0; x < starSize; x++)
                {
                    Color c = star.GetPixel(x, y);
                    bool isYellow = (c.R > 185 && c.G > 165 && c.B < 140);
                    if (!isYellow)
                    {
                        star.SetPixel(x, y, Color.Transparent);
                    }
                }
            }
            star.Save(Path.Combine(spritesDir, "pupil_star.png"), ImageFormat.Png);

            // 2. Create idle_no_star: fill the center of iris with the iris dark gradient
            Bitmap noStar = new Bitmap(idle);
            using (Graphics g = Graphics.FromImage(noStar))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color darkIris = Color.FromArgb(255, 38, 48, 22);

                // Left iris
                using (GraphicsPath gpL = new GraphicsPath())
                {
                    gpL.AddEllipse(447 - 18, 300 - 18, 36, 36);
                    using (PathGradientBrush pgb = new PathGradientBrush(gpL))
                    {
                        pgb.CenterColor = darkIris;
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 38, 48, 22) };
                        g.FillPath(pgb, gpL);
                    }
                }

                // Right iris
                using (GraphicsPath gpR = new GraphicsPath())
                {
                    gpR.AddEllipse(576 - 18, 305 - 18, 36, 36);
                    using (PathGradientBrush pgb = new PathGradientBrush(gpR))
                    {
                        pgb.CenterColor = darkIris;
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 38, 48, 22) };
                        g.FillPath(pgb, gpR);
                    }
                }
            }
            noStar.Save(Path.Combine(spritesDir, "idle_nostar.png"), ImageFormat.Png);

            // 3. Composite test: shift star left by 7 px (looking left)
            Bitmap compLeft = new Bitmap(noStar);
            using (Graphics gc = Graphics.FromImage(compLeft))
            {
                gc.DrawImage(star, 447 - 24 - 7, 300 - 24, starSize, starSize);
                gc.DrawImage(star, 576 - 24 - 7, 305 - 24, starSize, starSize);
            }

            // Crop face to check
            Rectangle cropRect = new Rectangle(350, 220, 324, 160);
            using (Bitmap crop = new Bitmap(324, 160))
            using (Graphics gcr = Graphics.FromImage(crop))
            {
                gcr.DrawImage(compLeft, 0, 0, cropRect, GraphicsUnit.Pixel);
                crop.Save(Path.Combine(spritesDir, "preview_star_left.png"), ImageFormat.Png);
            }

            Console.WriteLine("preview_star_left.png created!");
        }
    }
}
