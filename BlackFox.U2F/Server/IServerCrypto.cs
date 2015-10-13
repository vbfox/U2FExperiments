using JetBrains.Annotations;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Server
{
    public interface IServerCrypto
    {
        bool VerifySignature([NotNull] X509Certificate attestationCertificate, [NotNull] byte[] signedBytes, [NotNull] byte[] signature);

        bool VerifySignature([NotNull] ECPublicKeyParameters publicKey, [NotNull] byte[] signedBytes, [NotNull] byte[] signature);

        [NotNull]
        ECPublicKeyParameters DecodePublicKey([NotNull] byte[] encodedPublicKey);

        [NotNull]
        byte[] ComputeSha256([NotNull] byte[] bytes);
    }
}