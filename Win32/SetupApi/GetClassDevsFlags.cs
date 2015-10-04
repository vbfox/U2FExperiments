using System;

namespace U2FExperiments.Win32.SetupApi
{
    /// <summary>
    /// Control options that filter the device information elements that are added to the device information set.
    /// </summary>
    [Flags]
    enum GetClassDevsFlags
    {
        /// <summary>
        /// Return a list of installed devices for all device setup classes or all device interface classes.
        /// </summary>
        AllClasses = 0x00000004,
        /// <summary>
        /// Return devices that support device interfaces for the specified device interface classes.
        /// This flag must be set in the Flags parameter if the Enumerator parameter specifies a device instance ID.
        /// </summary>
        DeviceInterface = 0x00000010,
        /// <summary>
        /// Return only the device that is associated with the system default device interface, if one is set,
        /// for the specified device interface classes.
        /// </summary>
        Default = 0x00000001,
        /// <summary>
        /// Return only devices that are currently present in a system.
        /// </summary>
        Present = 0x00000002,
        /// <summary>
        /// Return only devices that are a part of the current hardware profile.
        /// </summary>
        Profile = 0x00000008
    }
}