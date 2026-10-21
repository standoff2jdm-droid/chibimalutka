using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class CraftPerfectGazeFrames
{
    public static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        string assetsDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
        string releaseDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\release_dist\ChibiDesktopPet\assets\sprites";
        string artifactDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        var directions = new[]
        {
            new { Name = "idle_look_left", Dx = -5, Dy = 0 },
            new { Name = "idle_look_right", Dx = 5, Dy = 0 },
            new { Name = "idle_look_up", Dx = 0, Dy = -5 },
            new { Name = "idle_look_down", Dx = 0, Dy = 5 },
            new { Name = "idle_look_upleft", Dx = -4, Dy = -4 },
            new { Name = "idle_look_upright", Dx = 4, Dy = -4 },
            new { Name = "idle_look_downleft", Dx = -4, Dy = 4 },
            new { Name = "idle_look_downright", Dx = 4, Dy = 4 }
        };

        using (Bitmap idle = new Bitmap(idlePath))
        {
            foreach (var dir in directions)
            {
                using (Bitmap frame = BuildGazeSprite(idle, dir.Dx, dir.Dy))
                {
                    string fileName = dir.Name + ".png";
                    string artPath = Path.Combine(artifactDir, fileName);
                    string cropPath = Path.Combine(artifactDir, "crop_" + dir.Name + ".png");
                    string assetPath = Path.Combine(assetsDir, fileName);
                    string relPath = Path.Combine(releaseDir, fileName);

                    frame.Save(artPath, ImageFormat.Png);
                    frame.Save(assetPath, ImageFormat.Png);
                    frame.Save(relPath, ImageFormat.Png);

                    SaveCrop(frame, cropPath);
                    Console.WriteLine("Generated: " + fileName);
                }
            }
        }

        Console.WriteLine("All 8 gaze frames generated and deployed to assets!");
    }

    private static Bitmap BuildGazeSprite(Bitmap src, int dx, int dy)
    {
        Bitmap res = new Bitmap(src);

        // Process Left Eye (Center: 446, 278, bounds: [420..474], [252..304])
        ShiftEye(src, res, 446, 278, 420, 474, 252, 304, dx, dy);

        // Process Right Eye (Center: 574, 278, bounds: [546..600], [252..304])
        ShiftEye(src, res, 574, 278, 546, 600, 252, 304, dx, dy);

        // Absolute guarantee: any pixel outside the exact eye opening rectangles is restored from src
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                bool inLeftEye = (x >= 420 && x <= 474 && y >= 252 && y <= 304);
                bool inRightEye = (x >= 546 && x <= 600 && y >= 252 && y <= 304);
                if (!inLeftEye && !inRightEye)
                {
                    res.SetPixel(x, y, src.GetPixel(x, y));
                }
            }
        }

        return res;
    }

    private static bool IsFaceFeature(Color c, int x, int y, int minX, int maxX, int minY, int maxY)
    {
        // Outside the eye box is ALWAYS face
        if (x < minX || x > maxX || y < minY || y > maxY) return true;

        // Dark neutral eyeliner / eyelashes / contours
        if (c.R < 130 && c.G < 130 && c.B < 130 && Math.Abs(c.G - c.B) <= 14) return true;

        // Skin (forehead, nose, cheeks)
        if (c.R > 220 && c.G < 210 && c.B < 205 && c.R > c.B + 20) return true;

        // Hair (purple bangs or olive tips)
        if (c.B > 95 && c.R > 75 && (c.B >= c.G || c.R > c.G + 15)) return true;
        if (c.G > 115 && c.R > 110 && c.B < 150 && c.G >= c.B + 10 && c.R < 185) return true;

        return false;
    }

    private static void ShiftEye(Bitmap src, Bitmap dest, int cx, int cy, int minX, int maxX, int minY, int maxY, int dx, int dy)
    {
        int w = 72, h = 82;
        Bitmap iris = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        int halfW = w / 2;
        int halfH = h / 2;

        // 1. Isolate solid iris from src (NO internal holes!)
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double nx = (x - halfW) / (double)halfW;
                double ny = (y - halfH) / (double)halfH;
                double dist = nx * nx + ny * ny;

                if (dist <= 1.0)
                {
                    Color c = src.GetPixel(cx - halfW + x, cy - halfH + y);
                    float a = 1.0f;
                    if (dist > 0.90) a = (float)((1.0 - dist) / 0.10);
                    iris.SetPixel(x, y, Color.FromArgb((int)(Math.Min(1.0f, Math.Max(0.0f, a)) * 255), c.R, c.G, c.B));
                }
                else
                {
                    iris.SetPixel(x, y, Color.Transparent);
                }
            }
        }

        // 2. In dest, fill ONLY the crescent that is newly exposed by the shift
        for (int y = minY; y <= maxY; y++)
        {
            double ny = (y - cy) / (double)halfH;
            int sr = (int)(230 + ny * 20);
            int sg = (int)(225 + ny * 22);
            int sb = (int)(235 + ny * 15);
            Color scleraCol = Color.FromArgb(255, Math.Min(255, Math.Max(0, sr)), Math.Min(255, Math.Max(0, sg)), Math.Min(255, Math.Max(0, sb)));

            if (dx < 0)
            {
                // Vacated crescent on the right of iris
                int startX = Math.Max(minX, cx + halfW + dx - 2);
                int endX = Math.Min(maxX, cx + halfW + 2);
                for (int x = startX; x <= endX; x++)
                {
                    if (!IsFaceFeature(src.GetPixel(x, y), x, y, minX, maxX, minY, maxY))
                    {
                        dest.SetPixel(x, y, scleraCol);
                    }
                }
            }
            else if (dx > 0)
            {
                // Vacated crescent on the left of iris
                int startX = Math.Max(minX, cx - halfW - 2);
                int endX = Math.Min(maxX, cx - halfW + dx + 2);
                for (int x = startX; x <= endX; x++)
                {
                    if (!IsFaceFeature(src.GetPixel(x, y), x, y, minX, maxX, minY, maxY))
                    {
                        dest.SetPixel(x, y, scleraCol);
                    }
                }
            }

            if (dy < 0)
            {
                // Vacated crescent at bottom of iris
                int startY = Math.Max(minY, cy + halfH + dy - 2);
                int endY = Math.Min(maxY, cy + halfH + 2);
                if (y >= startY && y <= endY)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (!IsFaceFeature(src.GetPixel(x, y), x, y, minX, maxX, minY, maxY))
                        {
                            dest.SetPixel(x, y, scleraCol);
                        }
                    }
                }
            }
            else if (dy > 0)
            {
                // Vacated crescent at top of iris
                int startY = Math.Max(minY, cy - halfH - 2);
                int endY = Math.Min(maxY, cy - halfH + dy + 2);
                if (y >= startY && y <= endY)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (!IsFaceFeature(src.GetPixel(x, y), x, y, minX, maxX, minY, maxY))
                        {
                            dest.SetPixel(x, y, scleraCol);
                        }
                    }
                }
            }
        }

        // 3. Draw shifted iris onto dest
        using (Graphics g = Graphics.FromImage(dest))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(iris, cx + dx - halfW, cy + dy - halfH);
        }

        // 4. Cleanly restore all face features (bangs, eyebrows, eyelashes, eyelids, contours)
        for (int y = minY - 5; y <= maxY + 5; y++)
        {
            for (int x = minX - 5; x <= maxX + 5; x++)
            {
                Color c = src.GetPixel(x, y);
                if (IsFaceFeature(c, x, y, minX, maxX, minY, maxY))
                {
                    dest.SetPixel(x, y, c);
                }
            }
        }

        iris.Dispose();
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
