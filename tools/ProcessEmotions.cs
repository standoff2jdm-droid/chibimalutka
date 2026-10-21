using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public class ProcessEmotions
{
    public static void Main()
    {
        string baseDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";
        string spritesDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
        string idlePath = Path.Combine(spritesDir, "idle.png");

        Console.WriteLine("1. Extracting Clean Horns from idle.png...");
        Bitmap hornL = null;
        Bitmap hornR = null;

        using (Bitmap idleBmp = new Bitmap(idlePath))
        {
            // Left horn in idle is around (360, 72), w: 38, h: 56
            hornL = ExtractHorn(idleBmp, new Rectangle(360, 72, 38, 56));
            // Right horn in idle is around (622, 74), w: 38, h: 56
            hornR = ExtractHorn(idleBmp, new Rectangle(622, 74, 38, 56));
        }

        Console.WriteLine("2. Processing 4 Emotion Sprites...");
        // 1. Console
        ProcessSprite(
            Path.Combine(baseDir, "sprite_console_1792507823218.jpg"),
            Path.Combine(spritesDir, "console.png"),
            hornL, hornR,
            new Point(370, 106), new Point(616, 110), 1.0f
        );

        // 2. Drawing
        ProcessSprite(
            Path.Combine(baseDir, "sprite_drawing_1792507842670.jpg"),
            Path.Combine(spritesDir, "drawing.png"),
            hornL, hornR,
            new Point(370, 106), new Point(616, 110), 1.0f
        );

        // 3. Sleep
        ProcessSprite(
            Path.Combine(baseDir, "sprite_sleep_1792507882034.jpg"),
            Path.Combine(spritesDir, "sleep.png"),
            hornL, hornR,
            new Point(382, 102), new Point(632, 116), 1.0f
        );

        // 4. Pout
        ProcessSprite(
            Path.Combine(baseDir, "sprite_pout_1792507912591.jpg"),
            Path.Combine(spritesDir, "pout.png"),
            hornL, hornR,
            new Point(370, 106), new Point(616, 110), 1.0f
        );

        hornL.Dispose();
        hornR.Dispose();

        Console.WriteLine("3. Extracting High-Accuracy Eye Tracking Assets from idle.png...");
        ProcessEyeAssets(idlePath, spritesDir);

        Console.WriteLine("All assets generated successfully!");
    }

    private static Bitmap ExtractHorn(Bitmap idle, Rectangle rect)
    {
        Bitmap horn = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(horn))
        {
            g.DrawImage(idle, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
        }

        // Flood-fill make outer white/light purple transparent
        bool[,] visited = new bool[rect.Width, rect.Height];
        Queue<Point> q = new Queue<Point>();

        for (int x = 0; x < rect.Width; x++)
        {
            q.Enqueue(new Point(x, 0)); visited[x, 0] = true;
            q.Enqueue(new Point(x, rect.Height - 1)); visited[x, rect.Height - 1] = true;
        }
        for (int y = 0; y < rect.Height; y++)
        {
            q.Enqueue(new Point(0, y)); visited[0, y] = true;
            q.Enqueue(new Point(rect.Width - 1, y)); visited[rect.Width - 1, y] = true;
        }

        while (q.Count > 0)
        {
            Point p = q.Dequeue();
            Color c = horn.GetPixel(p.X, p.Y);

            // Is it NOT horn red or horn dark border?
            bool isHornRed = (c.R > 130 && c.R > c.G * 1.5 && c.R > c.B * 1.5);
            bool isHornBorder = (c.R < 75 && c.G < 60 && c.B < 65);

            if (!isHornRed && !isHornBorder)
            {
                horn.SetPixel(p.X, p.Y, Color.Transparent);

                int[] dx = { 1, -1, 0, 0 };
                int[] dy = { 0, 0, 1, -1 };
                for (int i = 0; i < 4; i++)
                {
                    int nx = p.X + dx[i];
                    int ny = p.Y + dy[i];
                    if (nx >= 0 && nx < rect.Width && ny >= 0 && ny < rect.Height && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        q.Enqueue(new Point(nx, ny));
                    }
                }
            }
        }
        return horn;
    }

    private static void ProcessSprite(string srcJpg, string outPng, Bitmap hornL, Bitmap hornR, Point pL, Point pR, float hornScale)
    {
        Console.WriteLine("Processing " + Path.GetFileName(outPng) + "...");
        using (Bitmap src = new Bitmap(srcJpg))
        {
            int w = src.Width;
            int h = src.Height;
            Bitmap cut = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            // Flood-fill transparency from perimeter
            bool[,] isBg = new bool[w, h];
            Queue<Point> q = new Queue<Point>();

            for (int x = 0; x < w; x++)
            {
                q.Enqueue(new Point(x, 0)); isBg[x, 0] = true;
                q.Enqueue(new Point(x, h - 1)); isBg[x, h - 1] = true;
            }
            for (int y = 0; y < h; y++)
            {
                q.Enqueue(new Point(0, y)); isBg[0, y] = true;
                q.Enqueue(new Point(w - 1, y)); isBg[w - 1, y] = true;
            }

            while (q.Count > 0)
            {
                Point p = q.Dequeue();
                Color c = src.GetPixel(p.X, p.Y);
                // Background is white (#FFFFFF)
                int diff = (255 - c.R) + (255 - c.G) + (255 - c.B);
                if (diff < 50)
                {
                    int[] dx = { 1, -1, 0, 0 };
                    int[] dy = { 0, 0, 1, -1 };
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = p.X + dx[i];
                        int ny = p.Y + dy[i];
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h && !isBg[nx, ny])
                        {
                            isBg[nx, ny] = true;
                            q.Enqueue(new Point(nx, ny));
                        }
                    }
                }
            }

            // Copy pixels with smooth edge anti-aliasing
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (isBg[x, y])
                    {
                        Color c = src.GetPixel(x, y);
                        int diff = (255 - c.R) + (255 - c.G) + (255 - c.B);
                        if (diff < 15)
                        {
                            cut.SetPixel(x, y, Color.Transparent);
                        }
                        else
                        {
                            float alpha = Math.Min(1.0f, diff / 40.0f);
                            cut.SetPixel(x, y, Color.FromArgb((int)(alpha * 255), c.R, c.G, c.B));
                        }
                    }
                    else
                    {
                        cut.SetPixel(x, y, src.GetPixel(x, y));
                    }
                }
            }

            // Composite red horn clips onto hair
            using (Graphics g = Graphics.FromImage(cut))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (hornL != null && pL.X > 0)
                {
                    g.DrawImage(hornL, new Rectangle(pL.X, pL.Y, (int)(hornL.Width * hornScale), (int)(hornL.Height * hornScale)));
                }
                if (hornR != null && pR.X > 0)
                {
                    g.DrawImage(hornR, new Rectangle(pR.X, pR.Y, (int)(hornR.Width * hornScale), (int)(hornR.Height * hornScale)));
                }
            }

            cut.Save(outPng, ImageFormat.Png);
            cut.Dispose();
        }
    }

    private static void ProcessEyeAssets(string idlePath, string spritesDir)
    {
        using (Bitmap idle = new Bitmap(idlePath))
        {
            int w = idle.Width;
            int h = idle.Height;

            // Eye centers in idle.png: Left (447, 300), Right (576, 305)
            int pupilW = 58;
            int pupilH = 64;

            // 1. Extract Left Pupil (clean elliptical mask strictly covering iris & star)
            using (Bitmap pupilL = new Bitmap(pupilW, pupilH, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(pupilL))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(2, 2, pupilW - 4, pupilH - 4);
                    g.SetClip(gp);

                    int srcX = 447 - pupilW / 2;
                    int srcY = 300 - pupilH / 2;
                    g.DrawImage(idle, new Rectangle(0, 0, pupilW, pupilH), new Rectangle(srcX, srcY, pupilW, pupilH), GraphicsUnit.Pixel);
                }
                pupilL.Save(Path.Combine(spritesDir, "pupil_l.png"), ImageFormat.Png);
            }

            // 2. Extract Right Pupil
            using (Bitmap pupilR = new Bitmap(pupilW, pupilH, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(pupilR))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(2, 2, pupilW - 4, pupilH - 4);
                    g.SetClip(gp);

                    int srcX = 576 - pupilW / 2;
                    int srcY = 305 - pupilH / 2;
                    g.DrawImage(idle, new Rectangle(0, 0, pupilW, pupilH), new Rectangle(srcX, srcY, pupilW, pupilH), GraphicsUnit.Pixel);
                }
                pupilR.Save(Path.Combine(spritesDir, "pupil_r.png"), ImageFormat.Png);
            }

            // 3. Create Clean idle_sclera.png (filling eye sockets with natural anime sclera gradient)
            using (Bitmap sclera = new Bitmap(idle))
            using (Graphics g = Graphics.FromImage(sclera))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color scleraTop = Color.FromArgb(255, 230, 226, 238);
                Color scleraBottom = Color.FromArgb(255, 255, 255, 255);

                // Left socket
                Rectangle rL = new Rectangle(447 - 35, 300 - 36, 70, 72);
                using (GraphicsPath gpL = new GraphicsPath())
                {
                    gpL.AddEllipse(rL);
                    using (LinearGradientBrush b = new LinearGradientBrush(rL, scleraTop, scleraBottom, 90f))
                    {
                        g.FillPath(b, gpL);
                    }
                }

                // Right socket
                Rectangle rR = new Rectangle(576 - 35, 305 - 36, 70, 72);
                using (GraphicsPath gpR = new GraphicsPath())
                {
                    gpR.AddEllipse(rR);
                    using (LinearGradientBrush b = new LinearGradientBrush(rR, scleraTop, scleraBottom, 90f))
                    {
                        g.FillPath(b, gpR);
                    }
                }

                sclera.Save(Path.Combine(spritesDir, "idle_sclera.png"), ImageFormat.Png);
            }

            // 4. Create eyelashes.png (upper lash line and front bangs that clip over the eyes)
            using (Bitmap lashes = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                // Only copy the upper eyelash and bangs region above the eye sockets (y: 250 to 300)
                for (int y = 250; y < 315; y++)
                {
                    for (int x = 390; x < 635; x++)
                    {
                        Color c = idle.GetPixel(x, y);
                        // Eyelash dark colors (dark maroon / purple / black) OR bangs hair (purple/moss green)
                        bool isLashOrHair = (c.A > 200) && ((c.R < 80 && c.G < 60 && c.B < 65) || (c.R > 80 && c.B > 100) || (c.G > 100 && c.B < 90));
                        // Exclude the bright yellow star or green iris
                        bool isPupil = (c.R > 180 && c.G > 180 && c.B < 120);
                        if (isLashOrHair && !isPupil)
                        {
                            lashes.SetPixel(x, y, c);
                        }
                    }
                }
                lashes.Save(Path.Combine(spritesDir, "eyelashes.png"), ImageFormat.Png);
            }
        }
    }
}
