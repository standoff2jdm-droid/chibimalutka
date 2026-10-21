using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class RefineStar
{
    public static void Main()
    {
        string propPath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\prop_star.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap src = new Bitmap(propPath))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            // Precise star contour with hands
            PointF[] starPoints = new PointF[] {
                new PointF(212, 16),   // top tip
                new PointF(235, 80),   // top-right crotch
                new PointF(275, 125),
                new PointF(308, 168),  // right tip
                new PointF(275, 210),  // right hand
                new PointF(285, 235),
                new PointF(255, 255),
                new PointF(238, 305),  // bottom-right tip
                new PointF(185, 265),  // bottom crotch
                new PointF(108, 275),  // bottom-left tip
                new PointF(105, 230),  // left cuff base
                new PointF(125, 180),  // left hand
                new PointF(40, 95),    // left tip
                new PointF(110, 82),
                new PointF(138, 76)    // top-left crotch
            };

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddClosedCurve(starPoints, 0.15f);
                using (Graphics g = Graphics.FromImage(res))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.SetClip(path);
                    g.DrawImage(src, 0, 0);
                }
            }

            // Also draw a clean soft dark line around the star where clip meets boundary if needed
            res.Save(System.IO.Path.Combine(outDir, "refined_star_plush.png"), ImageFormat.Png);
            Console.WriteLine("Refined star plush saved!");
        }
    }
}
