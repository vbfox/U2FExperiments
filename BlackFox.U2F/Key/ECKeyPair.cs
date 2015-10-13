using System;
using JetBrains.Annotations;
using Org.BouncyCastle.Crypto.Parameters;

namespace BlackFox.U2F.Key
{
    // ReSharper disable once InconsistentNaming
    public class ECKeyPair
    {
        public ECPublicKeyParameters PublicKey { get; private set; }

        public ECPrivateKeyParameters PrivateKey { get; private set; }

        public ECKeyPair([NotNull] ECPublicKeyParameters publicKey, [NotNull] ECPrivateKeyParameters privateKey)
        {
            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            PublicKey = publicKey;
            PrivateKey = privateKey;

        }
    }
}