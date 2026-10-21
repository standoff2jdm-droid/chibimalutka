using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public class GenerateAllMasterEmotions
{
    public static void Main()
    {
        string spritesDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
        string brainDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

        string idlePath = Path.Combine(spritesDir, "idle.png");
        string sitPath = Path.Combine(spritesDir, "sit.png");
        string blinkPath = Path.Combine(spritesDir, "blink.png");

        Console.WriteLine("Generating Master Base-Style Emotion Sprites...");

        // 1. CONSOLE
        BuildMasterConsole(sitPath, Path.Combine(spritesDir, "prop_switch.png"), Path.Combine(spritesDir, "console.png"));

        // 2. DRAWING
        BuildMasterDrawing(sitPath, Path.Combine(spritesDir, "prop_tablet.png"), Path.Combine(spritesDir, "drawing.png"));

        // 3. SLEEP
        BuildMasterSleep(sitPath, blinkPath, Path.Combine(spritesDir, "prop_star.png"), Path.Combine(spritesDir, "sleep.png"));

        // 4. POUT
        BuildMasterPout(idlePath, Path.Combine(spritesDir, "pout.png"));

        // Previews
        MakePreview(Path.Combine(spritesDir, "console.png"), Path.Combine(brainDir, "final_preview_console.png"));
        MakePreview(Path.Combine(spritesDir, "drawing.png"), Path.Combine(brainDir, "final_preview_drawing.png"));
        MakePreview(Path.Combine(spritesDir, "sleep.png"), Path.Combine(brainDir, "final_preview_sleep.png"));
        MakePreview(Path.Combine(spritesDir, "pout.png"), Path.Combine(brainDir, "final_preview_pout.png"));

        Console.WriteLine("All 4 Master Emotion Sprites built and previews created successfully!");
    }

    private static void BuildMasterConsole(string sitPath, string propSwitchPath, string outPath)
    {
        using (Bitmap sit = new Bitmap(sitPath))
        using (Bitmap srcSwitch = new Bitmap(propSwitchPath))
        using (Bitmap res = new Bitmap(sit))
        {
            Bitmap cleanSwitch = new Bitmap(srcSwitch.Width, srcSwitch.Height, PixelFormat.Format32bppArgb);
            PointF[] pts = new PointF[] {
                new PointF(76, 45),
                new PointF(60, 44),
                new PointF(42, 54),
                new PointF(36, 70),
                new PointF(18, 85),
                new PointF(6, 115),
                new PointF(10, 145),
                new PointF(38, 158),
                new PointF(85, 154),
                new PointF(95, 146),
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
                using (Graphics g = Graphics.FromImage(cleanSwitch))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.SetClip(gp);
                    g.DrawImage(srcSwitch, 0, 0);
                    using (Pen p = new Pen(Color.FromArgb(240, 20, 15, 25), 2.2f))
                    {
                        g.DrawPolygon(p, pts);
                    }
                }
            }

            using (Graphics g = Graphics.FromImage(res))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(cleanSwitch, new Rectangle(372, 535, cleanSwitch.Width, cleanSwitch.Height));
            }

            cleanSwitch.Dispose();
            res.Save(outPath, ImageFormat.Png);
        }
    }

    private static void BuildMasterDrawing(string sitPath, string propTabletPath, string outPath)
    {
        using (Bitmap sit = new Bitmap(sitPath))
        using (Bitmap srcTablet = new Bitmap(propTabletPath))
        using (Bitmap res = new Bitmap(sit))
        {
            Bitmap cleanTablet = new Bitmap(srcTablet.Width, srcTablet.Height, PixelFormat.Format32bppArgb);
            PointF[] pts = new PointF[] {
                new PointF(60, 28),    // Stylus top cap
                new PointF(80, 42),
                new PointF(86, 75),    // Stylus meeting thumb
                new PointF(108, 92),
                new PointF(116, 115),
                new PointF(122, 126),  // Stylus on tablet
                new PointF(124, 116),  // Tablet top border (clean, no purple stripe)
                new PointF(225, 116),  // Tablet meeting right hand
                new PointF(238, 92),   // Right wrist
                new PointF(270, 95),   // Right sleeve
                new PointF(276, 130),
                new PointF(270, 160),
                new PointF(262, 178),  // Tablet bottom-right
                new PointF(25, 178),   // Tablet bottom-left
                new PointF(15, 150),
                new PointF(2, 115),
                new PointF(15, 80),
                new PointF(52, 68),
                new PointF(58, 48)
            };

            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddPolygon(pts);
                using (Graphics g = Graphics.FromImage(cleanTablet))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.SetClip(gp);
                    g.DrawImage(srcTablet, 0, 0);
                    using (Pen p = new Pen(Color.FromArgb(240, 20, 15, 25), 2.2f))
                    {
                        g.DrawPolygon(p, pts);
                    }
                }
            }

            using (Graphics g = Graphics.FromImage(res))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(cleanTablet, new Rectangle(372, 530, cleanTablet.Width, cleanTablet.Height));
            }

            cleanTablet.Dispose();
            res.Save(outPath, ImageFormat.Png);
        }
    }

    private static void BuildMasterSleep(string sitPath, string blinkPath, string propStarPath, string outPath)
    {
        using (Bitmap sit = new Bitmap(sitPath))
        using (Bitmap blink = new Bitmap(blinkPath))
        using (Bitmap srcStar = new Bitmap(propStarPath))
        using (Bitmap res = new Bitmap(sit))
        {
            // 1. Peaceful sleeping eyes from blink.png
            Rectangle eyeRect = new Rectangle(375, 255, 275, 80);
            using (Graphics g = Graphics.FromImage(res))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(eyeRect);
                    g.SetClip(gp);
                    g.DrawImage(blink, eyeRect, eyeRect, GraphicsUnit.Pixel);
                    g.ResetClip();
                }

                // 2. Full 5-ray star plushie with hugging hands - strictly excluding cheek and hair
                Bitmap cleanStar = new Bitmap(srcStar.Width, srcStar.Height, PixelFormat.Format32bppArgb);
                PointF[] pts = new PointF[] {
                    new PointF(210, 20),   // Top tip
                    new PointF(235, 75),   // Top-right inner crotch
                    new PointF(275, 120),
                    new PointF(308, 168),  // Right tip
                    new PointF(296, 192),
                    new PointF(285, 235),  // Right hand fingers
                    new PointF(265, 255),
                    new PointF(235, 305),  // Bottom-right ray
                    new PointF(180, 280),  // Bottom crotch
                    new PointF(116, 271),  // Bottom-left ray
                    new PointF(102, 250),  // Left cuff
                    new PointF(95, 190),   // Left sleeve
                    new PointF(125, 175),  // Left hand
                    new PointF(45, 95),    // Left ray
                    new PointF(100, 78),   // Top-left curve meeting cheek
                    new PointF(140, 65),
                    new PointF(180, 45)
                };

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddPolygon(pts);
                    using (Graphics gs = Graphics.FromImage(cleanStar))
                    {
                        gs.SmoothingMode = SmoothingMode.AntiAlias;
                        gs.SetClip(gp);
                        gs.DrawImage(srcStar, 0, 0);
                        using (Pen p = new Pen(Color.FromArgb(220, 60, 45, 30), 2.4f))
                        {
                            gs.DrawPolygon(p, pts);
                        }
                    }
                }

                // Scale star plushie to fit lap comfortably: 88%
                int starW = (int)(cleanStar.Width * 0.88);
                int starH = (int)(cleanStar.Height * 0.88);
                g.DrawImage(cleanStar, new Rectangle(395, 470, starW, starH));
                cleanStar.Dispose();
            }

            res.Save(outPath, ImageFormat.Png);
        }
    }

    private static void BuildMasterPout(string idlePath, string outPath)
    {
        using (Bitmap idle = new Bitmap(idlePath))
        using (Bitmap res = new Bitmap(idle))
        {
            using (Graphics g = Graphics.FromImage(res))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // 1. Cover original smile at X: 475..550, Y: 348..372 with skin tone
                Color skinBase = Color.FromArgb(255, 253, 230, 222);
                using (GraphicsPath mouthCover = new GraphicsPath())
                {
                    mouthCover.AddEllipse(474, 348, 76, 26);
                    using (PathGradientBrush pgb = new PathGradientBrush(mouthCover))
                    {
                        pgb.CenterColor = skinBase;
                        pgb.SurroundColors = new Color[] { skinBase };
                        g.FillPath(pgb, mouthCover);
                    }
                }

                // 2. Crisp anime pout mouth (・3・) at Y = 352
                Color darkInk = Color.FromArgb(255, 45, 30, 45);

                using (Pen penInk = new Pen(darkInk, 3.4f))
                {
                    penInk.StartCap = LineCap.Round;
                    penInk.EndCap = LineCap.Round;

                    // Cute pout mouth curve
                    g.DrawArc(penInk, 498, 350, 13, 12, 140, 200);
                    g.DrawArc(penInk, 509, 350, 13, 12, 200, 200);

                    // Lip crease shadow
                    using (Pen penShadow = new Pen(Color.FromArgb(180, 170, 95, 105), 2.2f))
                    {
                        penShadow.StartCap = LineCap.Round;
                        penShadow.EndCap = LineCap.Round;
                        g.DrawLine(penShadow, 506, 365, 516, 365);
                    }
                }

                // 3. Determined slightly-knitted inner eyebrows
                using (Pen browPen = new Pen(Color.FromArgb(240, 50, 40, 55), 3.2f))
                {
                    browPen.StartCap = LineCap.Round;
                    browPen.EndCap = LineCap.Round;
                    g.DrawLine(browPen, 435, 246, 465, 254);
                    g.DrawLine(browPen, 589, 246, 559, 254);
                }

                // 4. Soft rosy cheek blush hatch marks
                Color blush = Color.FromArgb(180, 255, 100, 130);
                using (Pen pBlush = new Pen(blush, 2.8f))
                {
                    pBlush.StartCap = LineCap.Round;
                    pBlush.EndCap = LineCap.Round;

                    // Left cheek
                    g.DrawLine(pBlush, 405, 316, 413, 330);
                    g.DrawLine(pBlush, 415, 316, 423, 330);
                    g.DrawLine(pBlush, 425, 318, 432, 330);

                    // Right cheek
                    g.DrawLine(pBlush, 600, 318, 607, 330);
                    g.DrawLine(pBlush, 610, 316, 617, 330);
                    g.DrawLine(pBlush, 620, 316, 627, 330);
                }

                // 5. Adorable anime sigh puff cloud next to mouth
                using (Pen puffPen = new Pen(Color.FromArgb(200, 175, 175, 205), 2.2f))
                {
                    puffPen.StartCap = LineCap.Round;
                    puffPen.EndCap = LineCap.Round;
                    g.DrawArc(puffPen, 536, 348, 12, 10, 180, 180);
                    g.DrawArc(puffPen, 546, 350, 10, 8, 180, 180);
                    g.DrawLine(puffPen, 532, 357, 556, 357);
                }
            }

            res.Save(outPath, ImageFormat.Png);
        }
    }

    private static void MakePreview(string srcPath, string outPath)
    {
        using (Bitmap src = new Bitmap(srcPath))
        using (Bitmap scaled = new Bitmap(340, 380))
        using (Graphics g = Graphics.FromImage(scaled))
        {
            Rectangle rect = new Rectangle(180, 120, 664, 760);
            using (Bitmap cropped = src.Clone(rect, src.PixelFormat))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.White);
                g.DrawImage(cropped, 0, 0, 340, 380);
            }
            scaled.Save(outPath, ImageFormat.Png);
        }
    }
}
