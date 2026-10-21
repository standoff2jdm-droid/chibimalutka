using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ChibiArtGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet";
            string spritesDir = Path.Combine(baseDir, @"assets\sprites");
            string brainDir = @"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877";

            string idleSrc = Path.Combine(brainDir, "sprite_idle_1792506710002.jpg");
            string blinkSrc = Path.Combine(brainDir, "sprite_blink_1792506730351.jpg");
            string happySrc = Path.Combine(brainDir, "sprite_happy_pat_v2_1792506772311.jpg");
            string dragSrc = Path.Combine(brainDir, "sprite_drag_v3_1792506833153.jpg");
            string consoleSrc = Path.Combine(brainDir, "sprite_console_1792507823218.jpg");
            string drawingSrc = Path.Combine(brainDir, "sprite_drawing_1792507842670.jpg");
            string sleepSrc = Path.Combine(brainDir, "sprite_sleep_1792507882034.jpg");
            string poutSrc = Path.Combine(brainDir, "sprite_pout_1792507912591.jpg");

            // Build test idle sprite
            using (var bmp = new Bitmap(idleSrc))
            {
                var transparent = RemoveWhiteBackground(bmp);
                var styled = ApplyReferenceHairstyle(transparent, isSleeping: false);
                styled.Save(Path.Combine(spritesDir, "test_styled_idle.png"), ImageFormat.Png);
                Console.WriteLine("Saved test_styled_idle.png");
            }
        }

        public static Bitmap RemoveWhiteBackground(Bitmap src)
        {
            int w = src.Width;
            int h = src.Height;
            Bitmap dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = src.GetPixel(x, y);
                    // If near white, make transparent with smooth feather
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

        public static Bitmap ApplyReferenceHairstyle(Bitmap baseBmp, bool isSleeping)
        {
            int w = baseBmp.Width;
            int h = baseBmp.Height;

            // Give extra room at top so hair/horns/ahoge never get clipped
            int newW = w;
            int newH = h + 100;
            Bitmap result = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Draw base character with vertical offset of 90px to protect top of head
                g.DrawImage(baseBmp, 0, 90, w, h);

                if (isSleeping) return result;

                // Color palette from reference image:
                Color hairPurpleDark = Color.FromArgb(255, 65, 45, 95);     // Dark contour purple
                Color hairPurpleBase = Color.FromArgb(255, 120, 85, 175);   // Midtone purple
                Color hairPurpleLight = Color.FromArgb(255, 155, 120, 215); // Highlight purple
                Color hairGreenBase = Color.FromArgb(255, 140, 165, 80);    // Green ombre
                Color hairGreenLight = Color.FromArgb(255, 175, 205, 100);  // Bright green tips
                Color hornRed = Color.FromArgb(255, 205, 35, 65);          // Horn clip red
                Color hornDark = Color.FromArgb(255, 140, 15, 35);         // Horn shadow

                // --- 1. Outward-Flaring Spiky Wing Locks (LEFT SIDE) ---
                // In reference, left hair flares outward and upward like wings near temple
                using (GraphicsPath wingLeft = new GraphicsPath())
                {
                    wingLeft.StartFigure();
                    wingLeft.AddBezier(new PointF(295, 310), new PointF(250, 315), new PointF(205, 330), new PointF(185, 360));
                    wingLeft.AddBezier(new PointF(185, 360), new PointF(170, 375), new PointF(150, 395), new PointF(140, 425));
                    // Flare tip 1 (pointing up and out)
                    wingLeft.AddLine(new PointF(140, 425), new PointF(180, 415));
                    // Flare tip 2 (second spiky tier)
                    wingLeft.AddBezier(new PointF(180, 415), new PointF(150, 445), new PointF(135, 475), new PointF(155, 495));
                    wingLeft.AddBezier(new PointF(155, 495), new PointF(190, 470), new PointF(225, 440), new PointF(260, 430));
                    wingLeft.CloseFigure();

                    using (LinearGradientBrush lgb = new LinearGradientBrush(new Rectangle(130, 300, 180, 200), hairPurpleBase, hairPurpleDark, 45f))
                    {
                        g.FillPath(lgb, wingLeft);
                    }
                    using (Pen p = new Pen(hairPurpleDark, 4.5f))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, wingLeft);
                    }
                }

                // --- 2. Outward-Flaring Spiky Wing Locks (RIGHT SIDE) ---
                using (GraphicsPath wingRight = new GraphicsPath())
                {
                    wingRight.StartFigure();
                    wingRight.AddBezier(new PointF(725, 310), new PointF(770, 315), new PointF(815, 330), new PointF(835, 360));
                    wingRight.AddBezier(new PointF(835, 360), new PointF(850, 375), new PointF(870, 395), new PointF(885, 425));
                    // Flare tip 1
                    wingRight.AddLine(new PointF(885, 425), new PointF(845, 415));
                    // Flare tip 2
                    wingRight.AddBezier(new PointF(845, 415), new PointF(875, 445), new PointF(890, 475), new PointF(865, 495));
                    wingRight.AddBezier(new PointF(865, 495), new PointF(830, 470), new PointF(795, 440), new PointF(760, 430));
                    wingRight.CloseFigure();

                    using (LinearGradientBrush rgb = new LinearGradientBrush(new Rectangle(720, 300, 180, 200), hairPurpleBase, hairPurpleDark, 135f))
                    {
                        g.FillPath(rgb, wingRight);
                    }
                    using (Pen p = new Pen(hairPurpleDark, 4.5f))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, wingRight);
                    }
                }

                // --- 3. Fluffy Spiky Top Ahoge (Crown Cowlick) ---
                using (GraphicsPath ahoge = new GraphicsPath())
                {
                    ahoge.StartFigure();
                    ahoge.AddBezier(new PointF(490, 125), new PointF(460, 95), new PointF(430, 75), new PointF(395, 85));
                    ahoge.AddBezier(new PointF(395, 85), new PointF(370, 95), new PointF(365, 115), new PointF(385, 120));
                    ahoge.AddBezier(new PointF(385, 120), new PointF(415, 115), new PointF(450, 125), new PointF(510, 145));
                    ahoge.CloseFigure();

                    using (SolidBrush b = new SolidBrush(hairPurpleBase))
                    {
                        g.FillPath(b, ahoge);
                    }
                    using (Pen p = new Pen(hairPurpleDark, 4.0f))
                    {
                        p.LineJoin = LineJoin.Round;
                        g.DrawPath(p, ahoge);
                    }
                }

                // Second tuft at crown
                using (GraphicsPath tuft = new GraphicsPath())
                {
                    tuft.StartFigure();
                    tuft.AddBezier(new PointF(525, 125), new PointF(550, 100), new PointF(575, 95), new PointF(600, 110));
                    tuft.AddBezier(new PointF(600, 110), new PointF(570, 120), new PointF(545, 135), new PointF(535, 145));
                    tuft.CloseFigure();

                    using (SolidBrush b = new SolidBrush(hairPurpleBase))
                    {
                        g.FillPath(b, tuft);
                    }
                    using (Pen p = new Pen(hairPurpleDark, 3.5f))
                    {
                        g.DrawPath(p, tuft);
                    }
                }

                // --- 4. Two Red Horn Clips (matching reference!) ---
                // Left Horn Clip (curved pointy red horn)
                using (GraphicsPath hornL = new GraphicsPath())
                {
                    hornL.StartFigure();
                    hornL.AddBezier(new PointF(335, 185), new PointF(325, 160), new PointF(315, 135), new PointF(310, 125));
                    hornL.AddBezier(new PointF(310, 125), new PointF(328, 145), new PointF(345, 165), new PointF(350, 185));
                    hornL.CloseFigure();

                    using (LinearGradientBrush hgb = new LinearGradientBrush(new Rectangle(305, 120, 50, 70), hornRed, hornDark, 90f))
                    {
                        g.FillPath(hgb, hornL);
                    }
                    using (Pen p = new Pen(hornDark, 3.5f))
                    {
                        g.DrawPath(p, hornL);
                    }
                }

                // Right Horn Clip
                using (GraphicsPath hornR = new GraphicsPath())
                {
                    hornR.StartFigure();
                    hornR.AddBezier(new PointF(685, 185), new PointF(695, 160), new PointF(705, 135), new PointF(710, 125));
                    hornR.AddBezier(new PointF(710, 125), new PointF(692, 145), new PointF(675, 165), new PointF(670, 185));
                    hornR.CloseFigure();

                    using (LinearGradientBrush hgb = new LinearGradientBrush(new Rectangle(665, 120, 50, 70), hornRed, hornDark, 90f))
                    {
                        g.FillPath(hgb, hornR);
                    }
                    using (Pen p = new Pen(hornDark, 3.5f))
                    {
                        g.DrawPath(p, hornR);
                    }
                }

                // --- 5. Feathered Choppy Bangs (Layered over forehead) ---
                // Central sweeping spike
                using (GraphicsPath bangCenter = new GraphicsPath())
                {
                    bangCenter.StartFigure();
                    bangCenter.AddBezier(new PointF(460, 225), new PointF(475, 275), new PointF(490, 320), new PointF(485, 370));
                    bangCenter.AddBezier(new PointF(485, 370), new PointF(505, 325), new PointF(525, 280), new PointF(535, 230));
                    bangCenter.CloseFigure();

                    using (LinearGradientBrush bgb = new LinearGradientBrush(new Rectangle(450, 220, 90, 155), hairPurpleBase, hairPurpleLight, 60f))
                    {
                        g.FillPath(bgb, bangCenter);
                    }
                    using (Pen p = new Pen(hairPurpleDark, 3.5f))
                    {
                        g.DrawPath(p, bangCenter);
                    }
                }

                // Left angled bang spike
                using (GraphicsPath bangL = new GraphicsPath())
                {
                    bangL.StartFigure();
                    bangL.AddBezier(new PointF(390, 240), new PointF(410, 290), new PointF(430, 340), new PointF(425, 375));
                    bangL.AddBezier(new PointF(425, 375), new PointF(440, 335), new PointF(455, 290), new PointF(465, 245));
                    bangL.CloseFigure();

                    using (LinearGradientBrush bgb = new LinearGradientBrush(new Rectangle(385, 235, 85, 145), hairPurpleBase, hairPurpleLight, 60f))
                    {
                        g.FillPath(bgb, bangL);
                    }
                    using (Pen p = new Pen(hairPurpleDark, 3.5f))
                    {
                        g.DrawPath(p, bangL);
                    }
                }

                // Right angled bang spike
                using (GraphicsPath bangR = new GraphicsPath())
                {
                    bangR.StartFigure();
                    bangR.AddBezier(new PointF(535, 240), new PointF(550, 285), new PointF(575, 335), new PointF(585, 370));
                    bangR.AddBezier(new PointF(585, 370), new PointF(590, 325), new PointF(600, 285), new PointF(610, 245));
                    bangR.CloseFigure();

                    using (LinearGradientBrush bgb = new LinearGradientBrush(new Rectangle(530, 235, 85, 145), hairPurpleBase, hairPurpleLight, 60f))
                    {
                        g.FillPath(bgb, bangR);
                    }
                    using (Pen p = new Pen(hairPurpleDark, 3.5f))
                    {
                        g.DrawPath(p, bangR);
                    }
                }

                // --- 6. Long Green Front Strands (framing chest) ---
                // Left front green strand
                using (GraphicsPath strandL = new GraphicsPath())
                {
                    strandL.StartFigure();
                    strandL.AddBezier(new PointF(360, 380), new PointF(345, 440), new PointF(340, 510), new PointF(350, 580));
                    strandL.AddBezier(new PointF(350, 580), new PointF(355, 620), new PointF(365, 660), new PointF(355, 690));
                    strandL.AddBezier(new PointF(355, 690), new PointF(375, 650), new PointF(370, 580), new PointF(375, 510));
                    strandL.AddBezier(new PointF(375, 510), new PointF(380, 440), new PointF(385, 390), new PointF(375, 380));
                    strandL.CloseFigure();

                    using (LinearGradientBrush sgb = new LinearGradientBrush(new Rectangle(335, 370, 55, 330), hairPurpleBase, hairGreenLight, 90f))
                    {
                        g.FillPath(sgb, strandL);
                    }
                    using (Pen p = new Pen(Color.FromArgb(200, 50, 40, 70), 3.0f))
                    {
                        g.DrawPath(p, strandL);
                    }
                }

                // Right front green strand
                using (GraphicsPath strandR = new GraphicsPath())
                {
                    strandR.StartFigure();
                    strandR.AddBezier(new PointF(645, 380), new PointF(660, 440), new PointF(665, 510), new PointF(655, 580));
                    strandR.AddBezier(new PointF(655, 580), new PointF(650, 620), new PointF(640, 660), new PointF(650, 690));
                    strandR.AddBezier(new PointF(650, 690), new PointF(630, 650), new PointF(635, 580), new PointF(630, 510));
                    strandR.AddBezier(new PointF(630, 510), new PointF(625, 440), new PointF(620, 390), new PointF(630, 380));
                    strandR.CloseFigure();

                    using (LinearGradientBrush sgb = new LinearGradientBrush(new Rectangle(615, 370, 55, 330), hairPurpleBase, hairGreenLight, 90f))
                    {
                        g.FillPath(sgb, strandR);
                    }
                    using (Pen p = new Pen(Color.FromArgb(200, 50, 40, 70), 3.0f))
                    {
                        g.DrawPath(p, strandR);
                    }
                }
            }

            return result;
        }
    }
}
