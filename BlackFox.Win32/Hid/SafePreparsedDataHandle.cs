using System;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.Win32.Hid
{
    public class SafePreparsedDataHandle : SafeHandleMinusOneIsInvalid
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
            return HidDllNativeMethods.HidD_FreePreparsedData(handle);
        }
    }
}