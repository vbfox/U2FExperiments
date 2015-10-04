﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace U2FExperiments.Win32.SetupApi
{
    partial class SetupApiDll
    {
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
        public static class NativeMethods
        {
            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern DeviceInfoListSafeHandle SetupDiGetClassDevs(IntPtr gClass,
                [MarshalAs(UnmanagedType.LPStr)] string strEnumerator,
                IntPtr hParent,
                GetClassDevsFlags nFlags);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiEnumDeviceInterfaces(
                DeviceInfoListSafeHandle lpDeviceInfoSet,
                uint nDeviceInfoData,
                ref Guid gClass,
                uint nIndex,
                ref DeviceInterfaceData oInterfaceData);
        }
    }
}