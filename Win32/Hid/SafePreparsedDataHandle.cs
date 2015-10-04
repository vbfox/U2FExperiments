using System;
using Microsoft.Win32.SafeHandles;

namespace U2FExperiments.Win32.Hid
{
    class SafePreparsedDataHandle : SafeHandleMinusOneIsInvalid
    {
        public SafePreparsedDataHandle() : base(true)
        {

        }

        public SafePreparsedDataHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            handle = preexistingHandle;
        }

        protected override bool ReleaseHandle()
        {
            return HidDll.NativeMethods.HidD_FreePreparsedData(handle);
        }
    }
}