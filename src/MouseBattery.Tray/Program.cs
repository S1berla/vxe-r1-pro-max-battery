using System;
using System.Threading;
using System.Windows.Forms;

namespace MouseBattery.Tray
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (HasArgument(args, "--test"))
            {
                return RunSelfTest();
            }

            bool createdNew;
            using (var mutex = new Mutex(
                true,
                "Local\\VxeMouseBatteryTray",
                out createdNew))
            {
                if (!createdNew)
                {
                    return 0;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplicationContext());
            }

            return 0;
        }

        private static int RunSelfTest()
        {
            try
            {
                MouseBatteryReading reading = new VxeBatteryReader().ReadOnce();
                Console.WriteLine(
                    "{{\"power\":{0},\"battery\":\"{1}\"," +
                    "\"voltageMv\":{2},\"commandStatus\":{3}}}",
                    reading.Power,
                    reading.IsCharging ? "charging" : "discharging",
                    reading.VoltageMillivolts,
                    reading.CommandStatus);
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error.Message);
                return 1;
            }
        }

        private static bool HasArgument(string[] args, string expected)
        {
            for (int index = 0; index < args.Length; index++)
            {
                if (string.Equals(
                    args[index],
                    expected,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

