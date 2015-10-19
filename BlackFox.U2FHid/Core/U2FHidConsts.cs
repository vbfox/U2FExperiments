namespace BlackFox.U2FHid.Core
{
    public static class U2FHidConsts
    {
        /// <summary>
        /// Id of the broadcast channel 
        /// </summary>
        public const uint BroadcastChannel = 0xffffffff;

        /// <summary>
        /// Size of the nonce for INIT messages in bytes.
        /// </summary>
        public const int InitNonceSize = 8;
    }
}
