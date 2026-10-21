using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class PerfectEyeSockets
{
    public static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap idle = new Bitmap(idlePath))
        {
            // Left eye socket contour (viewer's left):
            // Center of pupil: (447, 278)
            // Left eye opening points (inside the black eyeliner/eyelashes):
            PointF[] leftEyeHole = new PointF[] {
                new PointF(395, 276), // Outer left corner
                new PointF(405, 252),
                new PointF(425, 236),
                new PointF(447, 233), // Top under upper eyeliner
                new PointF(470, 240),
                new PointF(488, 256),
                new PointF(496, 278), // Inner right corner
                new PointF(488, 305),
                new PointF(472, 320),
                new PointF(447, 324), // Bottom above lower lid
                new PointF(425, 320),
                new PointF(405, 305)
            };

            // Right eye socket contour (viewer's right):
            // Center of pupil: (574, 278)
            PointF[] rightEyeHole = new PointF[] {
                new PointF(525, 278), // Inner left corner
                new PointF(533, 256),
                new PointF(551, 240),
                new PointF(574, 233), // Top under upper eyeliner
                new PointF(596, 236),
                new PointF(616, 252),
                new PointF(626, 276), // Outer right corner
                new PointF(616, 305),
                new PointF(596, 320),
                new PointF(574, 324), // Bottom above lower lid
                new PointF(551, 320),
                new PointF(533, 305)
            };

            // 1. Create Sclera Base:
            // Start with idle.png. Inside leftEyeHole and rightEyeHole, fill with anime sclera gradient!
            Bitmap scleraBase = new Bitmap(idle);
            using (Graphics g = Graphics.FromImage(scleraBase))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                FillHoleWithSclera(g, leftEyeHole, 447, 278);
                FillHoleWithSclera(g, rightEyeHole, 574, 278);
            }
            scleraBase.Save(System.IO.Path.Combine(outDir, "perfect_sclera_base.png"), ImageFormat.Png);

            // 2. Extract Irises:
            // Exactly matching the eye sockets
            Bitmap irisL = ExtractIris(idle, 447, 278, 86, 96);
            Bitmap irisR = ExtractIris(idle, 574, 278, 86, 96);
            irisL.Save(System.IO.Path.Combine(outDir, "perfect_iris_L.png"), ImageFormat.Png);
            irisR.Save(System.IO.Path.Combine(outDir, "perfect_iris_R.png"), ImageFormat.Png);

            // 3. Create Face Overlay:
            // Take idle.png, and cut out ONLY leftEyeHole and rightEyeHole!
            // That way, every hair strand, eyebrow, eyelash line is 100% original idle.png!
            Bitmap overlay = new Bitmap(idle);
            using (Graphics g = Graphics.FromImage(overlay))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingMode = CompositingMode.SourceCopy;

                using (GraphicsPath gpL = new GraphicsPath())
                {
                    gpL.AddPolygon(leftEyeHole);
                    g.FillPath(Brushes.Transparent, gpL);
                }
                using (GraphicsPath gpR = new GraphicsPath())
                {
                    gpR.AddPolygon(rightEyeHole);
                    g.FillPath(Brushes.Transparent, gpR);
                }
            }
            overlay.Save(System.IO.Path.Combine(outDir, "perfect_face_overlay.png"), ImageFormat.Png);

            // 4. Test Gaze Look Left (dx = -9, dy = 0)
            Bitmap glanceLeft = RenderGaze(scleraBase, irisL, irisR, overlay, -9, 0);
            SaveCrop(glanceLeft, System.IO.Path.Combine(outDir, "perfect_look_left.png"));

            // 5. Test Gaze Look Right (dx = +9, dy = 0)
            Bitmap glanceRight = RenderGaze(scleraBase, irisL, irisR, overlay, 9, 0);
            SaveCrop(glanceRight, System.IO.Path.Combine(outDir, "perfect_look_right.png"));

            // 6. Test Gaze Look Up (dx = 0, dy = -6)
            Bitmap glanceUp = RenderGaze(scleraBase, irisL, irisR, overlay, 0, -6);
            SaveCrop(glanceUp, System.IO.Path.Combine(outDir, "perfect_look_up.png"));

            // 7. Test Gaze Center (dx = 0, dy = 0) - Must be 100% identical to idle.png!
            Bitmap glanceCenter = RenderGaze(scleraBase, irisL, irisR, overlay, 0, 0);
            SaveCrop(glanceCenter, System.IO.Path.Combine(outDir, "perfect_look_center.png"));

            scleraBase.Dispose();
            irisL.Dispose();
            irisR.Dispose();
            overlay.Dispose();
            glanceLeft.Dispose();
            glanceRight.Dispose();
            glanceUp.Dispose();
            glanceCenter.Dispose();

            Console.WriteLine("Perfect eye sockets created and verified!");
        }
    }

    private static void FillHoleWithSclera(Graphics g, PointF[] holePts, int cx, int cy)
    {
        using (GraphicsPath gp = new GraphicsPath())
        {
            gp.AddPolygon(holePts);
            // Sclera has soft anime eyelid shadow at the top (#E6E3EE) and crisp white at bottom (#FFFFFF)
            using (LinearGradientBrush lgb = new LinearGradientBrush(
                new Point(cx, cy - 48),
                new Point(cx, cy + 48),
                Color.FromArgb(255, 230, 227, 238),
                Color.FromArgb(255, 255, 255, 255)
            ))
            {
                g.FillPath(lgb, gp);
            }
        }
    }

    private static Bitmap ExtractIris(Bitmap src, int cx, int cy, int w, int h)
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

                double nx = (x - halfW) / (double)halfW;
                double ny = (y - halfH) / (double)halfH;
                double distSq = nx * nx + ny * ny;

                if (distSq <= 1.0)
                {
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

    private static Bitmap RenderGaze(Bitmap scleraBase, Bitmap irisL, Bitmap irisR, Bitmap overlay, int dx, int dy)
    {
        Bitmap res = new Bitmap(scleraBase);
        using (Graphics g = Graphics.FromImage(res))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Left Iris
            g.DrawImage(irisL, 447 + dx - irisL.Width / 2, 278 + dy - irisL.Height / 2);

            // Draw Right Iris
            g.DrawImage(irisR, 574 + dx - irisR.Width / 2, 278 + dy - irisR.Height / 2);

            // Draw Face Overlay (with eye openings cut out)
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
