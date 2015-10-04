using System;

namespace U2FExperiments.Win32
{
    class Native
    {
        /* invalid handle value */
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);


        #region kernel32.dll


        /* read access */
        public const uint GENERIC_READ = 0x80000000;
        /* write access */
        public const uint GENERIC_WRITE = 0x40000000;
        /* Enables subsequent open operations on a file or device to request 
         * write access.*/
        public const uint FILE_SHARE_WRITE = 0x2;
        /* Enables subsequent open operations on a file or device to request
         * read access. */
        public const uint FILE_SHARE_READ = 0x1;
        /* The file or device is being opened or created for asynchronous I/O. */
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        /* Opens a file or device, only if it exists. */
        public const uint OPEN_EXISTING = 3;
        /* Opens a file, always. */
        public const uint OPEN_ALWAYS = 4;



        #endregion
        #region hid.dll







        #endregion
        #region setupapi.dll


        /* Return only devices that are currently present in a system. */
        public const int DIGCF_PRESENT = 0x02;
        /* Return devices that support device interfaces for the specified 
         * device interface classes. */
        public const int DIGCF_DEVICEINTERFACE = 0x10;





        #endregion
  
    }
}
