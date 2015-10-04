using System;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace U2FExperiments.Win32.SetupApi
{
    class SafeDeviceInfoListHandle : SafeHandleMinusOneIsInvalid
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