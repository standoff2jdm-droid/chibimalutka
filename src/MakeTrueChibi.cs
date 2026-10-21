using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ChibiMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            string brainDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";
            string spritesDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";

            if (!Directory.Exists(spritesDir)) Directory.CreateDirectory(spritesDir);

            // 1. Idle Sprite
            Console.WriteLine("Generating idle.png...");
            Bitmap idleBmp;
            using (var raw = new Bitmap(Path.Combine(brainDir, "sprite_idle_1792506710002.jpg")))
            {
                var trans = RemoveWhite(raw);
                idleBmp = StyleChibi(trans, false, isSitting: false);
                idleBmp.Save(Path.Combine(spritesDir, "idle.png"), ImageFormat.Png);
            }

            // 2. Blink Closed Sprite
            Console.WriteLine("Generating blink.png...");
            Bitmap blinkBmp;
            using (var raw = new Bitmap(Path.Combine(brainDir, "sprite_blink_1792506730351.jpg")))
            {
                var trans = RemoveWhite(raw);
                blinkBmp = StyleChibi(trans, false, isSitting: false);
                blinkBmp.Save(Path.Combine(spritesDir, "blink.png"), ImageFormat.Png);
            }

            // 3. Blink Half Sprite (smooth intermediate frame)
            Console.WriteLine("Generating blink_half.png...");
            using (Bitmap halfBmp = CreateBlinkHalf(idleBmp, blinkBmp))
            {
                halfBmp.Save(Path.Combine(spritesDir, "blink_half.png"), ImageFormat.Png);
            }

            // 4. Happy Headpat Reaction Sprite
            Console.WriteLine("Generating happy.png...");
            using (var raw = new Bitmap(Path.Combine(brainDir, "sprite_happy_pat_v2_1792506772311.jpg")))
            {
                var trans = RemoveWhite(raw);
                var styled = StyleChibi(trans, false, isSitting: false);
                styled.Save(Path.Combine(spritesDir, "happy.png"), ImageFormat.Png);
            }

            // 5. Drag Dangling Sprite
            Console.WriteLine("Generating drag.png...");
            using (var raw = new Bitmap(Path.Combine(brainDir, "sprite_drag_v3_1792506833153.jpg")))
            {
                var trans = RemoveWhite(raw);
                var styled = StyleChibi(trans, false, isSitting: false);
                styled.Save(Path.Combine(spritesDir, "drag.png"), ImageFormat.Png);
            }

            // 6. Console Gaming Sprite
            Console.WriteLine("Generating console.png...");
            using (var raw = new Bitmap(Path.Combine(brainDir, "sprite_console_1792507823218.jpg")))
            {
                var trans = RemoveWhite(raw);
                var styled = StyleChibi(trans, false, isSitting: true);
                styled.Save(Path.Combine(spritesDir, "console.png"), ImageFormat.Png);
            }

            // 7. Drawing Tablet Sprite
            Console.WriteLine("Generating drawing.png...");
            using (var raw = new Bitmap(Path.Combine(brainDir, "sprite_drawing_1792507842670.jpg")))
            {
                var trans = RemoveWhite(raw);
                var styled = StyleChibi(trans, false, isSitting: true);
                styled.Save(Path.Combine(spritesDir, "drawing.png"), ImageFormat.Png);
            }

            // 8. Sleep Sprite
            Console.WriteLine("Generating sleep.png...");
            using (var raw = new Bitmap(Path.Combine(brainDir, "sprite_sleep_1792507882034.jpg")))
            {
                var trans = RemoveWhite(raw);
                var styled = StyleChibi(trans, true, isSitting: false);
                styled.Save(Path.Combine(spritesDir, "sleep.png"), ImageFormat.Png);
            }

            // 9. Pout Sprite
            Console.WriteLine("Generating pout.png...");
            using (var raw = new Bitmap(Path.Combine(brainDir, "sprite_pout_1792507912591.jpg")))
            {
                var trans = RemoveWhite(raw);
                var styled = StyleChibi(trans, false, isSitting: false);
                styled.Save(Path.Combine(spritesDir, "pout.png"), ImageFormat.Png);
            }

            // Clean up cached bitmaps
            idleBmp.Dispose();
            blinkBmp.Dispose();

            Console.WriteLine("All sprites generated successfully!");
        }

        static Bitmap RemoveWhite(Bitmap src)
        {
            int w = src.Width;
            int h = src.Height;
            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = src.GetPixel(x, y);
                    int brightness = (c.R + c.G + c.B) / 3;
                    int diff = Math.Max(Math.Abs(c.R - c.G), Math.Max(Math.Abs(c.R - c.B), Math.Abs(c.G - c.B)));
                    if (brightness > 248 && diff < 8)
                    {
                        dst.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                    }
                    else if (brightness > 235 && diff < 12)
                    {
                        int alpha = (int)((255 - brightness) * (255.0 / (255 - 235)));
                        alpha = Math.Max(0, Math.Min(255, alpha));
                        dst.SetPixel(x, y, Color.FromArgb(alpha, c.R, c.G, c.B));
                    }
                    else
                    {
                        dst.SetPixel(x, y, c);
                    }
                }
            }
            return dst;
        }

        static Bitmap CreateBlinkHalf(Bitmap openBmp, Bitmap closedBmp)
        {
            int w = openBmp.Width;
            int h = openBmp.Height;
            Bitmap res = new Bitmap(openBmp);

            int eyeTop = 90 + 330;
            int eyeBottom = 90 + 440;
            int eyeLeft = 330;
            int eyeRight = 690;

            for (int y = eyeTop; y < eyeBottom; y++)
            {
                double factor = (double)(y - eyeTop) / (eyeBottom - eyeTop);
                // Upper half of eye closes first
                double closedWeight = Math.Max(0.0, Math.Min(1.0, 1.4 - factor * 1.2));

                for (int x = eyeLeft; x < eyeRight; x++)
                {
                    Color cOpen = openBmp.GetPixel(x, y);
                    Color cClosed = closedBmp.GetPixel(x, y);

                    if (cOpen.A == 0 && cClosed.A == 0) continue;

                    int r = (int)(cOpen.R * (1.0 - closedWeight) + cClosed.R * closedWeight);
                    int g = (int)(cOpen.G * (1.0 - closedWeight) + cClosed.G * closedWeight);
                    int b = (int)(cOpen.B * (1.0 - closedWeight) + cClosed.B * closedWeight);
                    int a = (int)(cOpen.A * (1.0 - closedWeight) + cClosed.A * closedWeight);

                    res.SetPixel(x, y, Color.FromArgb(Math.Min(255, a), Math.Min(255, r), Math.Min(255, g), Math.Min(255, b)));
                }
            }

            return res;
        }

        public static Bitmap StyleChibi(Bitmap baseBmp, bool isSleep, bool isSitting)
        {
            int w = 1024;
            int h = 1120;
            int baseY = 90;
            Bitmap res = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(res))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                g.DrawImage(baseBmp, 0, baseY, 1024, 1024);

                if (isSleep) return res;

                Color contour     = Color.FromArgb(255, 46, 27, 78);      // Dark purple contour
                Color purpleTop   = Color.FromArgb(255, 126, 68, 203);   // Vibrant royal purple
                Color purpleMid   = Color.FromArgb(255, 145, 95, 215);   // Mid purple
                Color purpleLight = Color.FromArgb(255, 195, 160, 245);  // Glossy highlight
                Color greenLight  = Color.FromArgb(255, 175, 205, 100);  // Lime green tips
                Color redHorn     = Color.FromArgb(255, 225, 35, 65);    // Red horn
                Color redDark     = Color.FromArgb(255, 135, 15, 35);    // Red horn shadow
                Color redHi       = Color.FromArgb(255, 255, 130, 150);  // Red horn highlight

                // 1. Crown Ahoge (Emerging organically from top of hair)
                using (GraphicsPath ahoge = new GraphicsPath())
                {
                    ahoge.StartFigure();
                    ahoge.AddBezier(new PointF(490, baseY + 60), new PointF(470, baseY + 15), new PointF(435, baseY - 35), new PointF(395, baseY - 45));
                    ahoge.AddBezier(new PointF(395, baseY - 45), new PointF(370, baseY - 48), new PointF(355, baseY - 30), new PointF(370, baseY - 15));
                    ahoge.AddBezier(new PointF(370, baseY - 15), new PointF(395, baseY - 15), new PointF(430, baseY), new PointF(460, baseY + 30));
                    ahoge.AddBezier(new PointF(460, baseY + 30), new PointF(485, baseY + 50), new PointF(500, baseY + 60), new PointF(510, baseY + 65));
                    ahoge.CloseFigure();

                    using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(350, baseY - 55, 170, 120), purpleTop, purpleMid, 45f))
                    {
                        g.FillPath(b, ahoge);
                    }
                    using (Pen hip = new Pen(Color.FromArgb(160, purpleLight), 2.5f))
                    {
                        g.DrawBezier(hip, new PointF(480, baseY + 45), new PointF(455, baseY + 15), new PointF(420, baseY - 15), new PointF(385, baseY - 25));
                    }
                    using (Pen p = new Pen(contour, 3.5f))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, ahoge);
                    }
                }

                // 2. Crown hair crest (adding volume on top)
                using (GraphicsPath crest = new GraphicsPath())
                {
                    crest.StartFigure();
                    crest.AddBezier(new PointF(515, baseY + 65), new PointF(545, baseY + 38), new PointF(580, baseY + 32), new PointF(615, baseY + 48));
                    crest.AddBezier(new PointF(615, baseY + 48), new PointF(585, baseY + 58), new PointF(555, baseY + 75), new PointF(530, baseY + 80));
                    crest.CloseFigure();

                    using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(510, baseY + 30, 110, 55), purpleTop, purpleMid, 90f))
                    {
                        g.FillPath(b, crest);
                    }
                    using (Pen p = new Pen(contour, 3.0f))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, crest);
                    }
                }

                // 3. Two Red Devil Horn Clips (Authentic to reference!)
                DrawHorn(g, new PointF(340, baseY + 105), new PointF(305, baseY + 45), new PointF(360, baseY + 95), redHorn, redDark, redHi, contour);
                DrawHorn(g, new PointF(665, baseY + 95), new PointF(720, baseY + 45), new PointF(685, baseY + 105), redHorn, redDark, redHi, contour);

                // 4. Feathered Bang Highlight Strand
                using (GraphicsPath bang1 = new GraphicsPath())
                {
                    bang1.StartFigure();
                    bang1.AddBezier(new PointF(492, baseY + 200), new PointF(502, baseY + 245), new PointF(505, baseY + 278), new PointF(498, baseY + 305));
                    bang1.AddBezier(new PointF(498, baseY + 305), new PointF(508, baseY + 278), new PointF(516, baseY + 245), new PointF(520, baseY + 200));
                    bang1.CloseFigure();
                    using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(488, baseY + 195, 36, 115), purpleTop, purpleMid, 90f))
                    {
                        g.FillPath(b, bang1);
                    }
                    using (Pen p = new Pen(contour, 2.5f))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, bang1);
                    }
                }

                // 5. Long Front Green Strands (Draping over the shoulders/chest)
                // Left strand
                using (GraphicsPath strand = new GraphicsPath())
                {
                    strand.StartFigure();
                    strand.AddBezier(new PointF(360, baseY + 330), new PointF(350, baseY + 390), new PointF(355, baseY + 470), new PointF(365, baseY + 540));
                    strand.AddBezier(new PointF(365, baseY + 540), new PointF(370, baseY + 590), new PointF(380, baseY + 630), new PointF(370, baseY + 655));
                    strand.AddBezier(new PointF(370, baseY + 655), new PointF(385, baseY + 620), new PointF(380, baseY + 550), new PointF(375, baseY + 480));
                    strand.AddBezier(new PointF(375, baseY + 480), new PointF(375, baseY + 410), new PointF(375, baseY + 350), new PointF(368, baseY + 330));
                    strand.CloseFigure();

                    using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(345, baseY + 325, 45, 335), purpleMid, greenLight, 90f))
                    {
                        g.FillPath(b, strand);
                    }
                    using (Pen p = new Pen(contour, 2.8f))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, strand);
                    }
                }

                // Right strand
                using (GraphicsPath strand = new GraphicsPath())
                {
                    strand.StartFigure();
                    strand.AddBezier(new PointF(665, baseY + 330), new PointF(675, baseY + 390), new PointF(670, baseY + 470), new PointF(660, baseY + 540));
                    strand.AddBezier(new PointF(660, baseY + 540), new PointF(655, baseY + 590), new PointF(645, baseY + 630), new PointF(655, baseY + 665));
                    strand.AddBezier(new PointF(655, baseY + 665), new PointF(640, baseY + 620), new PointF(645, baseY + 550), new PointF(650, baseY + 480));
                    strand.AddBezier(new PointF(650, baseY + 480), new PointF(650, baseY + 410), new PointF(650, baseY + 350), new PointF(658, baseY + 330));
                    strand.CloseFigure();

                    using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(635, baseY + 325, 45, 345), purpleMid, greenLight, 90f))
                    {
                        g.FillPath(b, strand);
                    }
                    using (Pen p = new Pen(contour, 2.8f))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, strand);
                    }
                }
            }

            return res;
        }

        static void DrawHorn(Graphics g, PointF baseL, PointF tip, PointF baseR, Color col, Color dark, Color hi, Color stroke)
        {
            using (GraphicsPath horn = new GraphicsPath())
            {
                horn.StartFigure();
                horn.AddBezier(baseL, new PointF((baseL.X + tip.X) * 0.5f - 8, (baseL.Y + tip.Y) * 0.5f), new PointF(tip.X - 3, tip.Y + 15), tip);
                horn.AddBezier(tip, new PointF(tip.X + 6, tip.Y + 15), new PointF((baseR.X + tip.X) * 0.5f + 8, (baseR.Y + tip.Y) * 0.5f), baseR);
                horn.CloseFigure();

                float minX = Math.Min(baseL.X, Math.Min(tip.X, baseR.X)) - 5;
                float minY = Math.Min(baseL.Y, Math.Min(tip.Y, baseR.Y)) - 5;
                float w = Math.Max(baseL.X, Math.Max(tip.X, baseR.X)) - minX + 10;
                float h = Math.Max(baseL.Y, Math.Max(tip.Y, baseR.Y)) - minY + 10;

                using (LinearGradientBrush b = new LinearGradientBrush(new RectangleF(minX, minY, w, h), col, dark, 75f))
                {
                    g.FillPath(b, horn);
                }

                using (Pen hip = new Pen(Color.FromArgb(180, hi), 2.2f))
                {
                    g.DrawLine(hip, tip.X, tip.Y + 6, (baseL.X + tip.X) * 0.5f, (baseL.Y + tip.Y) * 0.5f);
                }

                using (Pen p = new Pen(stroke, 3.5f))
                {
                    p.LineJoin = LineJoin.Round;
                    g.DrawPath(p, horn);
                }
            }
        }
    }
}
