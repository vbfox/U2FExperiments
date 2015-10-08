using System.IO;
using System.Linq;

namespace BlackFox.Win32.Kernel32
{
    public static class Kernell32Extensions
    {
        public static FileFlags WithAttributes(this FileFlags flags, params FileAttributes[] attributes)
        {
            return attributes.Aggregate(flags, (current, attribute) => current | (FileFlags) (uint) attribute);
        }

        public static Kernel32FileAccess ToKernel32Generic(this FileAccess fileAccess)
        {
            var result = Kernel32FileAccess.None;
            if (fileAccess.HasFlag(FileAccess.Read))
            {
                result |= Kernel32FileAccess.GenericRead;
            }
            if (fileAccess.HasFlag(FileAccess.Write))
            {
                result |= Kernel32FileAccess.GenericWrite;
            }
            return result;
        }
    }
}