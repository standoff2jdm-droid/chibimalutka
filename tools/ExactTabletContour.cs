using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class ExactTabletContour
{
    public static void Main()
    {
        string propPath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\prop_tablet.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap src = new Bitmap(propPath))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            // Contour around Tablet, stylus pen and hands
            PointF[] pts = new PointF[] {
                // Stylus tip & shaft
                new PointF(66, 35),    // Top tip of stylus pen
                new PointF(82, 45),
                new PointF(86, 75),    // Stylus meeting left hand thumb
                new PointF(108, 92),
                new PointF(116, 115),  // Hand near tablet surface
                new PointF(122, 126),  // Stylus touching tablet
                new PointF(122, 115),  // Tablet top border going right
                new PointF(225, 115),  // Tablet meeting right hand
                new PointF(238, 90),   // Right wrist
                new PointF(270, 95),   // Right sleeve
                new PointF(276, 130),
                new PointF(270, 160),  // Right sleeve bottom
                new PointF(262, 178),  // Tablet bottom-right corner
                new PointF(25, 178),   // Tablet bottom-left corner
                new PointF(15, 150),   // Left sleeve bottom
                new PointF(2, 115),    // Left sleeve outer
                new PointF(15, 80),    // Left sleeve top
                new PointF(52, 70),    // Left wrist
                new PointF(60, 52)     // Stylus shaft
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

            using (Graphics g = Graphics.FromImage(res))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color borderCol = Color.FromArgb(240, 20, 15, 25);
                using (Pen p = new Pen(borderCol, 2.5f))
                {
                    g.DrawPolygon(p, pts);
                }
            }

            res.Save(System.IO.Path.Combine(outDir, "exact_tablet_contour.png"), ImageFormat.Png);
            Console.WriteLine("Exact tablet contour saved!");
        }
    }
}
