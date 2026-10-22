using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MouseBattery.Tray
{
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem refreshItem;
        private readonly ToolStripMenuItem autostartItem;
        private readonly Timer timer;
        private readonly VxeBatteryReader reader;

        private MouseBatteryReading lastReading;
        private DetailsForm detailsForm;
        private Icon currentIcon;
        private bool isReading;
        private bool initialized;
        private int consecutiveFailures;
        private bool disposed;

        internal TrayApplicationContext()
        {
            AutostartManager.ApplyDefaultOnFirstRun();

            reader = new VxeBatteryReader();

            refreshItem = new ToolStripMenuItem("立即刷新");
            refreshItem.Click += RefreshItemClick;

            autostartItem = new ToolStripMenuItem("开机启动");
            autostartItem.CheckOnClick = false;
            autostartItem.Checked = AutostartManager.IsEnabled();
            autostartItem.Click += AutostartItemClick;

            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += ExitItemClick;

            menu = new ContextMenuStrip();
            menu.Items.Add(refreshItem);
            menu.Items.Add(autostartItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            notifyIcon = new NotifyIcon();
            notifyIcon.ContextMenuStrip = menu;
            notifyIcon.Text = "VXE 鼠标电量：正在初始化";
            notifyIcon.DoubleClick += NotifyIconDoubleClick;
            SetIcon(null, true);
            notifyIcon.Visible = true;

            timer = new Timer();
            timer.Interval = 50;
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (!initialized)
            {
                initialized = true;
                timer.Stop();
            }

            if (!isReading)
            {
                RefreshAsync();
            }

            timer.Interval = 60000;
            timer.Start();
        }

        private void RefreshItemClick(object sender, EventArgs e)
        {
            if (!isReading)
            {
                timer.Stop();
                RefreshAsync();
            }
        }

        private void RefreshAsync()
        {
            if (isReading)
            {
                return;
            }

            isReading = true;
            refreshItem.Enabled = false;
            notifyIcon.Text = "VXE 鼠标电量：正在读取";

            if (detailsForm != null && !detailsForm.IsDisposed)
            {
                detailsForm.UpdateStatus("正在读取...");
            }

            TaskScheduler uiScheduler =
                TaskScheduler.FromCurrentSynchronizationContext();

            Task.Run(delegate
            {
                return reader.ReadOnce();
            }).ContinueWith(delegate(Task<MouseBatteryReading> task)
            {
                if (disposed)
                {
                    return;
                }

                isReading = false;
                refreshItem.Enabled = true;

                if (task.IsFaulted)
                {
                    HandleFailure(task.Exception == null
                        ? new Exception("Unknown HID read failure.")
                        : task.Exception.GetBaseException());
                }
                else
                {
                    HandleSuccess(task.Result);
                }
            }, uiScheduler);
        }

        private void HandleSuccess(MouseBatteryReading reading)
        {
            lastReading = reading;
            consecutiveFailures = 0;
            timer.Interval = 60000;

            SetIcon(reading, false);
            notifyIcon.Text = BuildTooltip(reading, false);

            if (detailsForm != null && !detailsForm.IsDisposed)
            {
                detailsForm.UpdateReading(reading, "正常", false);
            }

            timer.Start();

            AppLog.Info(string.Format(
                "Read succeeded: {0}% {1} {2}mV",
                reading.Power,
                reading.IsCharging ? "charging" : "discharging",
                reading.VoltageMillivolts));
        }

        private void HandleFailure(Exception error)
        {
            consecutiveFailures++;
            bool hasLastReading = lastReading != null;

            SetIcon(lastReading, true);
            notifyIcon.Text = hasLastReading
                ? BuildStaleTooltip(lastReading)
                : "VXE 鼠标电量：未连接或休眠";

            if (detailsForm != null && !detailsForm.IsDisposed)
            {
                detailsForm.UpdateReading(
                    lastReading,
                    "读取失败",
                    true);
            }

            timer.Interval = consecutiveFailures < 3 ? 15000 : 60000;
            timer.Start();
            AppLog.Error("Read failed.", error);
        }

        private void SetIcon(MouseBatteryReading reading, bool stale)
        {
            Icon next = BatteryIconRenderer.Create(reading, stale);
            Icon previous = currentIcon;

            currentIcon = next;
            notifyIcon.Icon = next;

            if (previous != null)
            {
                previous.Dispose();
            }
        }

        private static string BuildTooltip(
            MouseBatteryReading reading,
            bool stale)
        {
            string state = reading.IsCharging ? "充电" : "放电";
            string text = string.Format(
                "VXE R1 Pro Max {0}% {1} {2:0.00}V",
                reading.Power,
                state,
                reading.VoltageMillivolts / 1000.0);

            if (stale)
            {
                text += " 未更新";
            }

            if (text.Length > 63)
            {
                text = text.Substring(0, 63);
            }

            return text;
        }

        private static string BuildStaleTooltip(MouseBatteryReading reading)
        {
            return BuildTooltip(reading, true);
        }

        private void AutostartItemClick(object sender, EventArgs e)
        {
            try
            {
                bool enable = !autostartItem.Checked;
                AutostartManager.SetEnabled(enable);
                autostartItem.Checked = AutostartManager.IsEnabled();
            }
            catch (Exception error)
            {
                AppLog.Error("Could not update autostart.", error);
                MessageBox.Show(
                    "无法修改开机启动设置：\n" + error.Message,
                    "VXE 鼠标电量",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void NotifyIconDoubleClick(object sender, EventArgs e)
        {
            if (detailsForm == null || detailsForm.IsDisposed)
            {
                detailsForm = new DetailsForm();
                detailsForm.FormClosed += delegate
                {
                    detailsForm = null;
                };
                detailsForm.UpdateReading(
                    lastReading,
                    lastReading == null ? "等待读取" : "正常",
                    false);
                detailsForm.Show();
            }
            else
            {
                detailsForm.Show();
                detailsForm.Activate();
            }
        }

        private void ExitItemClick(object sender, EventArgs e)
        {
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                disposed = true;

                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                }

                if (notifyIcon != null)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Dispose();
                }

                if (currentIcon != null)
                {
                    currentIcon.Dispose();
                    currentIcon = null;
                }

                if (menu != null)
                {
                    menu.Dispose();
                }

                if (detailsForm != null && !detailsForm.IsDisposed)
                {
                    detailsForm.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
