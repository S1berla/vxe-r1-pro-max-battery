using System;
using System.Globalization;
using System.Threading;
using MouseBattery;

internal static class Program
{
    private static int Main(string[] args)
    {
        bool watch = HasArgument(args, "--watch");
        bool json = HasArgument(args, "--json");
        int intervalSeconds = ReadInterval(args);

        var reader = new VxeBatteryReader();

        if (!watch)
        {
            return ReadOnce(reader, json);
        }

        while (true)
        {
            ReadOnce(reader, json);
            Thread.Sleep(TimeSpan.FromSeconds(intervalSeconds));
        }
    }

    private static int ReadOnce(VxeBatteryReader reader, bool json)
    {
        try
        {
            MouseBatteryReading reading = reader.ReadOnce();

            if (json)
            {
                Console.WriteLine(
                    "{{\"power\":{0},\"battery\":\"{1}\"," +
                    "\"voltageMv\":{2},\"commandStatus\":{3}," +
                    "\"retrievedAtUtc\":\"{4:O}\"}}",
                    reading.Power,
                    reading.IsCharging ? "charging" : "discharging",
                    reading.VoltageMillivolts,
                    reading.CommandStatus,
                    reading.RetrievedAtUtc);
            }
            else
            {
                Console.WriteLine(
                    "{0:yyyy-MM-dd HH:mm:ss}  Battery: {1}%  " +
                    "State: {2}  Voltage: {3:0.000} V",
                    reading.RetrievedAtUtc.ToLocalTime(),
                    reading.Power,
                    reading.IsCharging ? "charging" : "discharging",
                    reading.VoltageMillivolts / 1000.0);
            }

            return 0;
        }
        catch (Exception error)
        {
            if (json)
            {
                Console.WriteLine(
                    "{{\"error\":\"{0}\"}}",
                    EscapeJson(error.Message));
            }
            else
            {
                Console.Error.WriteLine(
                    "{0:yyyy-MM-dd HH:mm:ss}  Read failed: {1}",
                    DateTime.Now,
                    error.Message);
            }

            return 1;
        }
    }

    private static bool HasArgument(string[] args, string value)
    {
        for (int index = 0; index < args.Length; index++)
        {
            if (string.Equals(
                args[index],
                value,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int ReadInterval(string[] args)
    {
        for (int index = 0; index < args.Length - 1; index++)
        {
            if (!string.Equals(
                args[index],
                "--interval",
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int value;
            if (int.TryParse(
                args[index + 1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value) &&
                value >= 5)
            {
                return value;
            }
        }

        return 60;
    }

    private static string EscapeJson(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}

