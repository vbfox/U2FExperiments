namespace U2FExperiments.Win32.Kernel32
{
    internal enum FileCreationDisposition : uint
    {
        /// <summary>
        /// Creates a new file, only if it does not already exist.
        /// </summary>
        CreateNew = 1,

        /// <summary>
        /// Creates a new file, always.
        /// </summary>
        CreateAlways = 2,

        /// <summary>
        /// Opens a file or device, only if it exists.
        /// </summary>
        OpenExisiting = 3,

        /// <summary>
        /// Opens a file, always.
        /// </summary>
        OpenAlways = 4,

        /// <summary>
        /// Opens a file and truncates it so that its size is zero bytes, only if it exists.
        /// </summary>
        TruncateExisting = 5
    }
}