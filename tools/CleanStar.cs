using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class CleanStar
{
    public static void Main()
    {
        string propPath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\prop_star.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap src = new Bitmap(propPath))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            // The star outline in prop_star:
            // Top point is around (210, 25).
            // Right point is around (305, 170).
            // Bottom right point is around (235, 295).
            // Left hand hugs around (130, 215).
            // Right hand hugs around (265, 225).
            // Left point is around (45, 95).
            
            // Let's create a smooth polygon for the star + hugging hands
            PointF[] starPoints = new PointF[] {
                new PointF(210, 22),   // top tip
                new PointF(238, 70),
                new PointF(265, 120),
                new PointF(305, 168),  // right tip
                new PointF(275, 210),  // right hand
                new PointF(285, 235),
                new PointF(260, 250),
                new PointF(238, 298),  // bottom-right tip
                new PointF(185, 260),
                new PointF(105, 270),  // bottom-left tip / under sleeve
                new PointF(110, 210),  // left hand
                new PointF(42, 98),    // left tip
                new PointF(110, 85),
                new PointF(160, 80)
            };

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddClosedCurve(starPoints, 0.25f);
                using (Graphics g = Graphics.FromImage(res))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.SetClip(path);
                    g.DrawImage(src, 0, 0);
                }
            }

            res.Save(System.IO.Path.Combine(outDir, "clean_star_polygon.png"), ImageFormat.Png);
            Console.WriteLine("Clean star polygon saved!");
        }
    }
}
