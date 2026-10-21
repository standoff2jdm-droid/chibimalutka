using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public class TestCutout
{
    public static void Main()
    {
        string baseDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        TestSwitch(baseDir, outDir);
        TestTablet(baseDir, outDir);
        TestStar(baseDir, outDir);
    }

    private static void TestSwitch(string baseDir, string outDir)
    {
        using (Bitmap src = new Bitmap(System.IO.Path.Combine(baseDir, "prop_switch.png")))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            // Flood-fill outer background to make it transparent
            // Outer background starts at top edges (x, 0) and bottom edges (x, src.Height-1)
            // Stop at dark outline of Switch and hands (outline: R < 70 && G < 70 && B < 70)
            bool[,] isBg = new bool[src.Width, src.Height];
            Queue<Point> q = new Queue<Point>();

            // Enqueue all top border pixels except where thumb might hit edge
            for (int x = 0; x < src.Width; x++)
            {
                q.Enqueue(new Point(x, 0)); isBg[x, 0] = true;
                q.Enqueue(new Point(x, src.Height - 1)); isBg[x, src.Height - 1] = true;
            }
            for (int y = 0; y < src.Height; y++)
            {
                q.Enqueue(new Point(0, y)); isBg[0, y] = true;
                q.Enqueue(new Point(src.Width - 1, y)); isBg[src.Width - 1, y] = true;
            }

            while (q.Count > 0)
            {
                Point p = q.Dequeue();
                Point[] neighbors = new Point[] {
                    new Point(p.X + 1, p.Y),
                    new Point(p.X - 1, p.Y),
                    new Point(p.X, p.Y + 1),
                    new Point(p.X, p.Y - 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.X >= 0 && n.X < src.Width && n.Y >= 0 && n.Y < src.Height && !isBg[n.X, n.Y])
                    {
                        Color c = src.GetPixel(n.X, n.Y);
                        // Is this pixel part of the dark outline?
                        // Outline: R < 75 && G < 65 && B < 75
                        bool isBorder = (c.R < 75 && c.G < 65 && c.B < 75);
                        if (!isBorder)
                        {
                            isBg[n.X, n.Y] = true;
                            q.Enqueue(n);
                        }
                    }
                }
            }

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    if (!isBg[x, y])
                    {
                        res.SetPixel(x, y, src.GetPixel(x, y));
                    }
                    else
                    {
                        res.SetPixel(x, y, Color.Transparent);
                    }
                }
            }

            res.Save(System.IO.Path.Combine(outDir, "test_cutout_switch.png"), ImageFormat.Png);
            Console.WriteLine("Switch cutout test saved!");
        }
    }

    private static void TestTablet(string baseDir, string outDir)
    {
        using (Bitmap src = new Bitmap(System.IO.Path.Combine(baseDir, "prop_tablet.png")))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            bool[,] isBg = new bool[src.Width, src.Height];
            Queue<Point> q = new Queue<Point>();

            for (int x = 0; x < src.Width; x++)
            {
                q.Enqueue(new Point(x, 0)); isBg[x, 0] = true;
                q.Enqueue(new Point(x, src.Height - 1)); isBg[x, src.Height - 1] = true;
            }
            for (int y = 0; y < src.Height; y++)
            {
                q.Enqueue(new Point(0, y)); isBg[0, y] = true;
                q.Enqueue(new Point(src.Width - 1, y)); isBg[src.Width - 1, y] = true;
            }

            while (q.Count > 0)
            {
                Point p = q.Dequeue();
                Point[] neighbors = new Point[] {
                    new Point(p.X + 1, p.Y),
                    new Point(p.X - 1, p.Y),
                    new Point(p.X, p.Y + 1),
                    new Point(p.X, p.Y - 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.X >= 0 && n.X < src.Width && n.Y >= 0 && n.Y < src.Height && !isBg[n.X, n.Y])
                    {
                        Color c = src.GetPixel(n.X, n.Y);
                        // Border is dark line
                        bool isBorder = (c.R < 70 && c.G < 65 && c.B < 70);
                        if (!isBorder)
                        {
                            isBg[n.X, n.Y] = true;
                            q.Enqueue(n);
                        }
                    }
                }
            }

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    if (!isBg[x, y])
                    {
                        res.SetPixel(x, y, src.GetPixel(x, y));
                    }
                    else
                    {
                        res.SetPixel(x, y, Color.Transparent);
                    }
                }
            }

            res.Save(System.IO.Path.Combine(outDir, "test_cutout_tablet.png"), ImageFormat.Png);
            Console.WriteLine("Tablet cutout test saved!");
        }
    }

    private static void TestStar(string baseDir, string outDir)
    {
        using (Bitmap src = new Bitmap(System.IO.Path.Combine(baseDir, "prop_star.png")))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            // For star: Star plushie is bright yellow (R > 180, G > 150, B < 150)
            // with cute dark eyes/mouth and pink blush, and dark border (R < 110, G < 95, B < 85)
            // Let's find all pixels belonging to the star pillow
            // The center of the star pillow is around (180, 160)
            bool[,] isStar = new bool[src.Width, src.Height];
            Queue<Point> q = new Queue<Point>();

            // Seed from inside the star
            q.Enqueue(new Point(180, 160));
            isStar[180, 160] = true;

            while (q.Count > 0)
            {
                Point p = q.Dequeue();
                Point[] neighbors = new Point[] {
                    new Point(p.X + 1, p.Y),
                    new Point(p.X - 1, p.Y),
                    new Point(p.X, p.Y + 1),
                    new Point(p.X, p.Y - 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.X >= 0 && n.X < src.Width && n.Y >= 0 && n.Y < src.Height && !isStar[n.X, n.Y])
                    {
                        Color c = src.GetPixel(n.X, n.Y);
                        // Is this pixel part of the star?
                        // Star is yellow tones, pink blush, cute eyes/mouth, white highlight, or star's dark border
                        bool isYellow = (c.R > 160 && c.G > 130 && c.B < 145);
                        bool isBlush = (c.R > 210 && c.G > 120 && c.B > 120 && c.G < 180);
                        bool isHighlight = (c.R > 240 && c.G > 240 && c.B > 180);
                        bool isStarFace = (c.R < 90 && c.G < 60 && c.B < 50); // eye / mouth
                        bool isStarBorder = (c.R < 130 && c.G < 110 && c.B < 95 && Math.Abs(c.R - c.G) < 35);

                        // But NOT skin (skin has R > 230, G in 180..220, B in 170..210, with R > B + 30)
                        bool isSkin = (c.R > 220 && c.G > 180 && c.B > 165 && (c.G - c.B) < 25 && c.B > 160);
                        // And NOT purple sweater (B > R)
                        bool isPurple = (c.B > c.R || (c.B > 130 && c.G < 120));

                        if ((isYellow || isBlush || isHighlight || isStarFace || isStarBorder) && !isPurple && !isSkin)
                        {
                            isStar[n.X, n.Y] = true;
                            q.Enqueue(n);
                        }
                    }
                }
            }

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    if (isStar[x, y])
                    {
                        res.SetPixel(x, y, src.GetPixel(x, y));
                    }
                    else
                    {
                        res.SetPixel(x, y, Color.Transparent);
                    }
                }
            }

            res.Save(System.IO.Path.Combine(outDir, "test_cutout_star.png"), ImageFormat.Png);
            Console.WriteLine("Star cutout test saved!");
        }
    }
}
