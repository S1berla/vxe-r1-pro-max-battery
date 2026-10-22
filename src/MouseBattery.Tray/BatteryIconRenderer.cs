using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace MouseBattery.Tray
{
    internal static class BatteryIconRenderer
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr iconHandle);

        internal static Icon Create(
            MouseBatteryReading reading,
            bool stale)
        {
            Color color = GetColor(reading, stale);
            string label = GetLabel(reading);

            using (var bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint =
                    TextRenderingHint.ClearTypeGridFit;

                using (var bodyPath = CreateRoundedRectangle(
                    new Rectangle(2, 7, 24, 19),
                    4))
                using (var fillBrush = new SolidBrush(
                    Color.FromArgb(215, color)))
                using (var borderPen = new Pen(color, 2.0f))
                {
                    graphics.FillPath(fillBrush, bodyPath);
                    graphics.DrawPath(borderPen, bodyPath);

                    using (var capBrush = new SolidBrush(color))
                    {
                        graphics.FillRectangle(capBrush, 27, 12, 3, 9);
                    }
                }

                float fontSize = label.Length >= 3 ? 8.5f : 11.0f;
                using (var font = new Font(
                    "Segoe UI",
                    fontSize,
                    FontStyle.Bold,
                    GraphicsUnit.Pixel))
                using (var textBrush = new SolidBrush(Color.White))
                using (var textFormat = new StringFormat())
                {
                    textFormat.Alignment = StringAlignment.Center;
                    textFormat.LineAlignment = StringAlignment.Center;

                    graphics.DrawString(
                        label,
                        font,
                        textBrush,
                        new RectangleF(2, 8, 24, 17),
                        textFormat);
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon temporary = Icon.FromHandle(handle))
                    {
                        return (Icon)temporary.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        private static Color GetColor(
            MouseBatteryReading reading,
            bool stale)
        {
            if (stale || reading == null)
            {
                return Color.FromArgb(142, 142, 147);
            }

            if (reading.IsCharging)
            {
                return Color.FromArgb(47, 128, 237);
            }

            if (reading.Power < 20)
            {
                return Color.FromArgb(255, 59, 48);
            }

            if (reading.Power < 60)
            {
                return Color.FromArgb(255, 176, 32);
            }

            return Color.FromArgb(52, 199, 89);
        }

        private static string GetLabel(MouseBatteryReading reading)
        {
            if (reading == null)
            {
                return "?";
            }

            int power = reading.Power;
            if (power < 0)
            {
                power = 0;
            }
            if (power > 100)
            {
                power = 100;
            }

            return power.ToString();
        }

        private static GraphicsPath CreateRoundedRectangle(
            Rectangle rectangle,
            int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();

            path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
            path.AddArc(
                rectangle.Right - diameter,
                rectangle.Y,
                diameter,
                diameter,
                270,
                90);
            path.AddArc(
                rectangle.Right - diameter,
                rectangle.Bottom - diameter,
                diameter,
                diameter,
                0,
                90);
            path.AddArc(
                rectangle.X,
                rectangle.Bottom - diameter,
                diameter,
                diameter,
                90,
                90);
            path.CloseFigure();

            return path;
        }
    }
}

