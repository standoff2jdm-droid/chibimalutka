using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class ExactStarContour
{
    public static void Main()
    {
        string propPath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\prop_star.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap src = new Bitmap(propPath))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            PointF[] pts = new PointF[] {
                new PointF(212, 15),   // Top tip
                new PointF(224, 45),
                new PointF(235, 78),   // Top-right inner crotch
                new PointF(270, 120),
                new PointF(308, 168),  // Right tip
                new PointF(295, 205),
                new PointF(290, 245),  // Around right fingers
                new PointF(265, 255),
                new PointF(238, 305),  // Bottom-right tip
                new PointF(185, 265),  // Bottom inner crotch
                new PointF(125, 268),
                new PointF(102, 255),  // Bottom of left cuff
                new PointF(102, 190),  // Left side of left cuff
                new PointF(118, 180),
                new PointF(95, 145),
                new PointF(42, 95),    // Left tip
                new PointF(85, 87),
                new PointF(135, 78),   // Top-left inner crotch
                new PointF(175, 48)
            };

            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddPolygon(pts);
                using (Graphics g = Graphics.FromImage(res))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.SetClip(gp);
                    g.DrawImage(src, 0, 0);
                }
            }

            // Outline feathering and clean border pen
            using (Graphics g = Graphics.FromImage(res))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color borderCol = Color.FromArgb(200, 60, 45, 30);
                using (Pen p = new Pen(borderCol, 2.5f))
                {
                    // Draw outer border line to guarantee crisp anime ink contour
                    g.DrawPolygon(p, pts);
                }
            }

            res.Save(System.IO.Path.Combine(outDir, "exact_star_contour.png"), ImageFormat.Png);
            Console.WriteLine("Exact star contour saved!");
        }
    }
}
