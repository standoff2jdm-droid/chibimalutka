using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

class TestEyes2
{
    static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        using (Bitmap idle = new Bitmap(idlePath))
        {
            int w = idle.Width;
            int h = idle.Height;

            int lw = 70;
            int lh = 72;
            int lcx = 436;
            int lcy = 426;

            int rw = 72;
            int rh = 72;
            int rcx = 585;
            int rcy = 425;

            // 1. Iris Left
            Bitmap irisL = new Bitmap(lw, lh, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(irisL))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(2, 2, lw - 4, lh - 4);
                    g.SetClip(gp);
                    g.DrawImage(idle, new Rectangle(0, 0, lw, lh), new Rectangle(lcx - lw / 2, lcy - lh / 2, lw, lh), GraphicsUnit.Pixel);
                }
            }

            // 2. Iris Right
            Bitmap irisR = new Bitmap(rw, rh, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(irisR))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(2, 2, rw - 4, rh - 4);
                    g.SetClip(gp);
                    g.DrawImage(idle, new Rectangle(0, 0, rw, rh), new Rectangle(rcx - rw / 2, rcy - rh / 2, rw, rh), GraphicsUnit.Pixel);
                }
            }

            // 3. Sclera Base: fill iris socket cleanly
            Bitmap scleraBase = new Bitmap(idle);
            using (Graphics g = Graphics.FromImage(scleraBase))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color topShade = Color.FromArgb(255, 205, 198, 212);
                Color botShade = Color.FromArgb(255, 252, 245, 242);

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(lcx - lw / 2 + 2, lcy - lh / 2 + 2, lw - 4, lh - 4);
                    using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(lcx - lw/2, lcy - lh/2, lw, lh), topShade, botShade, 90f))
                    {
                        g.FillPath(brush, gp);
                    }
                }

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(rcx - rw / 2 + 2, rcy - rh / 2 + 2, rw - 4, rh - 4);
                    using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(rcx - rw/2, rcy - rh/2, rw, lh), topShade, botShade, 90f))
                    {
                        g.FillPath(brush, gp);
                    }
                }
            }

            // 4. Eyelash Overlay: take original idle above the iris center with smooth alpha fadeout down to Y=435
            Bitmap eyelashes = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Only process around eye lash rows
                    if (y >= 360 && y <= 445 && x >= 360 && x <= 660)
                    {
                        Color c = idle.GetPixel(x, y);
                        // Upper eyelashes and eyelid crease have darker tone
                        // Let's compute alpha based on darkness and Y position
                        // Or if y < 400, it is 100% eyelid/bangs/lashes!
                        double alphaFactor = 0.0;
                        if (y <= 405)
                        {
                            alphaFactor = 1.0;
                        }
                        else if (y <= 435)
                        {
                            // Fade out smoothly
                            double t = (435 - y) / 30.0;
                            // Check if dark eyelash pixel
                            double brightness = (c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
                            if (brightness < 160)
                            {
                                alphaFactor = t;
                            }
                            else
                            {
                                alphaFactor = t * (1.0 - (brightness - 160.0) / 95.0);
                                if (alphaFactor < 0) alphaFactor = 0;
                            }
                        }

                        if (alphaFactor > 0.01)
                        {
                            int a = (int)(c.A * alphaFactor);
                            if (a > 255) a = 255;
                            eyelashes.SetPixel(x, y, Color.FromArgb(a, c.R, c.G, c.B));
                        }
                    }
                }
            }

            // Save the clean assets
            string outDir = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites";
            irisL.Save(System.IO.Path.Combine(outDir, "pupil_l.png"), ImageFormat.Png);
            irisR.Save(System.IO.Path.Combine(outDir, "pupil_r.png"), ImageFormat.Png);
            scleraBase.Save(System.IO.Path.Combine(outDir, "idle_sclera.png"), ImageFormat.Png);
            eyelashes.Save(System.IO.Path.Combine(outDir, "eyelashes.png"), ImageFormat.Png);

            // Test composite with cursor look (dx = 4, dy = 3)
            Bitmap testComp = new Bitmap(scleraBase);
            using (Graphics g = Graphics.FromImage(testComp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(irisL, lcx - lw / 2 + 4, lcy - lh / 2 + 3);
                g.DrawImage(irisR, rcx - rw / 2 + 4, rcy - rh / 2 + 3);
                g.DrawImage(eyelashes, 0, 0);
            }

            Bitmap crop = new Bitmap(350, 250);
            using (Graphics g = Graphics.FromImage(crop))
            {
                g.DrawImage(testComp, 0, 0, new Rectangle(340, 350, 350, 250), GraphicsUnit.Pixel);
            }
            crop.Save(@"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877\test_eye_comp2.png", ImageFormat.Png);
        }
    }
}
