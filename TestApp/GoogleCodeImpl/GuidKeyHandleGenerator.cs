using System;
using BlackFox.U2F.Key;

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
