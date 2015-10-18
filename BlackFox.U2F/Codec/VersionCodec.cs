using System;

namespace BlackFox.U2F.Codec
{
    public static class VersionCodec
    {
        public static U2FVersion DecodeVersion(string version)
        {
            switch (version)
            {
                case U2FConsts.U2Fv1:
                    return U2FVersion.V1;

                case U2FConsts.U2Fv2:
                    return U2FVersion.V2;

                default:
                    throw new ArgumentOutOfRangeException(nameof(version), version, "Unknown U2F version");
            }
        }
    }
}