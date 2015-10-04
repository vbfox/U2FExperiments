using System;
using System.Runtime.InteropServices;
using U2FExperiments.MiniUsbHid;

namespace U2FExperiments
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct U2FInitializationPaket
    {
        public int ChannelIdentifier;
        public byte CommandIdentifier;
        public byte PayloadLengthHi;
        public byte PayloadLengthLo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct U2FContinuationPaket
    {
        public int ChannelIdentifier;
        public byte PaketSequence;
    }

    class Program
    {
        static void Main(string[] args)
        {
            foreach (var b in DeviceList.Get())
            {
                Console.WriteLine("--------------------------");
                Console.WriteLine(b.Path);
                Console.WriteLine(b.Manufacturer);
                Console.WriteLine(b.Product);
                Console.WriteLine("VID = 0x{0:X4}", b.Vid);
                Console.WriteLine("PID = 0x{0:X4}", b.Pid);
            }
            Console.ReadLine();
        }
    }
}
