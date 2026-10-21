using System;
using System.Drawing;
using System.Drawing.Imaging;

public class PerfectStarMask
{
    public static void Main()
    {
        string propPath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\prop_star.png";
        string outDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        using (Bitmap src = new Bitmap(propPath))
        using (Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);

                    // Exclude top-left region where chin, neck and collar are
                    if (y < 95 && x < 155) { res.SetPixel(x, y, Color.Transparent); continue; }
                    if (y < 70 && x < 190) { res.SetPixel(x, y, Color.Transparent); continue; }
                    // Exclude hair on right
                    if (x > 240 && y < 110) { res.SetPixel(x, y, Color.Transparent); continue; }
                    // Exclude sweater on bottom-left outside cuff
                    if (x < 95 && y > 180) { res.SetPixel(x, y, Color.Transparent); continue; }

                    // Check if pixel belongs to star, blush, face, hands
                    bool isYellow = (c.R > 180 && c.G > 140 && c.B < 155 && c.R >= c.G && c.G > c.B);
                    bool isHighlight = (c.R > 235 && c.G > 235 && c.B > 180);
                    bool isStarFace = (c.R < 110 && c.G < 80 && c.B < 70);
                    bool isBlush = (c.R > 210 && c.G > 120 && c.B > 120 && c.G < 180);
                    bool isHand = (c.R > 215 && c.G > 170 && c.B > 155 && c.R > c.B + 30);
                    bool isCuff = (c.B > c.R && c.R > 90 && c.B > 130 && x < 145 && y > 180);
                    bool isDarkOutline = (c.R < 125 && c.G < 105 && c.B < 95 && Math.Abs(c.R - c.G) < 35);

                    // Hair rejection
                    bool isHair = (c.G > c.R && c.G > c.B);

                    if ((isYellow || isHighlight || isStarFace || isBlush || isHand || isCuff || isDarkOutline) && !isHair)
                    {
                        res.SetPixel(x, y, c);
                    }
                    else
                    {
                        res.SetPixel(x, y, Color.Transparent);
                    }
                }
            }

            res.Save(System.IO.Path.Combine(outDir, "perfect_star_mask.png"), ImageFormat.Png);
            Console.WriteLine("Perfect star mask saved!");
        }
    }
}
