using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace MouseBattery
{
    public sealed class MouseBatteryReading
    {
        public int Power { get; set; }
        public bool IsCharging { get; set; }
        public int VoltageMillivolts { get; set; }
        public byte CommandStatus { get; set; }
        public DateTime RetrievedAtUtc { get; set; }
    }

    public sealed class VxeBatteryReader
    {
        private const uint VendorId = 0x3554;
        private const uint ProductId = 0xF58A;
        private const ushort UsagePage = 0xFF02;
        private const ushort Usage = 2;

        private const byte ReportId = 0x08;
        private const byte GetWirelessMouseOnline = 0x03;
        private const byte DownLoadData = 0x01;
        private const byte GetBatteryLevel = 0x04;

        public MouseBatteryReading ReadOnce()
        {
            HidInterfaceInfo target = FindTarget();
            if (target == null)
            {
                throw new InvalidOperationException(
                    "VXE NordicMouse 1K Dongle data interface was not found.");
            }

            using (var connection = new HidConnection(target))
            {
                connection.Transact(
                    GetWirelessMouseOnline,
                    new byte[0],
                    1000);

                connection.Transact(
                    DownLoadData,
                    new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                    1000);

                TimeoutException lastTimeout = null;
                for (int attempt = 1; attempt <= 20; attempt++)
                {
                    try
                    {
                        byte[] response = connection.Transact(
                            GetBatteryLevel,
                            new byte[0],
                            500);

                        if (response.Length < 10)
                        {
                            throw new InvalidOperationException(
                                "Battery response was shorter than expected.");
                        }

                        return new MouseBatteryReading
                        {
                            Power = response[6],
                            IsCharging = response[7] == 1,
                            VoltageMillivolts =
                                response[8] | (response[9] << 8),
                            CommandStatus = response[2],
                            RetrievedAtUtc = DateTime.UtcNow,
                        };
                    }
                    catch (TimeoutException error)
                    {
                        lastTimeout = error;
                        Thread.Sleep(250);
                    }
                }

                throw new TimeoutException(
                    "Battery response was not received.",
                    lastTimeout);
            }
        }

        private static HidInterfaceInfo FindTarget()
        {
            foreach (HidInterfaceInfo info in HidNative.EnumerateInterfaces())
            {
                if (info.UsagePage != UsagePage || info.Usage != Usage)
                {
                    continue;
                }

                string upperPath = info.Path.ToUpperInvariant();
                if (upperPath.Contains("VID_3554") &&
                    upperPath.Contains("PID_F58A"))
                {
                    return info;
                }
            }

            return null;
        }

        private sealed class HidConnection : IDisposable
        {
            private readonly SafeFileHandle handle;
            private readonly int inputReportLength;
            private readonly int outputReportLength;

            internal HidConnection(HidInterfaceInfo info)
            {
                inputReportLength = info.InputReportByteLength;
                outputReportLength = info.OutputReportByteLength;

                handle = HidNative.CreateFile(
                    info.Path,
                    HidNative.GENERIC_READ | HidNative.GENERIC_WRITE,
                    HidNative.FILE_SHARE_READ | HidNative.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    HidNative.OPEN_EXISTING,
                    HidNative.FILE_FLAG_OVERLAPPED,
                    IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            internal byte[] Transact(
                byte commandId,
                byte[] data,
                int timeoutMilliseconds)
            {
                byte[] report = BuildReport(commandId, data);
                WriteReport(report, timeoutMilliseconds);

                DateTime deadline = DateTime.UtcNow.AddMilliseconds(
                    timeoutMilliseconds);

                while (true)
                {
                    int remaining = (int)Math.Max(
                        1,
                        (deadline - DateTime.UtcNow).TotalMilliseconds);

                    byte[] response = ReadReport(remaining);
                    if (response.Length >= 2 &&
                        response[0] == ReportId &&
                        response[1] == commandId)
                    {
                        return response;
                    }
                }
            }

            private static byte[] BuildReport(byte commandId, byte[] data)
            {
                if (data == null)
                {
                    data = new byte[0];
                }

                if (data.Length > 10)
                {
                    throw new ArgumentOutOfRangeException(
                        "data",
                        "A COMPX command can contain at most 10 data bytes.");
                }

                var report = new byte[17];
                report[0] = ReportId;
                report[1] = commandId;
                report[4] = (byte)data.Length;

                Array.Copy(data, 0, report, 5, data.Length);

                int sum = 8;
                for (int index = 1; index <= 15; index++)
                {
                    sum += report[index];
                }

                report[16] = unchecked((byte)(85 - (sum & 0xFF)));
                return report;
            }

            private void WriteReport(byte[] report, int timeoutMilliseconds)
            {
                IntPtr eventHandle = HidNative.CreateEvent(
                    IntPtr.Zero,
                    true,
                    false,
                    IntPtr.Zero);

                if (eventHandle == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                try
                {
                    var overlapped = new HidNative.OVERLAPPED();
                    overlapped.hEvent = eventHandle;

                    int bytesWritten;
                    bool completed = HidNative.WriteFile(
                        handle,
                        report,
                        outputReportLength,
                        out bytesWritten,
                        ref overlapped);

                    if (!completed)
                    {
                        int error = Marshal.GetLastWin32Error();
                        if (error != HidNative.ERROR_IO_PENDING)
                        {
                            throw new Win32Exception(error);
                        }

                        if (HidNative.WaitForSingleObject(
                                eventHandle,
                                (uint)timeoutMilliseconds) !=
                            HidNative.WAIT_OBJECT_0)
                        {
                            HidNative.CancelIoEx(handle, IntPtr.Zero);
                            throw new TimeoutException(
                                "Timed out writing the HID report.");
                        }
                    }

                    if (!HidNative.GetOverlappedResult(
                        handle,
                        ref overlapped,
                        out bytesWritten,
                        false))
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error());
                    }
                }
                finally
                {
                    HidNative.CloseHandle(eventHandle);
                }
            }

            private byte[] ReadReport(int timeoutMilliseconds)
            {
                IntPtr eventHandle = HidNative.CreateEvent(
                    IntPtr.Zero,
                    true,
                    false,
                    IntPtr.Zero);

                if (eventHandle == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                try
                {
                    var buffer = new byte[inputReportLength];
                    var overlapped = new HidNative.OVERLAPPED();
                    overlapped.hEvent = eventHandle;

                    int bytesRead;
                    bool completed = HidNative.ReadFile(
                        handle,
                        buffer,
                        buffer.Length,
                        out bytesRead,
                        ref overlapped);

                    if (!completed)
                    {
                        int error = Marshal.GetLastWin32Error();
                        if (error != HidNative.ERROR_IO_PENDING)
                        {
                            throw new Win32Exception(error);
                        }

                        if (HidNative.WaitForSingleObject(
                                eventHandle,
                                (uint)timeoutMilliseconds) !=
                            HidNative.WAIT_OBJECT_0)
                        {
                            HidNative.CancelIoEx(handle, IntPtr.Zero);
                            throw new TimeoutException(
                                "Timed out waiting for a HID input report.");
                        }
                    }

                    if (!HidNative.GetOverlappedResult(
                        handle,
                        ref overlapped,
                        out bytesRead,
                        false))
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error());
                    }

                    if (bytesRead == buffer.Length)
                    {
                        return buffer;
                    }

                    var result = new byte[bytesRead];
                    Array.Copy(buffer, result, bytesRead);
                    return result;
                }
                finally
                {
                    HidNative.CloseHandle(eventHandle);
                }
            }

            public void Dispose()
            {
                handle.Dispose();
            }
        }
    }
}

