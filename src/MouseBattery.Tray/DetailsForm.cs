using System;
using System.Drawing;
using System.Windows.Forms;

namespace MouseBattery.Tray
{
    internal sealed class DetailsForm : Form
    {
        private readonly Label powerValue;
        private readonly Label stateValue;
        private readonly Label voltageValue;
        private readonly Label updatedValue;
        private readonly Label statusValue;

        internal DetailsForm()
        {
            Text = "VXE Dragonfly R1 Pro Max";
            ClientSize = new Size(390, 240);
            MinimumSize = new Size(406, 279);
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 2;
            table.RowCount = 7;
            table.Padding = new Padding(18);
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Label title = CreateTitle();
            table.Controls.Add(title, 0, 0);
            table.SetColumnSpan(title, 2);

            table.Controls.Add(CreateCaption("设备"), 0, 1);
            Label deviceValue = CreateValue(
                "VXE Dragonfly R1 Pro Max\nVXE NordicMouse 1K Dongle");
            table.Controls.Add(deviceValue, 1, 1);

            table.Controls.Add(CreateCaption("电量"), 0, 2);
            powerValue = CreateValue("--");
            table.Controls.Add(powerValue, 1, 2);

            table.Controls.Add(CreateCaption("状态"), 0, 3);
            stateValue = CreateValue("未知");
            table.Controls.Add(stateValue, 1, 3);

            table.Controls.Add(CreateCaption("电压"), 0, 4);
            voltageValue = CreateValue("--");
            table.Controls.Add(voltageValue, 1, 4);

            table.Controls.Add(CreateCaption("更新时间"), 0, 5);
            updatedValue = CreateValue("--");
            table.Controls.Add(updatedValue, 1, 5);

            table.Controls.Add(CreateCaption("读取状态"), 0, 6);
            statusValue = CreateValue("等待读取");
            table.Controls.Add(statusValue, 1, 6);

            for (int row = 1; row <= 6; row++)
            {
                table.RowStyles.Add(
                    new RowStyle(SizeType.Absolute, row == 1 ? 52 : 30));
            }

            Controls.Add(table);
        }

        internal void UpdateReading(
            MouseBatteryReading reading,
            string status,
            bool stale)
        {
            if (reading == null)
            {
                powerValue.Text = "--";
                stateValue.Text = "未知";
                voltageValue.Text = "--";
                updatedValue.Text = "--";
                statusValue.Text = status ?? "未读取";
                return;
            }

            powerValue.Text = reading.Power + "%";
            stateValue.Text = reading.IsCharging ? "充电中" : "放电中";
            voltageValue.Text = string.Format(
                "{0:0.000} V",
                reading.VoltageMillivolts / 1000.0);
            updatedValue.Text = reading.RetrievedAtUtc
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");
            statusValue.Text = stale
                ? "未更新（设备休眠或 HID 占用）"
                : "正常";
        }

        internal void UpdateStatus(string status)
        {
            statusValue.Text = status;
        }

        private static Label CreateTitle()
        {
            var label = new Label();
            label.AutoSize = true;
            label.Font = new Font(
                "Segoe UI",
                14.0f,
                FontStyle.Bold,
                GraphicsUnit.Point);
            label.Text = "鼠标电池状态";
            label.Margin = new Padding(0, 0, 0, 12);
            return label;
        }

        private static Label CreateCaption(string text)
        {
            var label = new Label();
            label.AutoSize = true;
            label.ForeColor = Color.FromArgb(100, 100, 100);
            label.Text = text;
            label.Margin = new Padding(0, 5, 8, 0);
            return label;
        }

        private static Label CreateValue(string text)
        {
            var label = new Label();
            label.AutoSize = true;
            label.Text = text;
            label.Margin = new Padding(0, 4, 0, 0);
            return label;
        }
    }
}

