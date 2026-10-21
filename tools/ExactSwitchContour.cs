using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class ExactSwitchContour
{
    public static void Main()
    {
        string propPath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\prop_switch.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap src = new Bitmap(propPath))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            // Contour around the Switch, Joy-Cons and hands
            PointF[] pts = new PointF[] {
                // Top edge of switch & thumbs
                new PointF(76, 45),
                new PointF(60, 44),
                new PointF(42, 54),
                new PointF(36, 70),
                new PointF(18, 85),
                new PointF(6, 115),
                new PointF(10, 145),
                new PointF(38, 158),
                new PointF(85, 154),
                new PointF(95, 146),  // Bottom of switch between hands
                new PointF(185, 146),
                new PointF(195, 154),
                new PointF(242, 158),
                new PointF(270, 145),
                new PointF(274, 115),
                new PointF(262, 85),
                new PointF(244, 70),
                new PointF(238, 54),
                new PointF(220, 44),
                new PointF(204, 45)
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

            // Draw crisp dark ink border along top and bottom lines of Switch
            using (Graphics g = Graphics.FromImage(res))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color borderCol = Color.FromArgb(240, 20, 15, 25);
                using (Pen p = new Pen(borderCol, 2.5f))
                {
                    g.DrawPolygon(p, pts);
                }
            }

            res.Save(System.IO.Path.Combine(outDir, "exact_switch_contour.png"), ImageFormat.Png);
            Console.WriteLine("Exact switch contour saved!");
        }
    }
}
