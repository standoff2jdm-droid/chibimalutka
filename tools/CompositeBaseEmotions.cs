using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public class CompositeBaseEmotions
{
    public static void Main()
    {
        string spritesDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
        string baseDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        string idlePath = Path.Combine(spritesDir, "idle.png");
        string sitPath = Path.Combine(spritesDir, "sit.png");
        string blinkPath = Path.Combine(spritesDir, "blink.png");

        Console.WriteLine("1. Creating console.png based directly on sit.png (Base Style)...");
        CreateConsole(sitPath, Path.Combine(spritesDir, "prop_switch.png"), Path.Combine(spritesDir, "console.png"));

        Console.WriteLine("2. Creating drawing.png based directly on sit.png (Base Style)...");
        CreateDrawing(sitPath, Path.Combine(spritesDir, "prop_tablet.png"), Path.Combine(spritesDir, "drawing.png"));

        Console.WriteLine("3. Creating sleep.png based directly on sit.png + blink.png (Base Style)...");
        CreateSleep(sitPath, blinkPath, Path.Combine(spritesDir, "prop_star.png"), Path.Combine(spritesDir, "sleep.png"));

        Console.WriteLine("4. Creating pout.png based directly on idle.png (Base Style)...");
        CreatePout(idlePath, Path.Combine(spritesDir, "prop_pout_mouth.png"), Path.Combine(spritesDir, "prop_pout_arms.png"), Path.Combine(spritesDir, "pout.png"));

        Console.WriteLine("All 4 Base-Style emotion sprites successfully created!");
    }

    private static void CreateConsole(string sitPath, string switchPropPath, string outPath)
    {
        using (Bitmap sit = new Bitmap(sitPath))
        using (Bitmap swProp = new Bitmap(switchPropPath))
        {
            Bitmap res = new Bitmap(sit);
            using (Graphics g = Graphics.FromImage(res))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Make outer background of swProp transparent
                Bitmap cleanProp = CutoutInnerProp(swProp, new Rectangle(30, 20, 220, 120));
                // Draw Switch held in hands on lap in front of sweater
                g.DrawImage(cleanProp, new Rectangle(372, 530, cleanProp.Width, cleanProp.Height));
                cleanProp.Dispose();
            }
            res.Save(outPath, ImageFormat.Png);
            res.Dispose();
        }
    }

    private static void CreateDrawing(string sitPath, string tabletPropPath, string outPath)
    {
        using (Bitmap sit = new Bitmap(sitPath))
        using (Bitmap tabProp = new Bitmap(tabletPropPath))
        {
            Bitmap res = new Bitmap(sit);
            using (Graphics g = Graphics.FromImage(res))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Bitmap cleanProp = CutoutInnerProp(tabProp, new Rectangle(30, 15, 230, 155));
                g.DrawImage(cleanProp, new Rectangle(372, 530, cleanProp.Width, cleanProp.Height));
                cleanProp.Dispose();
            }
            res.Save(outPath, ImageFormat.Png);
            res.Dispose();
        }
    }

    private static void CreateSleep(string sitPath, string blinkPath, string starPropPath, string outPath)
    {
        using (Bitmap sit = new Bitmap(sitPath))
        using (Bitmap blink = new Bitmap(blinkPath))
        using (Bitmap starProp = new Bitmap(starPropPath))
        {
            Bitmap res = new Bitmap(sit);
            using (Graphics g = Graphics.FromImage(res))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. Copy peaceful sleeping eyes from blink.png onto sit.png
                // Eye region: x: 375..650, y: 260..335
                Rectangle eyeRect = new Rectangle(375, 255, 275, 80);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(eyeRect);
                    g.SetClip(gp);
                    g.DrawImage(blink, eyeRect, eyeRect, GraphicsUnit.Pixel);
                    g.ResetClip();
                }

                // 2. Add cute smiling plush yellow star hugged in front of sweater paws
                Bitmap cleanStar = CutoutStarPlush(starProp);
                // Scale star plushie to fit snugly in lap
                int starW = (int)(cleanStar.Width * 0.85);
                int starH = (int)(cleanStar.Height * 0.85);
                g.DrawImage(cleanStar, new Rectangle(400, 480, starW, starH));
                cleanStar.Dispose();
            }
            res.Save(outPath, ImageFormat.Png);
            res.Dispose();
        }
    }

    private static void CreatePout(string idlePath, string mouthPropPath, string armsPropPath, string outPath)
    {
        using (Bitmap idle = new Bitmap(idlePath))
        using (Bitmap mouthProp = new Bitmap(mouthPropPath))
        using (Bitmap armsProp = new Bitmap(armsPropPath))
        {
            Bitmap res = new Bitmap(idle);
            using (Graphics g = Graphics.FromImage(res))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. Pout mouth
                Bitmap cleanMouth = CutoutMouth(mouthProp);
                g.DrawImage(cleanMouth, new Rectangle(465, 332, cleanMouth.Width, cleanMouth.Height));
                cleanMouth.Dispose();

                // 2. Extra cute blush marks on cheeks
                Color blush = Color.FromArgb(160, 245, 120, 140);
                using (Pen p = new Pen(blush, 2.5f))
                {
                    // Left cheek blush lines
                    g.DrawLine(p, 410, 318, 418, 332);
                    g.DrawLine(p, 420, 318, 428, 332);
                    g.DrawLine(p, 430, 320, 437, 332);

                    // Right cheek blush lines
                    g.DrawLine(p, 595, 320, 603, 332);
                    g.DrawLine(p, 605, 318, 613, 332);
                    g.DrawLine(p, 615, 318, 623, 332);
                }

                // 3. Cute pouting eyebrows (knitted slightly downward)
                Color browCol = Color.FromArgb(240, 50, 40, 55);
                using (Pen pb = new Pen(browCol, 3.2f))
                {
                    pb.StartCap = LineCap.Round;
                    pb.EndCap = LineCap.Round;
                    // Left brow (slanted determined)
                    g.DrawLine(pb, 420, 248, 465, 255);
                    // Right brow (slanted determined)
                    g.DrawLine(pb, 605, 248, 560, 255);
                }

                // 4. Crossed sweater arms in front of sweater
                Bitmap cleanArms = CutoutArms(armsProp);
                g.DrawImage(cleanArms, new Rectangle(362, 490, cleanArms.Width, cleanArms.Height));
                cleanArms.Dispose();
            }
            res.Save(outPath, ImageFormat.Png);
            res.Dispose();
        }
    }

    private static Bitmap CutoutInnerProp(Bitmap src, Rectangle keepRect)
    {
        Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                if (keepRect.Contains(x, y))
                {
                    Color c = src.GetPixel(x, y);
                    // Fade edge near keepRect bounds
                    int distFromEdge = Math.Min(
                        Math.Min(x - keepRect.Left, keepRect.Right - x),
                        Math.Min(y - keepRect.Top, keepRect.Bottom - y)
                    );
                    float alpha = Math.Min(1.0f, distFromEdge / 12.0f);
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

    private static Bitmap CutoutStarPlush(Bitmap src)
    {
        Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        // Cut out star plushie (yellow tones + hands + star face)
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                Color c = src.GetPixel(x, y);
                // Yellow star plushie (R>180, G>140, B<120) OR hands/skin (R>230, G>180, B>170) OR dark border
                bool isStar = (c.R > 170 && c.G > 130 && c.B < 140);
                bool isSkin = (c.R > 215 && c.G > 165 && c.B > 160);
                bool isDarkLine = (c.R < 80 && c.G < 70 && c.B < 75 && (c.R + c.G + c.B) < 200);

                if (isStar || isSkin || isDarkLine)
                {
                    // Check if within star blob
                    if (x > 30 && x < 290 && y > 20 && y < 290)
                    {
                        res.SetPixel(x, y, c);
                    }
                }
            }
        }
        return res;
    }

    private static Bitmap CutoutMouth(Bitmap src)
    {
        Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                Color c = src.GetPixel(x, y);
                // Keep only the dark mouth line and subtle red crease
                bool isMouthLine = (c.R < 120 && c.G < 60 && c.B < 65);
                bool isLipShadow = (c.R > 210 && c.G < 165 && c.B < 165);
                if (isMouthLine || isLipShadow)
                {
                    res.SetPixel(x, y, c);
                }
            }
        }
        return res;
    }

    private static Bitmap CutoutArms(Bitmap src)
    {
        Bitmap res = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        Rectangle armArea = new Rectangle(20, 20, 260, 140);
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                if (armArea.Contains(x, y))
                {
                    Color c = src.GetPixel(x, y);
                    int distFromEdge = Math.Min(
                        Math.Min(x - armArea.Left, armArea.Right - x),
                        Math.Min(y - armArea.Top, armArea.Bottom - y)
                    );
                    float alpha = Math.Min(1.0f, distFromEdge / 10.0f);
                    res.SetPixel(x, y, Color.FromArgb((int)(alpha * 255), c.R, c.G, c.B));
                }
            }
        }
        return res;
    }
}
