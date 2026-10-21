using System;
using System.Drawing;
using System.Drawing.Imaging;

class InspectSwitch
{
    static void Main()
    {
        string propPath = @"c:\Users\xzen\Documents\antigravity\adventurous-galileo\desktop-pet\assets\sprites\prop_switch.png";
        using (Bitmap bmp = new Bitmap(propPath))
        {
            Console.WriteLine("Switch size: " + bmp.Width + "x" + bmp.Height);
            // Let's create an alpha mask:
            // Background is sweater.
            // Switch is between X=30..250, Y=35..150.
            // Top black border of Switch is around Y=42..46.
            // Thumbs are at left (X: 45..85, Y: 40..85) and right (X: 195..235, Y: 40..85).
            // Let's test a flood fill from (0,0) and (279,0) and (140, 0)
        }
    }
}
