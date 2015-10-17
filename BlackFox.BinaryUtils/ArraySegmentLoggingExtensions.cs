using System;
using System.IO;
using Common.Logging;
using JetBrains.Annotations;

namespace BlackFox.Binary
{
    internal static class ArraySegmentLoggingExtensions
    {
        public static Action<FormatMessageHandler> ToLoggableAsHex(this ArraySegment<byte> segment, [CanBeNull] string header,
            bool includeAscii = true, bool includeStartAddresses = true)
        {
            return m =>
            {
                var writer = new StringWriter();
                if (header != null)
                {
                    writer.WriteLine(header);
                    writer.WriteLine();
                }
                segment.WriteAsHexTo(writer, includeAscii, includeStartAddresses);
                m("{0}", writer.ToString());
            };
        }
    }
}
