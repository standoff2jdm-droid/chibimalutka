using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class TestStarGazePerfect
{
    public static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap idle = new Bitmap(idlePath))
        {
            // 1. Extract the clean Star Pupil asset from left eye
            // Left star center in idle.png is at (447, 278)
            Bitmap starPupil = ExtractStarAsset(idle, 447, 278);
            starPupil.Save(System.IO.Path.Combine(outDir, "clean_star_pupil_perfect.png"), ImageFormat.Png);

            // 2. Inpaint/remove the stars from idle.png to create a flawless base with pure green irises
            Bitmap baseNoStar = new Bitmap(idle);
            InpaintStarFromIris(baseNoStar, 447, 278);
            InpaintStarFromIris(baseNoStar, 574, 278);
            baseNoStar.Save(System.IO.Path.Combine(outDir, "idle_pure_green_irises.png"), ImageFormat.Png);
            SaveCrop(baseNoStar, System.IO.Path.Combine(outDir, "crop_pure_green_irises.png"));

            // 3. Test look center: star at (0, 0)
            Bitmap testCenter = CompositeStarGaze(baseNoStar, starPupil, 0, 0);
            SaveCrop(testCenter, System.IO.Path.Combine(outDir, "crop_gaze_center.png"));

            // 4. Test look left: star at (-10, 0)
            Bitmap testLeft = CompositeStarGaze(baseNoStar, starPupil, -10, 0);
            SaveCrop(testLeft, System.IO.Path.Combine(outDir, "crop_gaze_left.png"));

            // 5. Test look right: star at (+10, 0)
            Bitmap testRight = CompositeStarGaze(baseNoStar, starPupil, 10, 0);
            SaveCrop(testRight, System.IO.Path.Combine(outDir, "crop_gaze_right.png"));

            // 6. Test look up-left: star at (-8, -6)
            Bitmap testUpLeft = CompositeStarGaze(baseNoStar, starPupil, -8, -6);
            SaveCrop(testUpLeft, System.IO.Path.Combine(outDir, "crop_gaze_upleft.png"));

            starPupil.Dispose();
            baseNoStar.Dispose();
            testCenter.Dispose();
            testLeft.Dispose();
            testRight.Dispose();
            testUpLeft.Dispose();

            Console.WriteLine("Perfect star gaze tests created!");
        }
    }

    private static Bitmap ExtractStarAsset(Bitmap src, int cx, int cy)
    {
        int w = 54, h = 60;
        Bitmap res = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        int halfW = w / 2;
        int halfH = h / 2;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sx = cx - halfW + x;
                int sy = cy - halfH + y;
                Color c = src.GetPixel(sx, sy);

                // Star yellow criteria: R > 175, G > 155, B < 155 and R >= G
                bool isStarYellow = (c.R > 175 && c.G > 155 && c.B < 155 && c.R >= c.G);
                // Highlight inside star:
                bool isSpecular = (c.R > 230 && c.G > 230 && c.B > 200 && (x - halfW) > -15 && (x - halfW) < 15 && (y - halfH) > -15 && (y - halfH) < 15);

                if (isStarYellow || isSpecular)
                {
                    res.SetPixel(x, y, c);
                }
                else
                {
                    res.SetPixel(x, y, Color.Transparent);
                }
            }
        }
        return res;
    }

    private static void InpaintStarFromIris(Bitmap bmp, int cx, int cy)
    {
        // Find all yellow star pixels around (cx, cy) and replace them with smooth iris shading
        // Iris has vertical gradient: dark at top (Y < cy), light at bottom (Y > cy)
        int rad = 28;
        for (int y = cy - rad; y <= cy + rad; y++)
        {
            for (int x = cx - rad; x <= cx + rad; x++)
            {
                Color c = bmp.GetPixel(x, y);
                bool isStarYellow = (c.R > 165 && c.G > 150 && c.B < 160 && c.R >= c.G - 5);
                bool isSpecular = (c.R > 230 && c.G > 230 && c.B > 190);

                if (isStarYellow || isSpecular)
                {
                    // Vertical position inside iris (-1.0 to 1.0)
                    double ny = (y - cy) / (double)rad;
                    // Interpolate between top iris dark green (65, 88, 48) and bottom iris light green (135, 165, 75)
                    int r = (int)(75 + ny * 25);
                    int g = (int)(95 + ny * 35);
                    int b = (int)(48 + ny * 20);

                    // Add slight radial shading (darker towards center pupil)
                    double dist = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / rad;
                    if (dist < 0.5)
                    {
                        r = (int)(r * 0.82);
                        g = (int)(g * 0.85);
                        b = (int)(b * 0.82);
                    }

                    r = Math.Max(0, Math.Min(255, r));
                    g = Math.Max(0, Math.Min(255, g));
                    b = Math.Max(0, Math.Min(255, b));

                    bmp.SetPixel(x, y, Color.FromArgb(255, r, g, b));
                }
            }
        }
    }

    private static Bitmap CompositeStarGaze(Bitmap baseBmp, Bitmap star, int dx, int dy)
    {
        Bitmap res = new Bitmap(baseBmp);
        using (Graphics g = Graphics.FromImage(res))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw left star at (447 + dx, 278 + dy)
            g.DrawImage(star, 447 + dx - star.Width / 2, 278 + dy - star.Height / 2);

            // Draw right star at (574 + dx, 278 + dy)
            g.DrawImage(star, 574 + dx - star.Width / 2, 278 + dy - star.Height / 2);
        }
        return res;
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
