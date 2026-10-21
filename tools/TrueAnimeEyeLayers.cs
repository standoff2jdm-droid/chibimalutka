using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class TrueAnimeEyeLayers
{
    public static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap idle = new Bitmap(idlePath))
        {
            // Left Iris center: (447, 275), size ~ 86 x 96
            // Right Iris center: (574, 275), size ~ 86 x 96
            
            // 1. Extract Left Iris with smooth alpha edge
            Bitmap irisL = ExtractCompleteIris(idle, 447, 275, 84, 96);
            irisL.Save(System.IO.Path.Combine(outDir, "anime_iris_L.png"), ImageFormat.Png);

            // 2. Extract Right Iris with smooth alpha edge
            Bitmap irisR = ExtractCompleteIris(idle, 574, 275, 84, 96);
            irisR.Save(System.IO.Path.Combine(outDir, "anime_iris_R.png"), ImageFormat.Png);

            // 3. Create the Sclera Base:
            // Same as idle.png, but inside the eye sockets, fill the area under the iris with smooth anime sclera
            // (soft white #FEFEFE with subtle shadow #ECEAF2 near top eyelid)
            Bitmap scleraBase = new Bitmap(idle);
            FillEyeSocketSclera(scleraBase, 447, 275);
            FillEyeSocketSclera(scleraBase, 574, 275);
            scleraBase.Save(System.IO.Path.Combine(outDir, "anime_sclera_base.png"), ImageFormat.Png);

            // 4. Create the Eyelash & Hair Overlay:
            // Extract ONLY the dark eyeliner, eyelashes, and hair bangs that cover the top/sides of the eyes
            Bitmap overlay = ExtractEyeOverlay(idle);
            overlay.Save(System.IO.Path.Combine(outDir, "anime_eyelashes_overlay.png"), ImageFormat.Png);

            // 5. TEST COMPOSITING: Look Left! (dx = -8 in 1024 space)
            Bitmap testLeft = CompositeGaze(scleraBase, irisL, irisR, overlay, -10, 0);
            SaveCrop(testLeft, System.IO.Path.Combine(outDir, "test_true_look_left.png"));

            // 6. TEST COMPOSITING: Look Right! (dx = +8 in 1024 space)
            Bitmap testRight = CompositeGaze(scleraBase, irisL, irisR, overlay, 10, 0);
            SaveCrop(testRight, System.IO.Path.Combine(outDir, "test_true_look_right.png"));

            // 7. TEST COMPOSITING: Look Center (dx = 0, dy = 0) - MUST BE 100% IDENTICAL TO IDLE.PNG!
            Bitmap testCenter = CompositeGaze(scleraBase, irisL, irisR, overlay, 0, 0);
            SaveCrop(testCenter, System.IO.Path.Combine(outDir, "test_true_look_center.png"));

            irisL.Dispose();
            irisR.Dispose();
            scleraBase.Dispose();
            overlay.Dispose();
            testLeft.Dispose();
            testRight.Dispose();
            testCenter.Dispose();

            Console.WriteLine("True anime eye layers generated!");
        }
    }

    private static Bitmap ExtractCompleteIris(Bitmap src, int cx, int cy, int w, int h)
    {
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

                // Normalized distance inside ellipse: (dx / halfW)^2 + (dy / halfH)^2
                double nx = (x - halfW) / (double)halfW;
                double ny = (y - halfH) / (double)halfH;
                double distSq = nx * nx + ny * ny;

                if (distSq <= 1.0)
                {
                    // Smooth antialiased boundary (fade over outer 10%)
                    float alpha = 1.0f;
                    if (distSq > 0.85)
                    {
                        alpha = (float)((1.0 - distSq) / 0.15);
                    }
                    alpha = Math.Max(0.0f, Math.Min(1.0f, alpha));

                    res.SetPixel(x, y, Color.FromArgb((int)(alpha * 255), c.R, c.G, c.B));
                }
                else
                {
                    res.SetPixel(x, y, Color.Transparent);
                }
            }
        }
        return res;
    }

    private static void FillEyeSocketSclera(Bitmap bmp, int cx, int cy)
    {
        // Fill inside eye socket:
        // Soft gradient: top is shadowed (#ECEAF2), bottom is pure white (#FFFFFF)
        int w = 78, h = 88;
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(cx - w / 2, cy - h / 2, w, h);
                using (LinearGradientBrush lgb = new LinearGradientBrush(
                    new Point(cx, cy - h / 2),
                    new Point(cx, cy + h / 2),
                    Color.FromArgb(255, 232, 230, 240), // Eyelid shadow on sclera
                    Color.FromArgb(255, 255, 255, 255)  // Bright lower sclera
                ))
                {
                    g.FillPath(lgb, gp);
                }
            }
        }
    }

    private static Bitmap ExtractEyeOverlay(Bitmap src)
    {
        // Full 1024x1024 overlay so NO coordinate distortion or scale mismatch can ever happen!
        Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

        // Region around both eyes: X: 370..650, Y: 180..340
        for (int y = 180; y < 340; y++)
        {
            for (int x = 370; x < 650; x++)
            {
                Color c = src.GetPixel(x, y);

                // Is this pixel part of eyelashes, eyeliner, bangs, or upper skin fold?
                // 1. Dark eyeliner / eyelashes: (R < 90 && G < 70 && B < 80)
                bool isEyelash = (c.R < 95 && c.G < 75 && c.B < 85);
                // 2. Purple hair bangs crossing eyes: (B > 110 && B > G)
                bool isHair = (c.B > 100 && c.R > 80 && (c.B >= c.G || c.R > c.G + 15));
                // 3. Eyelid skin fold / crease above eye: Y < 235
                bool isUpperLid = (y < 235 && (c.R > 220 || isEyelash));

                if (isEyelash || isHair || isUpperLid)
                {
                    res.SetPixel(x, y, c);
                }
            }
        }
        return res;
    }

    private static Bitmap CompositeGaze(Bitmap scleraBase, Bitmap irisL, Bitmap irisR, Bitmap overlay, int dx, int dy)
    {
        Bitmap res = new Bitmap(scleraBase);
        using (Graphics g = Graphics.FromImage(res))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Left Iris at (447 + dx, 275 + dy)
            g.DrawImage(irisL, 447 + dx - irisL.Width / 2, 275 + dy - irisL.Height / 2);

            // Draw Right Iris at (574 + dx, 275 + dy)
            g.DrawImage(irisR, 574 + dx - irisR.Width / 2, 275 + dy - irisR.Height / 2);

            // Draw Eyelashes & Hair Overlay on top
            g.DrawImage(overlay, 0, 0);
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
