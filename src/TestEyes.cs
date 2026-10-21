using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class TestEyes
{
    static void Main()
    {
        string idlePath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\idle.png";
        using (Bitmap idle = new Bitmap(idlePath))
        {
            int w = idle.Width;
            int h = idle.Height;

            // 1. Extract Left Iris with feather/anti-aliasing
            int lw = 70;
            int lh = 72;
            int lcx = 436;
            int lcy = 426;

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

            // 2. Extract Right Iris
            int rw = 72;
            int rh = 72;
            int rcx = 585;
            int rcy = 425;

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

            // 3. Create Clean Sclera Base: ONLY replace inside the iris ellipse with shaded sclera
            Bitmap scleraBase = new Bitmap(idle);
            using (Graphics g = Graphics.FromImage(scleraBase))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color topShade = Color.FromArgb(255, 205, 198, 212);
                Color botShade = Color.FromArgb(255, 252, 245, 242);

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(lcx - lw / 2 + 3, lcy - lh / 2 + 3, lw - 6, lh - 6);
                    using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(lcx - lw/2, lcy - lh/2, lw, lh), topShade, botShade, 90f))
                    {
                        g.FillPath(brush, gp);
                    }
                }

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(rcx - rw / 2 + 3, rcy - rh / 2 + 3, rw - 6, rh - 6);
                    using (LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(rcx - rw/2, rcy - rh/2, rw, lh), topShade, botShade, 90f))
                    {
                        g.FillPath(brush, gp);
                    }
                }
            }

            // 4. Create Upper Eyelash / Eyelid Overlay from original idle
            Bitmap eyelashes = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(eyelashes))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Copy only eyelash regions (top and corners)
                for (int y = 370; y < 475; y++)
                {
                    for (int x = 370; x < 650; x++)
                    {
                        Color c = idle.GetPixel(x, y);
                        // Check if dark eyelash or eyelid contour
                        bool isLashL = (x >= 370 && x <= 495 && (c.R < 150 && c.G < 110 && c.B < 110));
                        bool isLashR = (x >= 525 && x <= 650 && (c.R < 150 && c.G < 110 && c.B < 110));
                        // Also lower eyelid line
                        bool isLowerL = (x >= 400 && x <= 470 && y >= 455 && y <= 468 && (c.R < 190 && c.G < 140));
                        bool isLowerR = (x >= 550 && x <= 620 && y >= 455 && y <= 468 && (c.R < 190 && c.G < 140));

                        if (isLashL || isLashR || isLowerL || isLowerR)
                        {
                            eyelashes.SetPixel(x, y, c);
                        }
                    }
                }
            }

            // 5. Test composite with eyes shifted slightly towards cursor (dx = +4, dy = +2)
            Bitmap testComp = new Bitmap(scleraBase);
            using (Graphics g = Graphics.FromImage(testComp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int dx = 5;
                int dy = 2;
                g.DrawImage(irisL, lcx - lw / 2 + dx, lcy - lh / 2 + dy);
                g.DrawImage(irisR, rcx - rw / 2 + dx, rcy - rh / 2 + dy);
                g.DrawImage(eyelashes, 0, 0);
            }

            // Crop face
            Bitmap crop = new Bitmap(350, 250);
            using (Graphics g = Graphics.FromImage(crop))
            {
                g.DrawImage(testComp, 0, 0, new Rectangle(340, 350, 350, 250), GraphicsUnit.Pixel);
            }
            crop.Save(@"C:\Users\xzen\.gemini\antigravity\brain\002603e3-e586-4699-b6f6-2a5a85df1877\test_eye_comp.png", ImageFormat.Png);
        }
    }
}
