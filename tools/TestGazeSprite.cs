using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class TestGazeSprite
{
    public static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap idle = new Bitmap(idlePath))
        {
            // Create Look Left (dx = -6)
            Bitmap lookLeft = ShiftEyeGaze(idle, -7, 0);
            lookLeft.Save(System.IO.Path.Combine(outDir, "gaze_look_left.png"), ImageFormat.Png);
            SaveCrop(lookLeft, System.IO.Path.Combine(outDir, "crop_gaze_look_left.png"));

            // Create Look Right (dx = +6)
            Bitmap lookRight = ShiftEyeGaze(idle, 7, 0);
            lookRight.Save(System.IO.Path.Combine(outDir, "gaze_look_right.png"), ImageFormat.Png);
            SaveCrop(lookRight, System.IO.Path.Combine(outDir, "crop_gaze_look_right.png"));

            lookLeft.Dispose();
            lookRight.Dispose();
            Console.WriteLine("Gaze frames generated!");
        }
    }

    private static Bitmap ShiftEyeGaze(Bitmap src, int dx, int dy)
    {
        Bitmap res = new Bitmap(src);

        // We only modify the eye regions:
        // Left eye box: X: 395..500, Y: 225..325
        // Right eye box: X: 520..628, Y: 225..325
        ProcessSingleEye(src, res, 447, 278, dx, dy);
        ProcessSingleEye(src, res, 574, 278, dx, dy);

        return res;
    }

    private static void ProcessSingleEye(Bitmap src, Bitmap dest, int cx, int cy, int dx, int dy)
    {
        // 1. Isolate the iris from src:
        // Iris radius is around 40px
        int w = 84, h = 92;
        Bitmap iris = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sx = cx - w / 2 + x;
                int sy = cy - h / 2 + y;
                Color c = src.GetPixel(sx, sy);

                // Is it part of the iris or star or highlight?
                bool isGreenIris = (c.G > 85 && c.G > c.B && c.R > 75 && c.R < 250);
                bool isStarYellow = (c.R > 180 && c.G > 160 && c.B < 150);
                bool isSpecular = (c.R > 230 && c.G > 230 && c.B > 200 && sx > cx - 25 && sy < cy);

                // Normalized radius
                double nx = (x - w / 2.0) / (w / 2.0);
                double ny = (y - h / 2.0) / (h / 2.0);
                double dist = nx * nx + ny * ny;

                if ((isGreenIris || isStarYellow || isSpecular) && dist <= 1.0)
                {
                    float a = 1.0f;
                    if (dist > 0.88) a = (float)((1.0 - dist) / 0.12);
                    iris.SetPixel(x, y, Color.FromArgb((int)(Math.Min(1.0f, Math.Max(0.0f, a)) * 255), c.R, c.G, c.B));
                }
                else
                {
                    iris.SetPixel(x, y, Color.Transparent);
                }
            }
        }

        // 2. In dest, fill the region vacated by iris with the sclera color of that eye
        // Sample sclera color from the corner of this eye:
        Color scleraTop = src.GetPixel(cx + (dx > 0 ? -36 : 36), cy - 20);
        Color scleraBot = src.GetPixel(cx + (dx > 0 ? -36 : 36), cy + 20);

        using (Graphics g = Graphics.FromImage(dest))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Fill the vacated side with a soft crescent of sclera
            int fillX = (dx < 0 ? cx + w / 2 - Math.Abs(dx) - 4 : cx - w / 2);
            Rectangle fillRect = new Rectangle(fillX, cy - h / 2 + 8, Math.Abs(dx) + 8, h - 16);
            using (LinearGradientBrush lgb = new LinearGradientBrush(fillRect, scleraTop, scleraBot, LinearGradientMode.Vertical))
            {
                g.FillEllipse(lgb, fillRect);
            }

            // Draw the shifted iris
            g.DrawImage(iris, cx + dx - w / 2, cy + dy - h / 2);

            // Redraw upper eyelashes and hair bangs from src to guarantee 100% clean overlap
            for (int y = cy - h / 2 - 10; y <= cy - 10; y++)
            {
                for (int x = cx - w / 2 - 10; x <= cx + w / 2 + 10; x++)
                {
                    Color c = src.GetPixel(x, y);
                    // Dark eyelash: R < 95, G < 75, B < 85
                    bool isEyelash = (c.R < 95 && c.G < 75 && c.B < 85);
                    // Purple bangs: B > 100 && R > 80 && B >= G
                    bool isHair = (c.B > 100 && c.R > 80 && (c.B >= c.G || c.R > c.G + 15));
                    if (isEyelash || isHair)
                    {
                        dest.SetPixel(x, y, c);
                    }
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
