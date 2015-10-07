using System.Linq;

namespace U2FExperiments.Win32.Kernel32
{
    internal static class Kernell32Extensions
    {
        public static FileFlags WithAttributes(this FileFlags flags, params FileAttributes[] attributes)
        {
            return attributes.Aggregate(flags, (current, attribute) => current | (FileFlags) (uint) attribute);
        }
    }
}