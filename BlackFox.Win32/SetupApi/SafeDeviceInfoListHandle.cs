using System;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.Win32.SetupApi
{
    public class SafeDeviceInfoListHandle : SafeHandleMinusOneIsInvalid
    {
        [UsedImplicitly]
        SafeDeviceInfoListHandle()
            : base(true)
        {
        }

        public SafeDeviceInfoListHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            handle = preexistingHandle;
        }

        protected override bool ReleaseHandle()
        {
            return SetupApiDll.DestroyDeviceInfoList(handle);
        }
    }
}