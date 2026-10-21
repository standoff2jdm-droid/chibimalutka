using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace SpriteTool
{
    class Program
    {
        static bool IsBgColor(Color c)
        {
            if (c.R >= 244 && c.G >= 244 && c.B >= 244)
            {
                int maxDiff = Math.Max(Math.Abs(c.R - c.G), Math.Max(Math.Abs(c.R - c.B), Math.Abs(c.G - c.B)));
                if (maxDiff <= 8)
                {
                    return true;
                }
            }
            return false;
        }

        public static void ProcessImage(string inputPath, string outputPath)
        {
            Console.WriteLine("Processing: " + inputPath + " -> " + outputPath);
            using (Bitmap bmp = new Bitmap(inputPath))
            {
                int w = bmp.Width;
                int h = bmp.Height;

                // Clone as 32bppArgb so all pixels have Alpha = 255 initially!
                Bitmap result = bmp.Clone(new Rectangle(0, 0, w, h), PixelFormat.Format32bppArgb);

                bool[,] isBg = new bool[w, h];
                Queue<Point> q = new Queue<Point>();

                // Seed outer borders
                for (int x = 0; x < w; x++)
                {
                    if (IsBgColor(bmp.GetPixel(x, 0))) { isBg[x, 0] = true; q.Enqueue(new Point(x, 0)); }
                    if (IsBgColor(bmp.GetPixel(x, h - 1))) { isBg[x, h - 1] = true; q.Enqueue(new Point(x, h - 1)); }
                }
                for (int y = 0; y < h; y++)
                {
                    if (IsBgColor(bmp.GetPixel(0, y))) { isBg[0, y] = true; q.Enqueue(new Point(0, y)); }
                    if (IsBgColor(bmp.GetPixel(w - 1, y))) { isBg[w - 1, y] = true; q.Enqueue(new Point(w - 1, y)); }
                }

                // BFS Flood Fill from edges
                int[] dx = new int[] { 0, 0, 1, -1 };
                int[] dy = new int[] { 1, -1, 0, 0 };

                while (q.Count > 0)
                {
                    Point pt = q.Dequeue();
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = pt.X + dx[i];
                        int ny = pt.Y + dy[i];
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h && !isBg[nx, ny])
                        {
                            if (IsBgColor(bmp.GetPixel(nx, ny)))
                            {
                                isBg[nx, ny] = true;
                                q.Enqueue(new Point(nx, ny));
                            }
                        }
                    }
                }

                // Apply transparency to background and smooth edges
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (isBg[x, y])
                        {
                            result.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                        }
                        else
                        {
                            bool nearBg = false;
                            for (int i = 0; i < 4; i++)
                            {
                                int nx = x + dx[i];
                                int ny = y + dy[i];
                                if (nx >= 0 && nx < w && ny >= 0 && ny < h && isBg[nx, ny])
                                {
                                    nearBg = true;
                                    break;
                                }
                            }

                            if (nearBg)
                            {
                                Color orig = bmp.GetPixel(x, y);
                                double brightness = (orig.R + orig.G + orig.B) / (3.0 * 255.0);
                                int alpha = (int)(255 * Math.Max(0.15, 1.0 - Math.Pow(brightness, 3)));
                                if (alpha > 255) alpha = 255;
                                result.SetPixel(x, y, Color.FromArgb(alpha, orig.R, orig.G, orig.B));
                            }
                        }
                    }
                }

                result.Save(outputPath, ImageFormat.Png);
                result.Dispose();
                Console.WriteLine("Done saving: " + outputPath);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ProcessSprites <input> <output>");
                return;
            }
            ProcessImage(args[0], args[1]);
        }
    }
}
