using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class EyeExtractor
{
    static void Main()
    {
        string spritesDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
        string idlePath = Path.Combine(spritesDir, "idle.png");

        using (Bitmap idle = new Bitmap(idlePath))
        {
            int w = idle.Width;
            int h = idle.Height;

            // 1. Clean Left Pupil (centered around 433, 432)
            int pw = 80;
            int ph = 96;
            Bitmap pupilL = new Bitmap(pw, ph, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(pupilL))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Clip to clean smooth oval
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(2, 2, pw - 4, ph - 4);
                    g.SetClip(gp);

                    // Draw the source iris
                    int srcX = 433 - pw / 2;
                    int srcY = 432 - ph / 2;
                    g.DrawImage(idle, new Rectangle(0, 0, pw, ph), new Rectangle(srcX, srcY, pw, ph), GraphicsUnit.Pixel);
                }
            }
            pupilL.Save(Path.Combine(spritesDir, "pupil_l.png"), ImageFormat.Png);

            // 2. Clean Right Pupil (centered around 591, 430)
            Bitmap pupilR = new Bitmap(pw, ph, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(pupilR))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(2, 2, pw - 4, ph - 4);
                    g.SetClip(gp);

                    int srcX = 591 - pw / 2;
                    int srcY = 430 - ph / 2;
                    g.DrawImage(idle, new Rectangle(0, 0, pw, ph), new Rectangle(srcX, srcY, pw, ph), GraphicsUnit.Pixel);
                }
            }
            pupilR.Save(Path.Combine(spritesDir, "pupil_r.png"), ImageFormat.Png);

            // 3. Clean Sclera Base (eye sockets filled with smooth white sclera)
            Bitmap scleraBase = new Bitmap(idle);
            using (Graphics g = Graphics.FromImage(scleraBase))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color scleraTop = Color.FromArgb(255, 230, 225, 235);
                Color scleraBottom = Color.FromArgb(255, 255, 255, 255);

                // Left socket
                using (GraphicsPath spL = new GraphicsPath())
                {
                    spL.AddEllipse(433 - 44, 432 - 48, 88, 96);
                    using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(433 - 44, 432 - 48, 88, 96), scleraTop, scleraBottom, 90f))
                    {
                        g.FillPath(b, spL);
                    }
                }

                // Right socket
                using (GraphicsPath spR = new GraphicsPath())
                {
                    spR.AddEllipse(591 - 44, 430 - 48, 88, 96);
                    using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(591 - 44, 430 - 48, 88, 96), scleraTop, scleraBottom, 90f))
                    {
                        g.FillPath(b, spR);
                    }
                }
            }
            scleraBase.Save(Path.Combine(spritesDir, "idle_sclera.png"), ImageFormat.Png);

            // 4. Clean Eyelash Overlay (upper lashes and eyelids)
            Bitmap eyelashes = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(eyelashes))
            {
                // Only copy the upper eyelash region from idle
                for (int y = 370; y < 440; y++)
                {
                    for (int x = 360; x < 670; x++)
                    {
                        if ((x >= 360 && x <= 495) || (x >= 530 && x <= 665))
                        {
                            Color c = idle.GetPixel(x, y);
                            // Eyelash contour is dark purple/black
                            if (c.R < 110 && c.G < 90 && c.B < 120 && c.A > 120)
                            {
                                eyelashes.SetPixel(x, y, c);
                            }
                        }
                    }
                }
            }
            eyelashes.Save(Path.Combine(spritesDir, "eyelashes.png"), ImageFormat.Png);

            Console.WriteLine("Perfect eye layers created!");
        }
    }
}
