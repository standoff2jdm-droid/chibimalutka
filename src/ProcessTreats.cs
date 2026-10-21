using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class ProcessTreats
{
    static void Main()
    {
        string brainDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";
        string outDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";

        ProcessOne(Path.Combine(brainDir, "treat_cake_1792567399914.jpg"), Path.Combine(outDir, "treat_cake.png"));
        ProcessOne(Path.Combine(brainDir, "treat_boba_1792567414756.jpg"), Path.Combine(outDir, "treat_boba.png"));
        ProcessOne(Path.Combine(brainDir, "treat_strawberry_1792567429770.jpg"), Path.Combine(outDir, "treat_strawberry.png"));
        ProcessOne(Path.Combine(brainDir, "treat_cookie_1792567446594.jpg"), Path.Combine(outDir, "treat_cookie.png"));

        Console.WriteLine("All 4 chibi treats processed to alpha PNG!");
    }

    static void ProcessOne(string inPath, string outPath)
    {
        using (Bitmap src = new Bitmap(inPath))
        {
            int w = src.Width;
            int h = src.Height;
            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = src.GetPixel(x, y);
                    int brightness = (c.R + c.G + c.B) / 3;
                    int diff = Math.Max(Math.Abs(c.R - c.G), Math.Max(Math.Abs(c.R - c.B), Math.Abs(c.G - c.B)));

                    // Pure white background cutout
                    if (brightness > 248 && diff < 8)
                    {
                        dst.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                    }
                    else if (brightness > 238 && diff < 12)
                    {
                        int alpha = (int)((255 - brightness) * (255.0 / (255 - 238)));
                        alpha = Math.Max(0, Math.Min(255, alpha));
                        dst.SetPixel(x, y, Color.FromArgb(alpha, c.R, c.G, c.B));
                    }
                    else
                    {
                        dst.SetPixel(x, y, c);
                    }
                }
            }

            // Trim transparent edges and center in clean square
            int minX = w, maxX = 0, minY = h, maxY = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (dst.GetPixel(x, y).A > 15)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            int tw = maxX - minX + 1;
            int th = maxY - minY + 1;
            int size = Math.Max(tw, th) + 40;
            Bitmap trimmed = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(trimmed))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                int ox = (size - tw) / 2;
                int oy = (size - th) / 2;
                g.DrawImage(dst, new Rectangle(ox, oy, tw, th), new Rectangle(minX, minY, tw, th), GraphicsUnit.Pixel);
            }

            // Resize to 256x256 for fast UI loading
            Bitmap finalImg = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(finalImg))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(trimmed, 0, 0, 256, 256);
            }

            finalImg.Save(outPath, ImageFormat.Png);
            dst.Dispose();
            trimmed.Dispose();
            finalImg.Dispose();
        }
    }
}
