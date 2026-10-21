using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class TestEyeTrackingRender
{
    public static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap idle = new Bitmap(idlePath))
        {
            // 1. Isolate the exact glowing star pupil from left and right eye
            // Left star center: (447, 278)
            // Right star center: (574, 278)
            // Star size is around 40x50 in 1024-space
            Bitmap starL = ExtractStarPupil(idle, 447, 278);
            Bitmap starR = ExtractStarPupil(idle, 574, 278);

            starL.Save(System.IO.Path.Combine(outDir, "clean_star_L.png"), ImageFormat.Png);

            // 2. Create idle_nostar where the star is cleanly filled with the iris's natural olive green
            Bitmap noStar = new Bitmap(idle);
            CleanFillStar(noStar, 447, 278);
            CleanFillStar(noStar, 574, 278);
            noStar.Save(System.IO.Path.Combine(outDir, "clean_idle_nostar.png"), ImageFormat.Png);

            // 3. Render test glancing LEFT (offset dx = -18, dy = 0)
            Bitmap glanceLeft = new Bitmap(noStar);
            using (Graphics g = Graphics.FromImage(glanceLeft))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Draw stars shifted left by 18 pixels in 1024 space
                int dx = -18, dy = 0;
                g.DrawImage(starL, 447 + dx - starL.Width / 2, 278 + dy - starL.Height / 2);
                g.DrawImage(starR, 574 + dx - starR.Width / 2, 278 + dy - starR.Height / 2);
            }
            glanceLeft.Save(System.IO.Path.Combine(outDir, "test_star_glance_left.png"), ImageFormat.Png);

            // 4. Render test glancing RIGHT (offset dx = +18, dy = 0)
            Bitmap glanceRight = new Bitmap(noStar);
            using (Graphics g = Graphics.FromImage(glanceRight))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int dx = 18, dy = 0;
                g.DrawImage(starL, 447 + dx - starL.Width / 2, 278 + dy - starL.Height / 2);
                g.DrawImage(starR, 574 + dx - starR.Width / 2, 278 + dy - starR.Height / 2);
            }
            glanceRight.Save(System.IO.Path.Combine(outDir, "test_star_glance_right.png"), ImageFormat.Png);

            // Also save eye region crops for inspection
            SaveCrop(glanceLeft, System.IO.Path.Combine(outDir, "crop_glance_left.png"));
            SaveCrop(glanceRight, System.IO.Path.Combine(outDir, "crop_glance_right.png"));

            starL.Dispose();
            starR.Dispose();
            noStar.Dispose();
            glanceLeft.Dispose();
            glanceRight.Dispose();

            Console.WriteLine("Eye tracking tests created successfully!");
        }
    }

    private static Bitmap ExtractStarPupil(Bitmap src, int cx, int cy)
    {
        int w = 56, h = 64;
        Bitmap res = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sx = cx - w / 2 + x;
                int sy = cy - h / 2 + y;
                Color c = src.GetPixel(sx, sy);

                // Star is yellow / gold / white highlight
                // Star body: R > 200, G > 180, B < 150
                // Star glow: R > 170, G > 150, B < 130
                // Specular white: R > 230, G > 230, B > 200
                bool isStarYellow = (c.R > 180 && c.G > 160 && c.B < 150 && c.R >= c.G);
                bool isSpecular = (c.R > 235 && c.G > 235 && c.B > 210);

                if (isStarYellow || isSpecular)
                {
                    // Distance from center
                    double dx = x - w / 2.0;
                    double dy = y - h / 2.0;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < 26.0)
                    {
                        res.SetPixel(x, y, c);
                    }
                }
            }
        }
        return res;
    }

    private static void CleanFillStar(Bitmap bmp, int cx, int cy)
    {
        // Smoothly fill the star with the surrounding deep olive/moss green of the iris
        // Sample iris color around the star:
        // Top iris: (cx, cy - 26) -> dark moss green (65, 85, 45)
        // Bottom iris: (cx, cy + 26) -> brighter olive green (120, 145, 65)
        // Center: around (90, 115, 55)
        Color irisDeep = bmp.GetPixel(cx, cy - 25);
        Color irisMid = bmp.GetPixel(cx - 24, cy);
        Color irisLight = bmp.GetPixel(cx, cy + 25);

        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(cx - 22, cy - 25, 44, 50);
                using (PathGradientBrush pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterColor = Color.FromArgb(255, 75, 95, 45); // Deep pupil center
                    pgb.SurroundColors = new Color[] { irisMid };
                    g.FillPath(pgb, gp);
                }
            }
        }
    }

    private static void SaveCrop(Bitmap src, string outPath)
    {
        Rectangle rect = new Rectangle(300, 150, 424, 200);
        using (Bitmap crop = src.Clone(rect, src.PixelFormat))
        {
            crop.Save(outPath, ImageFormat.Png);
        }
    }
}
