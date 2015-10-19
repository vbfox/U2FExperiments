namespace BlackFox.U2F.Codec
{
    public static class VersionCodec
    {
        public static bool TryDecodeVersion(string version, out U2FVersion parsedVersion)
        {
            switch (version)
            {
                case U2FConsts.U2Fv1:
                    parsedVersion = U2FVersion.V1;
                    return true;

                case U2FConsts.U2Fv2:
                    parsedVersion = U2FVersion.V2;
                    return true;

                default:
                    parsedVersion = default(U2FVersion);
                    return false;
            }
        }
    }
}