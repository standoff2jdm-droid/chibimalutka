using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public class InspectEyeSocket
{
    public static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap idle = new Bitmap(idlePath))
        {
            // Left eye:
            // Sclera + Iris is within an ellipse/almond shape:
            // Center around (447, 275), W: ~110, H: ~105
            // Right eye:
            // Center around (574, 275), W: ~110, H: ~105
            
            // Let's create an exact test where we move the iris & star!
            // In idle.png, let's extract:
            // 1. Left Iris + Star (disc around 447, 278, radius ~42)
            // 2. Right Iris + Star (disc around 574, 278, radius ~42)
            
            Bitmap leftIris = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(leftIris))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(8, 8, 84, 84);
                    g.SetClip(gp);
                    // Draw from idle at (447 - 50, 278 - 50)
                    g.DrawImage(idle, new Rectangle(0, 0, 100, 100), new Rectangle(397, 228, 100, 100), GraphicsUnit.Pixel);
                }
            }
            leftIris.Save(System.IO.Path.Combine(outDir, "test_left_iris.png"), ImageFormat.Png);
            leftIris.Dispose();
            
            Console.WriteLine("Left iris extracted!");
        }
    }
}
