using System;
using BlackFox.U2F.Gnubby.Simulated;

namespace U2FExperiments
{
    class GuidKeyHandleGenerator : IKeyHandleGenerator
    {
        public byte[] GenerateKeyHandle(byte[] applicationSha256, ECKeyPair keyPair)
        {
            return Guid.NewGuid().ToByteArray();
        }
    }
}
