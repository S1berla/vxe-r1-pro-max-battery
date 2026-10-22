using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MouseBattery
{
    internal static class HidNative
    {
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint OPEN_EXISTING = 3;
        internal const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        internal const int DIGCF_PRESENT = 0x00000002;
        internal const int DIGCF_DEVICEINTERFACE = 0x00000010;
        internal const int ERROR_IO_PENDING = 997;
        internal const int WAIT_OBJECT_0 = 0;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            internal int cbSize;
            internal Guid InterfaceClassGuid;
            internal int Flags;
            internal UIntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HIDP_CAPS
        {
            internal ushort Usage;
            internal ushort UsagePage;
            internal ushort InputReportByteLength;
            internal ushort OutputReportByteLength;
            internal ushort FeatureReportByteLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            internal ushort[] Reserved;

            internal ushort NumberLinkCollectionNodes;
            internal ushort NumberInputButtonCaps;
            internal ushort NumberInputValueCaps;
            internal ushort NumberInputDataIndices;
            internal ushort NumberOutputButtonCaps;
            internal ushort NumberOutputValueCaps;
            internal ushort NumberOutputDataIndices;
            internal ushort NumberFeatureButtonCaps;
            internal ushort NumberFeatureValueCaps;
            internal ushort NumberFeatureDataIndices;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct OVERLAPPED
        {
            internal UIntPtr Internal;
            internal UIntPtr InternalHigh;
            internal uint Offset;
            internal uint OffsetHigh;
            internal IntPtr hEvent;
        }

        [DllImport("hid.dll")]
        internal static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool HidD_GetPreparsedData(
            SafeFileHandle hidDeviceObject,
            out IntPtr preparsedData);

        [DllImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

        [DllImport("hid.dll")]
        internal static extern int HidP_GetCaps(
            IntPtr preparsedData,
            ref HIDP_CAPS capabilities);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid,
            IntPtr enumerator,
            IntPtr hwndParent,
            int flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr deviceInfoSet,
            IntPtr deviceInfoData,
            ref Guid interfaceClassGuid,
            int memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize,
            out int requiredSize,
            IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteFile(
            SafeFileHandle file,
            byte[] buffer,
            int numberOfBytesToWrite,
            out int numberOfBytesWritten,
            ref OVERLAPPED overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadFile(
            SafeFileHandle file,
            byte[] buffer,
            int numberOfBytesToRead,
            out int numberOfBytesRead,
            ref OVERLAPPED overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetOverlappedResult(
            SafeFileHandle file,
            ref OVERLAPPED overlapped,
            out int numberOfBytesTransferred,
            [MarshalAs(UnmanagedType.Bool)] bool wait);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateEvent(
            IntPtr eventAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool manualReset,
            [MarshalAs(UnmanagedType.Bool)] bool initialState,
            IntPtr name);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CancelIoEx(SafeFileHandle file, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);

        internal static IEnumerable<HidInterfaceInfo> EnumerateInterfaces()
        {
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);

            IntPtr deviceInfoSet = SetupDiGetClassDevs(
                ref hidGuid,
                IntPtr.Zero,
                IntPtr.Zero,
                DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            if (deviceInfoSet == new IntPtr(-1))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                for (int index = 0; ; index++)
                {
                    var interfaceData = new SP_DEVICE_INTERFACE_DATA();
                    interfaceData.cbSize =
                        Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

                    bool present = SetupDiEnumDeviceInterfaces(
                        deviceInfoSet,
                        IntPtr.Zero,
                        ref hidGuid,
                        index,
                        ref interfaceData);

                    if (!present)
                    {
                        int error = Marshal.GetLastWin32Error();
                        if (error == 259)
                        {
                            yield break;
                        }
                        throw new Win32Exception(error);
                    }

                    int requiredSize;
                    SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet,
                        ref interfaceData,
                        IntPtr.Zero,
                        0,
                        out requiredSize,
                        IntPtr.Zero);

                    IntPtr detailBuffer = Marshal.AllocHGlobal(requiredSize);
                    try
                    {
                        Marshal.WriteInt32(
                            detailBuffer,
                            0,
                            IntPtr.Size == 8 ? 8 : 6);

                        bool detailAvailable = SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet,
                            ref interfaceData,
                            detailBuffer,
                            requiredSize,
                            out requiredSize,
                            IntPtr.Zero);

                        if (!detailAvailable)
                        {
                            continue;
                        }

                        string path = Marshal.PtrToStringUni(
                            IntPtr.Add(detailBuffer, 4));

                        HidInterfaceInfo info = TryReadCaps(path);
                        if (info != null)
                        {
                            yield return info;
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detailBuffer);
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        private static HidInterfaceInfo TryReadCaps(string path)
        {
            using (SafeFileHandle handle = CreateFile(
                path,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    return null;
                }

                IntPtr preparsedData;
                if (!HidD_GetPreparsedData(handle, out preparsedData))
                {
                    return null;
                }

                try
                {
                    var caps = new HIDP_CAPS();
                    caps.Reserved = new ushort[17];

                    int status = HidP_GetCaps(preparsedData, ref caps);
                    if (status < 0)
                    {
                        return null;
                    }

                    return new HidInterfaceInfo(
                        path,
                        caps.UsagePage,
                        caps.Usage,
                        caps.InputReportByteLength,
                        caps.OutputReportByteLength,
                        caps.FeatureReportByteLength);
                }
                finally
                {
                    HidD_FreePreparsedData(preparsedData);
                }
            }
        }
    }

    internal sealed class HidInterfaceInfo
    {
        internal HidInterfaceInfo(
            string path,
            ushort usagePage,
            ushort usage,
            ushort inputReportByteLength,
            ushort outputReportByteLength,
            ushort featureReportByteLength)
        {
            Path = path;
            UsagePage = usagePage;
            Usage = usage;
            InputReportByteLength = inputReportByteLength;
            OutputReportByteLength = outputReportByteLength;
            FeatureReportByteLength = featureReportByteLength;
        }

        internal string Path { get; private set; }
        internal ushort UsagePage { get; private set; }
        internal ushort Usage { get; private set; }
        internal ushort InputReportByteLength { get; private set; }
        internal ushort OutputReportByteLength { get; private set; }
        internal ushort FeatureReportByteLength { get; private set; }
    }
}

