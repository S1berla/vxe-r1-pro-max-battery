using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MouseBattery.Tray
{
    internal static class BatteryIconRenderer
    {
        private static readonly Color HealthyColor =
            Color.FromArgb(52, 199, 89);
        private static readonly Color UnknownColor =
            Color.FromArgb(142, 142, 147);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr iconHandle);

        internal static Icon Create(
            MouseBatteryReading reading,
            bool stale)
        {
            int power = GetPower(reading);
            Color fillColor = stale || reading == null
                ? UnknownColor
                : HealthyColor;

            using (var bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                RectangleF bodyRectangle = new RectangleF(7, 2, 18, 28);
                using (GraphicsPath bodyPath = CreateRoundedRectangle(
                    bodyRectangle,
                    9))
                using (var emptyBrush = new SolidBrush(
                    Color.FromArgb(95, 30, 30, 30)))
                using (var fillBrush = new SolidBrush(fillColor))
                {
                    graphics.FillPath(emptyBrush, bodyPath);

                    if (power > 0)
                    {
                        float fillHeight =
                            bodyRectangle.Height * power / 100.0f;

                        graphics.SetClip(bodyPath);
                        graphics.FillRectangle(
                            fillBrush,
                            bodyRectangle.X,
                            bodyRectangle.Bottom - fillHeight,
                            bodyRectangle.Width,
                            fillHeight);
                        graphics.ResetClip();
                    }

                    using (var outerPen = new Pen(
                        Color.FromArgb(180, 0, 0, 0),
                        4.0f))
                    using (var outlinePen = new Pen(
                        stale || reading == null
                            ? UnknownColor
                            : Color.FromArgb(245, 255, 255, 255),
                        2.0f))
                    {
                        graphics.DrawPath(outerPen, bodyPath);
                        graphics.DrawPath(outlinePen, bodyPath);
                    }
                }

                using (GraphicsPath wheelPath = CreateRoundedRectangle(
                    new RectangleF(14, 6, 4, 8),
                    2))
                using (var wheelBrush = new SolidBrush(
                    Color.FromArgb(215, 255, 255, 255)))
                {
                    graphics.FillPath(wheelBrush, wheelPath);
                }

                using (var centerLinePen = new Pen(
                    Color.FromArgb(130, 255, 255, 255),
                    1.0f))
                {
                    graphics.DrawLine(centerLinePen, 16, 16, 16, 27);
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

        private static int GetPower(MouseBatteryReading reading)
        {
            if (reading == null)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(100, reading.Power));
        }

        private static GraphicsPath CreateRoundedRectangle(
            RectangleF rectangle,
            float radius)
        {
            float diameter = radius * 2.0f;
            var path = new GraphicsPath();

            path.AddArc(
                rectangle.X,
                rectangle.Y,
                diameter,
                diameter,
                180,
                90);
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
